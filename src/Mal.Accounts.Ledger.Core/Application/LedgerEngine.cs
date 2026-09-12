using Mal.Accounts.Ledger.Core.Domain;

namespace Mal.Accounts.Ledger.Core.Application;

public sealed class LedgerEngine
{
    public const decimal DailyInterestRate = 0.0004m; // 0.04%
    public static readonly Money OverdraftFeeAed = new(Currency.AED, 25.00m);

    private readonly Dictionary<string, Account> _accounts;
    private readonly List<LedgerEntry> _entries = [];
    private readonly Dictionary<string, Authorization> _authorizations = [];
    private readonly List<AuthorizationStateChange> _authorizationHistory = [];
    private readonly List<ProcessingError> _errors = [];
    private readonly List<FeeAssessment> _fees = [];
    private readonly List<DailyInterestAccrual> _interestAccruals = [];

    public LedgerEngine(IEnumerable<Account> accounts)
    {
        _accounts = accounts.ToDictionary(a => a.Id, StringComparer.Ordinal);
    }

    public ReplayResult Replay(IEnumerable<LedgerEvent> events, int finalDay = 6)
    {
        foreach (var @event in events)
            Process(@event);

        CapitalizeInterest(finalDay);
        var snapshots = BuildSnapshots(finalDay);

        return new ReplayResult(
            _entries.ToArray(),
            _authorizations.Values.OrderBy(a => a.AuthorizationId).ToArray(),
            _errors.ToArray(),
            _fees.ToArray(),
            _interestAccruals.ToArray(),
            snapshots);
    }

    private void Process(LedgerEvent @event)
    {
        if (!_accounts.TryGetValue(@event.AccountId, out var account))
        {
            AddError(@event, "UNKNOWN_ACCOUNT", $"Account '{@event.AccountId}' does not exist.");
            return;
        }

        if (@event.ValueDay < 1 || @event.ValueDay > 6 || @event.BookedDay < 1 || @event.BookedDay > 6)
        {
            AddError(@event, "INVALID_DAY", "Booked day and value day must be within Day 1 through Day 6.");
            return;
        }

        switch (@event)
        {
            case CreditEvent e:
                AddEntry(e.EventId, e.AccountId, LedgerEntryType.Credit, e.Amount, e.ValueDay);
                break;

            case DebitEvent e:
                AddEntry(e.EventId, e.AccountId, LedgerEntryType.Debit, e.Amount, e.ValueDay);
                break;

            case AuthorizationEvent e:
                ProcessAuthorization(account, e);
                break;

            case SettlementEvent e:
                ProcessSettlement(account, e);
                break;

            case ReversalEvent e:
                ProcessReversal(account, e);
                break;

            case InstallmentCreditEvent e:
                ProcessInstallmentCredit(account, e);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(@event), @event.GetType(), "Unsupported event type.");
        }
    }

    private void ProcessAuthorization(Account account, AuthorizationEvent @event)
    {
        if (_authorizations.ContainsKey(@event.AuthorizationId))
        {
            AddError(@event, "DUPLICATE_AUTHORIZATION", $"Authorization '{@event.AuthorizationId}' already exists.");
            return;
        }

        var ledgerBalance = GetLedgerBalance(account.Id, @event.ValueDay);
        var availableAfterHold = ledgerBalance.Subtract(@event.HoldAmount);

        if (availableAfterHold.Amount >= 0m)
        {
            var authorization = new Authorization(@event.AuthorizationId, account.Id, @event.HoldAmount,
                @event.BookedDay, AuthorizationState.Approved);
            _authorizations.Add(@event.AuthorizationId, authorization);
            _authorizationHistory.Add(new AuthorizationStateChange(
                authorization.AuthorizationId, authorization.AccountId, authorization.HoldAmount,
                @event.BookedDay, authorization.State));
        }
        else
        {
            var reason = $"Available balance after hold would be {availableAfterHold}.";
            var authorization = new Authorization(@event.AuthorizationId, account.Id, @event.HoldAmount,
                @event.BookedDay, AuthorizationState.Rejected, reason);
            _authorizations.Add(@event.AuthorizationId, authorization);
            _authorizationHistory.Add(new AuthorizationStateChange(
                authorization.AuthorizationId, authorization.AccountId, authorization.HoldAmount,
                @event.BookedDay, authorization.State, reason));
            AddError(@event, "AUTHORIZATION_DECLINED",
                $"Authorization '{@event.AuthorizationId}' rejected because available balance after hold is {availableAfterHold}.");
        }
    }

    private void ProcessSettlement(Account account, SettlementEvent @event)
    {
        if (!_authorizations.TryGetValue(@event.AuthorizationId, out var authorization))
        {
            AddError(@event, "UNKNOWN_AUTHORIZATION",
                $"Settlement references authorization '{@event.AuthorizationId}', which does not exist.");
            return;
        }

        if (authorization.State != AuthorizationState.Approved)
        {
            AddError(@event, "INVALID_AUTHORIZATION_STATE",
                $"Authorization '{@event.AuthorizationId}' is {authorization.State} and cannot be settled.");
            return;
        }

        if (authorization.AccountId != account.Id)
        {
            AddError(@event, "AUTHORIZATION_ACCOUNT_MISMATCH",
                $"Authorization '{@event.AuthorizationId}' belongs to account '{authorization.AccountId}'.");
            return;
        }

        if (authorization.HoldAmount.Currency != @event.Amount.Currency)
        {
            AddError(@event, "CURRENCY_MISMATCH", "Settlement currency does not match the authorization currency.");
            return;
        }

        if (@event.Amount.Amount > authorization.HoldAmount.Amount)
        {
            AddError(@event, "SETTLEMENT_EXCEEDS_HOLD",
                $"Settlement {@event.Amount} exceeds the approved hold {authorization.HoldAmount}.");
            return;
        }

        AddEntry(@event.EventId, @event.AccountId, LedgerEntryType.Debit, @event.Amount, @event.ValueDay);
        var settled = authorization with { State = AuthorizationState.Settled, RejectionReason = null };
        _authorizations[@event.AuthorizationId] = settled;
        _authorizationHistory.Add(new AuthorizationStateChange(
            settled.AuthorizationId, settled.AccountId, settled.HoldAmount,
            @event.BookedDay, settled.State));
    }

    private void ProcessReversal(Account account, ReversalEvent @event)
    {
        var original = _entries.FirstOrDefault(e => e.SourceEventId == @event.ReversesEventId && e.AccountId == account.Id);

        if (original is null)
        {
            AddError(@event, "UNKNOWN_REVERSAL_TARGET",
                $"Reversal references event '{@event.ReversesEventId}', which has no ledger entry for this account.");
            return;
        }

        if (_entries.Any(e => e.ReversalOfEntryId == original.EntryId))
        {
            AddError(@event, "ALREADY_REVERSED",
                $"Ledger entry '{original.EntryId}' has already been reversed.");
            return;
        }

        var reversalType = original.Type switch
        {
            LedgerEntryType.Debit => LedgerEntryType.Credit,
            LedgerEntryType.Credit => LedgerEntryType.Debit,
            _ => throw new InvalidOperationException($"Entry type {original.Type} cannot be reversed by this event.")
        };

        AddEntry(@event.EventId, @event.AccountId, reversalType, original.Amount, @event.ValueDay,
            original.EntryId);
    }

    private void ProcessInstallmentCredit(Account account, InstallmentCreditEvent @event)
    {
        if (@event.InstallmentCount <= 0)
        {
            AddError(@event, "INVALID_INSTALLMENT_COUNT", "Installment count must be greater than zero.");
            return;
        }

        var scale = CurrencyRules.Scale(account.Currency);
        var quantum = 1m / Pow10(scale);
        var baseAmount = decimal.Round(@event.TotalAmount.Amount / @event.InstallmentCount, scale, MidpointRounding.AwayFromZero);
        var allocated = 0m;

        for (var i = 1; i <= @event.InstallmentCount; i++)
        {
            var amount = i == @event.InstallmentCount
                ? decimal.Round(@event.TotalAmount.Amount - allocated, scale, MidpointRounding.AwayFromZero)
                : baseAmount;

            if (amount < 0m || Math.Abs(amount - baseAmount) > quantum)
                throw new InvalidOperationException("Installment allocation exceeded the one-quantum rounding remainder policy.");

            allocated += amount;
            AddEntry($"{@event.EventId}-I{i}", @event.AccountId, LedgerEntryType.Credit,
                new Money(account.Currency, amount), @event.ValueDay);
        }

        if (allocated != @event.TotalAmount.Amount)
            throw new InvalidOperationException("Installment allocation did not reconcile to the source amount.");
    }

    private void AddEntry(string sourceEventId, string accountId, LedgerEntryType type, Money amount, int valueDay,
        string? reversalOfEntryId = null)
    {
        var account = _accounts[accountId];
        if (amount.Currency != account.Currency)
            throw new InvalidOperationException($"Entry currency {amount.Currency} does not match account currency {account.Currency}.");

        var signed = type is LedgerEntryType.Debit or LedgerEntryType.OverdraftFee ? amount.Negate() : amount;
        var entryId = $"L{_entries.Count + 1:000}";
        _entries.Add(new LedgerEntry(entryId, accountId, type, signed, valueDay, sourceEventId, reversalOfEntryId));

        // The supplied criterion says E7 causes one fee on its value date. Therefore
        // a negative historical day is assessed only when a new event makes that
        // event's own value date negative; later days are not retroactively charged.
        if (type is LedgerEntryType.Debit or LedgerEntryType.Credit)
            AssessOverdraftFeeForValueDay(accountId, valueDay, sourceEventId);
    }

    private void AssessOverdraftFeeForValueDay(string accountId, int valueDay, string triggerEventId)
    {
        if (_fees.Any(f => f.AccountId == accountId && f.AssessmentDay == valueDay))
            return;

        var account = _accounts[accountId];
        var balanceBeforeFee = GetLedgerBalance(accountId, valueDay);
        if (balanceBeforeFee.Amount >= 0m)
            return;

        if (account.Currency != Currency.AED)
        {
            AddError(new SyntheticEvent(triggerEventId, valueDay, accountId, valueDay),
                "UNSUPPORTED_OVERDRAFT_FEE_CURRENCY", "The supplied overdraft fee is AED-only; no BHD fee amount was specified.");
            return;
        }

        var feeId = $"F{_fees.Count + 1:000}";
        var fee = new FeeAssessment(feeId, accountId, valueDay, OverdraftFeeAed, triggerEventId);
        _fees.Add(fee);
        AddEntry(feeId, accountId, LedgerEntryType.OverdraftFee, OverdraftFeeAed, valueDay);
    }

    private void CapitalizeInterest(int finalDay)
    {
        foreach (var account in _accounts.Values)
        {
            var accruals = new List<DailyInterestAccrual>();
            for (var day = 1; day <= finalDay; day++)
            {
                var closing = GetLedgerBalance(account.Id, day);
                var raw = closing.Amount > 0m ? closing.Amount * DailyInterestRate : 0m;
                var rounded = decimal.Round(raw, CurrencyRules.Scale(account.Currency), MidpointRounding.AwayFromZero);
                accruals.Add(new DailyInterestAccrual(account.Id, day, closing,
                    new Money(account.Currency, rounded)));
            }

            _interestAccruals.AddRange(accruals);
            var total = accruals.Aggregate(0m, (sum, a) => sum + a.AccruedInterest.Amount);
            if (total != 0m)
            {
                AddEntry($"INTEREST-DAY-{finalDay}", account.Id, LedgerEntryType.InterestCapitalization,
                    new Money(account.Currency, total), finalDay);
            }
        }
    }

    private IReadOnlyList<DailySnapshot> BuildSnapshots(int finalDay)
    {
        var snapshots = new List<DailySnapshot>();
        foreach (var account in _accounts.Values.OrderBy(a => a.Id))
        {
            for (var day = 1; day <= finalDay; day++)
            {
                var fees = _fees.Where(f => f.AccountId == account.Id && f.AssessmentDay == day).ToArray();
                var auths = _authorizationHistory
                    .Where(a => a.AccountId == account.Id && a.Day <= day)
                    .GroupBy(a => a.AuthorizationId)
                    .Select(g =>
                    {
                        var latest = g.OrderBy(x => x.Day).Last();
                        return new Authorization(latest.AuthorizationId, latest.AccountId, latest.HoldAmount,
                            g.Min(x => x.Day), latest.State, latest.Reason);
                    })
                    .OrderBy(a => a.AuthorizationId)
                    .ToArray();
                var errors = _errors.Where(e => e.AccountId == account.Id && e.Day == day).ToArray();
                var interest = _interestAccruals.Where(i => i.AccountId == account.Id && i.Day == day).ToArray();

                snapshots.Add(new DailySnapshot(account.Id, account.Currency, day,
                    GetLedgerBalance(account.Id, day), fees, auths, errors, interest));
            }
        }
        return snapshots;
    }

    private Money GetLedgerBalance(string accountId, int day)
    {
        var account = _accounts[accountId];
        var total = account.OpeningBalance.Amount;
        foreach (var entry in _entries.Where(e => e.AccountId == accountId && e.ValueDay <= day))
            total += entry.Amount.Amount;
        return new Money(account.Currency, total);
    }

    private void AddError(LedgerEvent @event, string code, string message) =>
        _errors.Add(new ProcessingError(@event.BookedDay, @event.EventId, @event.AccountId, code, message));

    private static decimal Pow10(int exponent)
    {
        var result = 1m;
        for (var i = 0; i < exponent; i++) result *= 10m;
        return result;
    }

    private sealed record SyntheticEvent(string EventId, int BookedDay, string AccountId, int ValueDay)
        : LedgerEvent(EventId, BookedDay, AccountId, ValueDay);
}
