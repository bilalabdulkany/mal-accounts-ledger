using Mal.Accounts.Ledger.Core.Application;
using Mal.Accounts.Ledger.Core.Domain;

var accounts = new[]
{
    new Account("ACC-001", Currency.AED, new Money(Currency.AED, 0.00m)),
    new Account("ACC-002", Currency.BHD, new Money(Currency.BHD, 0.000m))
};

var events = new LedgerEvent[]
{
    new CreditEvent("E1", 1, "ACC-001", new Money(Currency.AED, 1200.00m), 1),
    new DebitEvent("E2", 1, "ACC-001", new Money(Currency.AED, 950.00m), 1),
    new AuthorizationEvent("E3", 2, "ACC-001", "Auth-A", new Money(Currency.AED, 200.00m), 2),
    new CreditEvent("E4", 3, "ACC-001", new Money(Currency.AED, 400.00m), 3),
    new SettlementEvent("E5", 4, "ACC-001", "Auth-A", new Money(Currency.AED, 185.00m), 4),
    new SettlementEvent("E6", 4, "ACC-001", "Auth-Z", new Money(Currency.AED, 180.00m), 4),
    new DebitEvent("E7", 5, "ACC-001", new Money(Currency.AED, 620.00m), 2),
    new AuthorizationEvent("E8", 5, "ACC-001", "Auth-B", new Money(Currency.AED, 90.00m), 5),
    new ReversalEvent("E9", 6, "ACC-001", "E7", 2),
    new InstallmentCreditEvent("E10", 5, "ACC-002", new Money(Currency.BHD, 10.000m), 3, 5)
};

var result = new LedgerEngine(accounts).Replay(events, 6);

foreach (var account in accounts)
{
    Console.WriteLine(new string('=', 72));
    Console.WriteLine($"ACCOUNT {account.Id} ({account.Currency})");
    Console.WriteLine(new string('=', 72));

    foreach (var day in result.Snapshots.Where(s => s.AccountId == account.Id))
    {
        Console.WriteLine($"\nDAY {day.Day}");
        Console.WriteLine($"  Closing ledger balance : {day.ClosingLedgerBalance}");

        var fees = day.Fees.Count == 0
            ? "none"
            : string.Join(", ", day.Fees.Select(f => $"{f.Amount} (trigger {f.TriggerEventId})"));
        Console.WriteLine($"  Fee assessments       : {fees}");

        var auths = day.Authorizations.Count == 0
            ? "none"
            : string.Join("; ", day.Authorizations.Select(a =>
                a.RejectionReason is null
                    ? $"{a.AuthorizationId}={a.State}"
                    : $"{a.AuthorizationId}={a.State} ({a.RejectionReason})"));
        Console.WriteLine($"  Authorization states  : {auths}");

        var errors = day.Errors.Count == 0
            ? "none"
            : string.Join("; ", day.Errors.Select(e => $"{e.EventId} [{e.Code}] {e.Message}"));
        Console.WriteLine($"  Errors                : {errors}");

        var interest = day.InterestAccruals.Count == 0
            ? "none"
            : string.Join(", ", day.InterestAccruals.Select(i => i.AccruedInterest.ToString()));
        Console.WriteLine($"  Daily interest        : {interest}");
    }

    var accountInterest = result.InterestAccruals
        .Where(i => i.AccountId == account.Id)
        .Sum(i => i.AccruedInterest.Amount);
    Console.WriteLine($"\n  Capitalized interest  : {new Money(account.Currency, accountInterest)}");
}

Console.WriteLine("\nLEDGER ENTRIES (append-only history)");
foreach (var entry in result.Entries)
    Console.WriteLine($"  {entry.EntryId} | {entry.AccountId} | {entry.Type,-22} | {entry.Amount} | value Day {entry.ValueDay} | source {entry.SourceEventId}" +
                      (entry.ReversalOfEntryId is null ? "" : $" | reverses {entry.ReversalOfEntryId}"));
