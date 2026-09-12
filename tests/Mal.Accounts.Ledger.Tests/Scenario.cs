using Mal.Accounts.Ledger.Core.Domain;

namespace Mal.Accounts.Ledger.Tests;

internal static class Scenario
{
    public static ReplayResult Run() => CreateEngine().Replay(Events());

    public static Mal.Accounts.Ledger.Core.Application.LedgerEngine CreateEngine() =>
        new([
            new Account("ACC-001", Currency.Aed, new Money(Currency.Aed, 0m)),
            new Account("ACC-002", Currency.Bhd, new Money(Currency.Bhd, 0m))]);

    public static IReadOnlyList<LedgerEvent> EventsThrough(string eventId)
    {
        var events = Events();
        var index = events.Select((evt, i) => (evt, i))
            .Single(x => x.evt.EventId == eventId).i;
        return events.Take(index + 1).ToArray();
    }

    public static IReadOnlyList<LedgerEvent> Events() => [
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
    ];

    public static DaySnapshot Day(ReplayResult result, string accountId, int day) =>
        result.DailySnapshots.Single(x => x.AccountId == accountId && x.Day == day);
}
