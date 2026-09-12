namespace Mal.Accounts.Ledger.Core.Domain;

public enum Currency
{
    Aed,
    Bhd
}

public enum EntryType
{
    Credit,
    Debit,
    OverdraftFee,
    InterestCapitalization
}

public enum AuthorizationStatus
{
    Approved,
    Rejected,
    Settled
}

public sealed record Money
{
    public Currency Currency { get; }
    public decimal Amount { get; }

    public Money(Currency currency, decimal amount)
    {
        Currency = currency;
        Amount = decimal.Round(amount, ScaleFor(currency), MidpointRounding.AwayFromZero);
    }

    public int Scale => ScaleFor(Currency);

    public static int ScaleFor(Currency currency) => currency switch
    {
        Currency.Aed => 2,
        Currency.Bhd => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, null)
    };

    public Money Rounded() => this;

    public override string ToString() =>
        $"{CurrencyCode(Currency)} {Amount.ToString($"F{Scale}", System.Globalization.CultureInfo.InvariantCulture)}";

    public static string CurrencyCode(Currency currency) => currency switch
    {
        Currency.Aed => "AED",
        Currency.Bhd => "BHD",
        _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, null)
    };

    public static Money Zero(Currency currency) => new(currency, 0m);
}

public sealed record Account
{
    public string Id { get; }
    public Currency Currency { get; }
    public Money OpeningBalance { get; }

    public Account(string id, Currency currency, Money openingBalance)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Account id is required.", nameof(id));

        if (openingBalance.Currency != currency)
            throw new ArgumentException("Opening balance currency must match account currency.", nameof(openingBalance));

        Id = id;
        Currency = currency;
        OpeningBalance = openingBalance;
    }
}

public sealed record LedgerEntry(
    string EntryId,
    string AccountId,
    EntryType Type,
    Money Amount,
    int ValueDay,
    string? SourceEventId = null,
    string? RelatedAuthorizationId = null);

public sealed record Authorization(
    string AuthorizationId,
    string AccountId,
    Money Hold,
    int ValueDay,
    AuthorizationStatus Status,
    string EventId,
    int BookedDay,
    int? SettledDay);

public abstract record LedgerEvent(string EventId, int BookedDay, int ValueDay, string AccountId);

public sealed record CreditEvent(string EventId, int BookedDay, int ValueDay, string AccountId, Money Amount)
    : LedgerEvent(EventId, BookedDay, ValueDay, AccountId);

public sealed record DebitEvent(string EventId, int BookedDay, int ValueDay, string AccountId, Money Amount)
    : LedgerEvent(EventId, BookedDay, ValueDay, AccountId);

public sealed record AuthorizationEvent(string EventId, int BookedDay, int ValueDay, string AccountId, string AuthorizationId, Money Hold)
    : LedgerEvent(EventId, BookedDay, ValueDay, AccountId);

public sealed record SettlementEvent(string EventId, int BookedDay, int ValueDay, string AccountId, string AuthorizationId, Money Amount)
    : LedgerEvent(EventId, BookedDay, ValueDay, AccountId);

public sealed record ReversalEvent(string EventId, int BookedDay, int ValueDay, string AccountId, string ReversesEventId)
    : LedgerEvent(EventId, BookedDay, ValueDay, AccountId);

public sealed record InstallmentCreditEvent(string EventId, int BookedDay, int ValueDay, string AccountId, Money Total, int InstallmentCount)
    : LedgerEvent(EventId, BookedDay, ValueDay, AccountId);

public sealed record ProcessingError(string AccountId, int Day, string EventId, string Code, string Message);

public sealed record InterestAccrual(string AccountId, int Day, Money ClosingBalance, Money Amount);

public sealed record DaySnapshot(
    string AccountId,
    int Day,
    Money ClosingLedgerBalance,
    Money FeeAssessments,
    IReadOnlyList<Authorization> Authorizations,
    IReadOnlyList<ProcessingError> Errors,
    Money DailyInterest);

public sealed record ReplayResult(
    IReadOnlyList<LedgerEntry> LedgerEntries,
    IReadOnlyList<Authorization> Authorizations,
    IReadOnlyList<DaySnapshot> DailySnapshots,
    IReadOnlyList<InterestAccrual> InterestAccruals,
    IReadOnlyDictionary<string, Money> CapitalizedInterest);
