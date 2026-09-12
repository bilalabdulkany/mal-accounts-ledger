using Mal.Accounts.Ledger.Core.Domain;

namespace Mal.Accounts.Ledger.Core.Application;

public sealed class LedgerEngine
{
    public const int FirstDay = 1;
    public const int LastDay = 6;
    public const decimal OverdraftFeeAmount = 25.00m;
    public const decimal DailyInterestRate = 0.0004m; // 0.04%

    private readonly Dictionary<string, Account> _accounts;
    private readonly List<LedgerEntry> _entries = [];
    private readonly Dictionary<string, Authorization> _authorizations = new(StringComparer.Ordinal);
    private readonly List<ProcessingError> _errors = [];
    private readonly HashSet<(string AccountId, int Day)> _assessedFees = [];
    private readonly HashSet<string> _processedEventIds = new(StringComparer.Ordinal);
    private bool _replayed;

    public LedgerEngine(IEnumerable<Account> accounts)
    {
        _accounts = accounts.ToDictionary(a => a.Id, StringComparer.Ordinal);
        if (_accounts.Count == 0)
            throw new ArgumentException("At least one account is required.", nameof(accounts));
    }

    public IReadOnlyList<LedgerEntry> Entries => _entries;
    public IReadOnlyList<Authorization> Authorizations => _authorizations.Values.OrderBy(a => a.AuthorizationId).ToList();

    public ReplayResult Replay(IReadOnlyList<LedgerEvent> events)
    {
        if (_replayed)
            throw new InvalidOperationException("An engine instance can only replay one event stream.");

        _replayed = true;

        foreach (var evt in events)
        {
            if (!_processedEventIds.Add(evt.EventId))
            {
                AddError(evt, "DUPLICATE_EVENT", "Event ID has already been processed.");
                continue;
            }

            try
            {
                ValidateEventShape(evt);
                Process(evt);
            }
            catch (InvalidOperationException ex)
            {
                AddError(evt, "INVALID_EVENT", ex.Message);
            }
        }

        // Interest is calculated from the fully replayed ledger before the Day-6
        // interest capitalization entry itself is appended.
        var interest = CalculateDailyInterest();
        var capitalized = interest
            .GroupBy(x => x.AccountId, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => SumMoney(g.Select(x => x.Amount), _accounts[g.Key].Currency),
                StringComparer.Ordinal);

        foreach (var (accountId, total) in capitalized)
        {
            if (total.Amount == 0m)
                continue;

            Append(new LedgerEntry(
                $"INTEREST-DAY6-{accountId}",
                accountId,
                EntryType.InterestCapitalization,
                total,
                LastDay,
                SourceEventId: "INTEREST-CAPITALIZATION"));
        }

        // The capitalization amount is defined as the sum of the already-rounded
        // daily accruals. This makes the reconciliation invariant explicit.
        foreach (var account in _accounts.Values)
        {
            var dailySum = SumMoney(
                interest.Where(x => x.AccountId == account.Id).Select(x => x.Amount),
                account.Currency);

            if (!capitalized.TryGetValue(account.Id, out var capitalizedAmount))
                throw new InvalidOperationException($"Missing capitalization total for {account.Id}.");

            if (dailySum != capitalizedAmount)
                throw new InvalidOperationException(
                    $"Interest reconciliation failed for {account.Id}: daily accruals {dailySum} != capitalization {capitalizedAmount}.");
        }

        var snapshots = BuildSnapshots(interest);
        return new ReplayResult(
            _entries.ToList(),
            Authorizations,
            snapshots,
            interest,
            capitalized);
    }

    public Money CalculateLedgerBalance(string accountId, int day)
    {
        var account = GetAccount(accountId);
        ValidateDay(day);

        var amount = account.OpeningBalance.Amount;
        foreach (var entry in _entries.Where(e => e.AccountId == accountId && e.ValueDay <= day))
        {
            amount += entry.Type is EntryType.Credit or EntryType.InterestCapitalization
                ? entry.Amount.Amount
                : -entry.Amount.Amount;
        }

        return new Money(account.Currency, amount);
    }

    public Money CalculateAvailableBalance(string accountId, int day)
    {
        var account = GetAccount(accountId);
        var ledgerBalance = CalculateLedgerBalance(accountId, day).Amount;
        var activeHolds = CalculateActiveHolds(accountId, day);
        return new Money(account.Currency, ledgerBalance - activeHolds);
    }

    private void ValidateEventShape(LedgerEvent evt)
    {
        var account = GetAccount(evt.AccountId);
        ValidateDay(evt.BookedDay);
        ValidateDay(evt.ValueDay);

        switch (evt)
        {
            case CreditEvent e:
                ValidateCurrency(e.Amount, account.Currency);
                break;
            case DebitEvent e:
                ValidateCurrency(e.Amount, account.Currency);
                break;
            case AuthorizationEvent e:
                ValidateCurrency(e.Hold, account.Currency);
                if (string.IsNullOrWhiteSpace(e.AuthorizationId))
                    throw new InvalidOperationException("Authorization ID is required.");
                if (e.Hold.Amount <= 0m)
                    throw new InvalidOperationException("Authorization hold must be positive.");
                break;
            case SettlementEvent e:
                ValidateCurrency(e.Amount, account.Currency);
                if (string.IsNullOrWhiteSpace(e.AuthorizationId))
                    throw new InvalidOperationException("Authorization ID is required.");
                if (e.Amount.Amount <= 0m)
                    throw new InvalidOperationException("Settlement amount must be positive.");
                break;
            case ReversalEvent e:
                if (string.IsNullOrWhiteSpace(e.ReversesEventId))
                    throw new InvalidOperationException("Reversal target event ID is required.");
                break;
            case InstallmentCreditEvent e:
                ValidateCurrency(e.Total, account.Currency);
                if (e.Total.Amount <= 0m)
                    throw new InvalidOperationException("Installment total must be positive.");
                if (e.InstallmentCount <= 0)
                    throw new InvalidOperationException("Installment count must be positive.");
                break;
        }
    }

    private void Process(LedgerEvent evt)
    {
        switch (evt)
        {
            case CreditEvent e:
                Append(new LedgerEntry(e.EventId, e.AccountId, EntryType.Credit, e.Amount, e.ValueDay));
                AssessOverdraftFor(e.AccountId, e.ValueDay, e.EventId);
                break;
            case DebitEvent e:
                Append(new LedgerEntry(e.EventId, e.AccountId, EntryType.Debit, e.Amount, e.ValueDay));
                AssessOverdraftFor(e.AccountId, e.ValueDay, e.EventId);
                break;
            case AuthorizationEvent e:
                ProcessAuthorization(e);
                break;
            case SettlementEvent e:
                ProcessSettlement(e);
                break;
            case ReversalEvent e:
                ProcessReversal(e);
                break;
            case InstallmentCreditEvent e:
                ProcessInstallmentCredit(e);
                AssessOverdraftFor(e.AccountId, e.ValueDay, e.EventId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(evt));
        }
    }

    private void ProcessAuthorization(AuthorizationEvent e)
    {
        if (_authorizations.ContainsKey(e.AuthorizationId))
        {
            AddError(e, "DUPLICATE_AUTHORIZATION", $"Authorization {e.AuthorizationId} already exists.");
            return;
        }

        var balance = CalculateLedgerBalance(e.AccountId, e.ValueDay).Amount;
        var activeHolds = CalculateActiveHolds(e.AccountId, e.BookedDay);
        var hold = e.Hold;
        var availableAfterHold = balance - activeHolds - hold.Amount;
        var status = availableAfterHold >= 0m
            ? AuthorizationStatus.Approved
            : AuthorizationStatus.Rejected;

        _authorizations[e.AuthorizationId] = new Authorization(
            e.AuthorizationId,
            e.AccountId,
            hold,
            e.ValueDay,
            status,
            e.EventId,
            e.BookedDay,
            SettledDay: null);
    }

    private void ProcessSettlement(SettlementEvent e)
    {
        if (!_authorizations.TryGetValue(e.AuthorizationId, out var auth))
        {
            AddError(e, "UNKNOWN_AUTHORIZATION", $"Settlement references unknown authorization {e.AuthorizationId}.");
            return;
        }

        if (auth.BookedDay > e.BookedDay)
        {
            AddError(e, "AUTHORIZATION_NOT_YET_BOOKED", $"Authorization {e.AuthorizationId} was booked after this settlement.");
            return;
        }

        if (auth.Status != AuthorizationStatus.Approved)
        {
            AddError(e, "AUTHORIZATION_NOT_APPROVED", $"Authorization {e.AuthorizationId} is not approved.");
            return;
        }

        var amount = e.Amount;
        if (amount.Amount > auth.Hold.Amount)
        {
            AddError(e, "SETTLEMENT_EXCEEDS_HOLD", $"Settlement exceeds hold for {e.AuthorizationId}.");
            return;
        }

        Append(new LedgerEntry(
            e.EventId,
            e.AccountId,
            EntryType.Debit,
            amount,
            e.ValueDay,
            RelatedAuthorizationId: e.AuthorizationId));

        _authorizations[e.AuthorizationId] = auth with
        {
            Status = AuthorizationStatus.Settled,
            SettledDay = e.BookedDay
        };

        AssessOverdraftFor(e.AccountId, e.ValueDay, e.EventId);
    }

    private void ProcessReversal(ReversalEvent e)
    {
        var original = _entries.FirstOrDefault(x => x.EntryId == e.ReversesEventId);
        if (original is null)
        {
            AddError(e, "UNKNOWN_REVERSAL_TARGET", $"Cannot reverse unknown ledger entry {e.ReversesEventId}.");
            return;
        }

        if (original.AccountId != e.AccountId)
        {
            AddError(e, "REVERSAL_ACCOUNT_MISMATCH", "Reversal account does not match original entry.");
            return;
        }

        if (original.Type is not (EntryType.Credit or EntryType.Debit))
        {
            AddError(e, "NON_REVERSIBLE_ENTRY", $"Ledger entry {e.ReversesEventId} is not reversible.");
            return;
        }

        if (_entries.Any(x => x.SourceEventId == e.ReversesEventId && x.EntryId != e.ReversesEventId))
        {
            AddError(e, "ALREADY_REVERSED", $"Ledger entry {e.ReversesEventId} has already been reversed.");
            return;
        }

        var reversalType = original.Type == EntryType.Credit ? EntryType.Debit : EntryType.Credit;
        Append(new LedgerEntry(
            e.EventId,
            e.AccountId,
            reversalType,
            original.Amount,
            e.ValueDay,
            SourceEventId: e.ReversesEventId));

        AssessOverdraftFor(e.AccountId, e.ValueDay, e.EventId);
    }

    private void ProcessInstallmentCredit(InstallmentCreditEvent e)
    {
        var total = e.Total;
        var scale = total.Scale;
        var baseAmount = decimal.Round(
            total.Amount / e.InstallmentCount,
            scale,
            MidpointRounding.ToZero);
        var remainder = total.Amount - baseAmount * e.InstallmentCount;

        for (var i = 1; i <= e.InstallmentCount; i++)
        {
            // Deterministic policy: allocate the rounding remainder to the final installment.
            var amount = baseAmount + (i == e.InstallmentCount ? remainder : 0m);
            Append(new LedgerEntry(
                $"{e.EventId}-I{i}",
                e.AccountId,
                EntryType.Credit,
                new Money(total.Currency, amount),
                e.ValueDay,
                SourceEventId: e.EventId));
        }
    }

    private void AssessOverdraftFor(string accountId, int day, string triggeringEventId)
    {
        if (!_assessedFees.Add((accountId, day)))
            return;

        var balanceBeforeFee = CalculateLedgerBalance(accountId, day).Amount;
        if (balanceBeforeFee >= 0m)
        {
            _assessedFees.Remove((accountId, day));
            return;
        }

        var account = GetAccount(accountId);
        var fee = new Money(account.Currency, OverdraftFeeAmount);
        Append(new LedgerEntry(
            $"FEE-{accountId}-D{day}",
            accountId,
            EntryType.OverdraftFee,
            fee,
            day,
            SourceEventId: triggeringEventId));
    }

    private List<InterestAccrual> CalculateDailyInterest()
    {
        var result = new List<InterestAccrual>();

        foreach (var account in _accounts.Values)
        {
            for (var day = FirstDay; day <= LastDay; day++)
            {
                var balance = CalculateLedgerBalance(account.Id, day);
                var raw = balance.Amount > 0m ? balance.Amount * DailyInterestRate : 0m;
                var accrual = new Money(account.Currency, raw);
                result.Add(new InterestAccrual(account.Id, day, balance, accrual));
            }
        }

        return result;
    }

    private List<DaySnapshot> BuildSnapshots(IReadOnlyList<InterestAccrual> interest)
    {
        var result = new List<DaySnapshot>();

        foreach (var account in _accounts.Values)
        {
            for (var day = FirstDay; day <= LastDay; day++)
            {
                var fees = _entries
                    .Where(e => e.AccountId == account.Id && e.Type == EntryType.OverdraftFee && e.ValueDay == day)
                    .Select(e => e.Amount);

                var auths = _authorizations.Values
                    .Where(a => a.AccountId == account.Id && a.BookedDay <= day)
                    .OrderBy(a => a.AuthorizationId, StringComparer.Ordinal)
                    .Select(a => AuthorizationAsOf(a, day))
                    .ToList();

                var errors = _errors
                    .Where(e => e.AccountId == account.Id && e.Day == day)
                    .ToList();

                var dailyInterest = interest.Single(x => x.AccountId == account.Id && x.Day == day).Amount;
                result.Add(new DaySnapshot(
                    account.Id,
                    day,
                    CalculateLedgerBalance(account.Id, day),
                    SumMoney(fees, account.Currency),
                    auths,
                    errors,
                    dailyInterest));
            }
        }

        return result;
    }

    private Authorization AuthorizationAsOf(Authorization auth, int day)
    {
        if (auth.Status == AuthorizationStatus.Settled && auth.SettledDay is int settledDay && settledDay <= day)
            return auth;

        return auth with { Status = auth.Status == AuthorizationStatus.Rejected ? AuthorizationStatus.Rejected : AuthorizationStatus.Approved };
    }

    private decimal CalculateActiveHolds(string accountId, int day)
    {
        return _authorizations.Values
            .Where(a => a.AccountId == accountId && a.BookedDay <= day && a.Status == AuthorizationStatus.Approved)
            .Where(a => a.SettledDay is null || a.SettledDay > day)
            .Sum(a => a.Hold.Amount);
    }

    private Account GetAccount(string accountId)
    {
        if (!_accounts.TryGetValue(accountId, out var account))
            throw new InvalidOperationException($"Unknown account {accountId}.");
        return account;
    }

    private static void ValidateCurrency(Money money, Currency expected)
    {
        if (money.Currency != expected)
            throw new InvalidOperationException("Currency mismatch.");
    }

    private static void ValidateDay(int day)
    {
        if (day is < FirstDay or > LastDay)
            throw new InvalidOperationException("Day must be between Day 1 and Day 6.");
    }

    private static Money SumMoney(IEnumerable<Money> amounts, Currency currency)
    {
        return new Money(currency, amounts.Sum(x => x.Amount));
    }

    private void Append(LedgerEntry entry)
    {
        _entries.Add(entry with { Amount = entry.Amount });
    }

    private void AddError(LedgerEvent evt, string code, string message)
    {
        _errors.Add(new ProcessingError(evt.AccountId, evt.BookedDay, evt.EventId, code, message));
    }
}
