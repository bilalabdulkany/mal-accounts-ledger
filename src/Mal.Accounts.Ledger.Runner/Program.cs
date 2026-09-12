using Mal.Accounts.Ledger.Core.Application;
using Mal.Accounts.Ledger.Core.Domain;

var engine = new LedgerEngine([
    new Account("ACC-001", Currency.Aed, new Money(Currency.Aed, 0m)),
    new Account("ACC-002", Currency.Bhd, new Money(Currency.Bhd, 0m))]);

var events = new LedgerEvent[]
{
    new CreditEvent("E1", 1, 1, "ACC-001", new Money(Currency.Aed, 1200.00m)),
    new DebitEvent("E2", 1, 1, "ACC-001", new Money(Currency.Aed, 950.00m)),
    new AuthorizationEvent("E3", 2, 2, "ACC-001", "Auth-A", new Money(Currency.Aed, 200.00m)),
    new CreditEvent("E4", 3, 3, "ACC-001", new Money(Currency.Aed, 400.00m)),
    new SettlementEvent("E5", 4, 4, "ACC-001", "Auth-A", new Money(Currency.Aed, 185.00m)),
    new SettlementEvent("E6", 4, 4, "ACC-001", "Auth-Z", new Money(Currency.Aed, 180.00m)),
    new DebitEvent("E7", 5, 2, "ACC-001", new Money(Currency.Aed, 620.00m)),
    new AuthorizationEvent("E8", 5, 5, "ACC-001", "Auth-B", new Money(Currency.Aed, 90.00m)),
    new ReversalEvent("E9", 6, 2, "ACC-001", "E7"),
    new InstallmentCreditEvent("E10", 5, 5, "ACC-002", new Money(Currency.Bhd, 10.000m), 3)
};

var result = engine.Replay(events);

foreach (var accountId in new[] { "ACC-001", "ACC-002" })
{
    Console.WriteLine(new string('=', 68));
    Console.WriteLine($"ACCOUNT: {accountId}");
    Console.WriteLine(new string('=', 68));

    foreach (var day in result.DailySnapshots.Where(x => x.AccountId == accountId))
    {
        Console.WriteLine($"DAY {day.Day}");
        Console.WriteLine($"  Closing Ledger Balance : {day.ClosingLedgerBalance}");
        Console.WriteLine($"  Fee Assessments        : {day.FeeAssessments}");
        Console.WriteLine($"  Daily Interest         : {day.DailyInterest}");
        Console.WriteLine($"  Authorizations         : {(day.Authorizations.Count == 0 ? "-" : string.Join(", ", day.Authorizations.Select(a => $"{a.AuthorizationId}={a.Status}")))}");
        Console.WriteLine($"  Errors                 : {(day.Errors.Count == 0 ? "-" : string.Join(" | ", day.Errors.Select(e => $"{e.EventId} [{e.Code}] {e.Message}")))}");
        Console.WriteLine();
    }

    Console.WriteLine($"Capitalized interest Day 6: {result.CapitalizedInterest[accountId]}");
    Console.WriteLine();
}

Console.WriteLine("APPEND-ONLY LEDGER ENTRIES");
Console.WriteLine(new string('-', 68));
foreach (var entry in result.LedgerEntries)
{
    Console.WriteLine(
        $"{entry.EntryId,-24} {entry.AccountId,-8} {entry.Type,-24} {entry.Amount,-12} valueDay={entry.ValueDay} source={entry.SourceEventId ?? "-"}");
}
