namespace Mal.Accounts.Ledger.Core.Domain;

public enum Currency
{
    AED,
    BHD
}

public static class CurrencyRules
{
    public static int Scale(Currency currency) => currency switch
    {
        Currency.AED => 2,
        Currency.BHD => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, null)
    };
}

public readonly record struct Money
{
    public Currency Currency { get; }
    public decimal Amount { get; }

    public Money(Currency currency, decimal amount)
    {
        Currency = currency;
        Amount = decimal.Round(amount, CurrencyRules.Scale(currency), MidpointRounding.AwayFromZero);
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Currency, Amount + other.Amount);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Currency, Amount - other.Amount);
    }

    public Money Negate() => new(Currency, -Amount);

    public Money Multiply(decimal factor) => new(Currency, Amount * factor);

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Currency mismatch: {Currency} vs {other.Currency}.");
    }

    public override string ToString() => $"{Currency} {Amount.ToString($"F{CurrencyRules.Scale(Currency)}")}";
}

// Explicit record class syntax avoids the common compile error caused by trying to
// use a primary-constructor record while also declaring another constructor/property set.
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

public enum LedgerEntryType
{
    Credit,
    Debit,
    OverdraftFee,
    InterestCapitalization
}

public sealed record LedgerEntry(
    string EntryId,
    string AccountId,
    LedgerEntryType Type,
    Money Amount,
    int ValueDay,
    string SourceEventId,
    string? ReversalOfEntryId = null);

public abstract record LedgerEvent(
    string EventId,
    int BookedDay,
    string AccountId,
    int ValueDay);

public sealed record CreditEvent(
    string EventId,
    int BookedDay,
    string AccountId,
    Money Amount,
    int ValueDay)
    : LedgerEvent(EventId, BookedDay, AccountId, ValueDay);

public sealed record DebitEvent(
    string EventId,
    int BookedDay,
    string AccountId,
    Money Amount,
    int ValueDay)
    : LedgerEvent(EventId, BookedDay, AccountId, ValueDay);

public sealed record AuthorizationEvent(
    string EventId,
    int BookedDay,
    string AccountId,
    string AuthorizationId,
    Money HoldAmount,
    int ValueDay)
    : LedgerEvent(EventId, BookedDay, AccountId, ValueDay);

public sealed record SettlementEvent(
    string EventId,
    int BookedDay,
    string AccountId,
    string AuthorizationId,
    Money Amount,
    int ValueDay)
    : LedgerEvent(EventId, BookedDay, AccountId, ValueDay);

public sealed record ReversalEvent(
    string EventId,
    int BookedDay,
    string AccountId,
    string ReversesEventId,
    int ValueDay)
    : LedgerEvent(EventId, BookedDay, AccountId, ValueDay);

public sealed record InstallmentCreditEvent(
    string EventId,
    int BookedDay,
    string AccountId,
    Money TotalAmount,
    int InstallmentCount,
    int ValueDay)
    : LedgerEvent(EventId, BookedDay, AccountId, ValueDay);

public enum AuthorizationState
{
    Approved,
    Rejected,
    Settled
}

public sealed record Authorization(
    string AuthorizationId,
    string AccountId,
    Money HoldAmount,
    int BookedDay,
    AuthorizationState State,
    string? RejectionReason = null);

public sealed record AuthorizationStateChange(
    string AuthorizationId,
    string AccountId,
    Money HoldAmount,
    int Day,
    AuthorizationState State,
    string? Reason = null);

public sealed record ProcessingError(
    int Day,
    string EventId,
    string AccountId,
    string Code,
    string Message);

public sealed record FeeAssessment(
    string FeeId,
    string AccountId,
    int AssessmentDay,
    Money Amount,
    string TriggerEventId);

public sealed record DailyInterestAccrual(
    string AccountId,
    int Day,
    Money ClosingBalanceBeforeInterest,
    Money AccruedInterest);

public sealed record DailySnapshot(
    string AccountId,
    Currency Currency,
    int Day,
    Money ClosingLedgerBalance,
    IReadOnlyList<FeeAssessment> Fees,
    IReadOnlyList<Authorization> Authorizations,
    IReadOnlyList<ProcessingError> Errors,
    IReadOnlyList<DailyInterestAccrual> InterestAccruals);

public sealed record ReplayResult(
    IReadOnlyList<LedgerEntry> Entries,
    IReadOnlyList<Authorization> Authorizations,
    IReadOnlyList<ProcessingError> Errors,
    IReadOnlyList<FeeAssessment> FeeAssessments,
    IReadOnlyList<DailyInterestAccrual> InterestAccruals,
    IReadOnlyList<DailySnapshot> Snapshots);
