using Mal.Accounts.Ledger.Core.Application;
using Mal.Accounts.Ledger.Core.Domain;

<<<<<<< HEAD
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

    var openingBalance = accountId == "ACC-001"
        ? new Money(Currency.Aed, 0m)
        : new Money(Currency.Bhd, 0m);
    Console.WriteLine($"OPENING BALANCE          : {openingBalance}");
    Console.WriteLine();

    foreach (var day in result.DailySnapshots.Where(x => x.AccountId == accountId))
    {
        Console.WriteLine($"DAY {day.Day}");
        Console.WriteLine($"  Closing Ledger Balance : {day.ClosingLedgerBalance}");

        var dayEntries = result.LedgerEntries
            .Where(e => e.AccountId == accountId && e.ValueDay == day.Day)
            .ToList();
        Console.WriteLine("  Ledger Entries         :");
        if (dayEntries.Count == 0)
        {
            Console.WriteLine("    -");
        }
        else
        {
            foreach (var entry in dayEntries)
            {
                var relation = entry.ReversalOfEntryId is null
                    ? string.Empty
                    : $" reverses={entry.ReversalOfEntryId}";
                Console.WriteLine(
                    $"    {entry.EntryId}: {entry.Type} {entry.Amount}{relation}");
            }
        }

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
=======
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
>>>>>>> origin/main
