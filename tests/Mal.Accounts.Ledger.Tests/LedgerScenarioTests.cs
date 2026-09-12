namespace Mal.Accounts.Ledger.Tests;

public sealed class LedgerScenarioTests
{
    private static ReplayResult Replay()
    {
        var accounts = new[]
        {
            new Account("ACC-001", Currency.AED, new Money(Currency.AED, 0m)),
            new Account("ACC-002", Currency.BHD, new Money(Currency.BHD, 0m))
        };

        var events = new LedgerEvent[]
        {
            new CreditEvent("E1", 1, "ACC-001", new Money(Currency.AED, 1200m), 1),
            new DebitEvent("E2", 1, "ACC-001", new Money(Currency.AED, 950m), 1),
            new AuthorizationEvent("E3", 2, "ACC-001", "Auth-A", new Money(Currency.AED, 200m), 2),
            new CreditEvent("E4", 3, "ACC-001", new Money(Currency.AED, 400m), 3),
            new SettlementEvent("E5", 4, "ACC-001", "Auth-A", new Money(Currency.AED, 185m), 4),
            new SettlementEvent("E6", 4, "ACC-001", "Auth-Z", new Money(Currency.AED, 180m), 4),
            new DebitEvent("E7", 5, "ACC-001", new Money(Currency.AED, 620m), 2),
            new AuthorizationEvent("E8", 5, "ACC-001", "Auth-B", new Money(Currency.AED, 90m), 5),
            new ReversalEvent("E9", 6, "ACC-001", "E7", 2),
            new InstallmentCreditEvent("E10", 5, "ACC-002", new Money(Currency.BHD, 10m), 3, 5)
        };

        return new LedgerEngine(accounts).Replay(events, 6);
    }

    [Fact]
    public void Day2_pre_fee_balance_is_negative_370_and_one_fee_is_assessed()
    {
        var result = Replay();
        var day2 = result.Snapshots.Single(s => s.AccountId == "ACC-001" && s.Day == 2);

        Assert.Equal(-395.00m, day2.ClosingLedgerBalance.Amount); // Includes the booked Day-2 fee.
        Assert.Single(day2.Fees);
        Assert.Equal(25.00m, day2.Fees[0].Amount.Amount);

        var entriesThroughDay2 = result.Entries
            .Where(e => e.AccountId == "ACC-001" && e.ValueDay <= 2)
            .Sum(e => e.Amount.Amount);
        Assert.Equal(-395.00m, entriesThroughDay2);
    }

    [Fact]
    public void Final_historical_AED_balances_use_value_dates()
    {
        var result = Replay();
        var balances = result.Snapshots
            .Where(s => s.AccountId == "ACC-001")
            .OrderBy(s => s.Day)
            .Select(s => s.ClosingLedgerBalance.Amount)
            .ToArray();

        Assert.Equal(new[] { 250m, 225m, 625m, 440m, 440m, 1060m }, balances);
    }

    [Fact]
    public void AuthA_is_approved_then_settled_without_debiting_the_hold_amount()
    {
        var result = Replay();
        var authA = result.Authorizations.Single(a => a.AuthorizationId == "Auth-A");

        Assert.Equal(AuthorizationState.Settled, authA.State);
        Assert.Contains(result.Entries, e => e.SourceEventId == "E5" && e.Amount.Amount == -185.00m);
        Assert.DoesNotContain(result.Entries, e => e.SourceEventId == "E5" && e.Amount.Amount == -200.00m);
    }

    [Fact]
    public void Authorization_snapshots_reflect_state_as_of_each_day()
    {
        var result = Replay();
        var day2 = result.Snapshots.Single(s => s.AccountId == "ACC-001" && s.Day == 2);
        var day4 = result.Snapshots.Single(s => s.AccountId == "ACC-001" && s.Day == 4);
        var day5 = result.Snapshots.Single(s => s.AccountId == "ACC-001" && s.Day == 5);

        Assert.Equal(AuthorizationState.Approved, day2.Authorizations.Single(a => a.AuthorizationId == "Auth-A").State);
        Assert.Equal(AuthorizationState.Settled, day4.Authorizations.Single(a => a.AuthorizationId == "Auth-A").State);
        Assert.DoesNotContain(day4.Authorizations, a => a.AuthorizationId == "Auth-Z");
        Assert.Equal(AuthorizationState.Rejected, day5.Authorizations.Single(a => a.AuthorizationId == "Auth-B").State);
    }

    [Fact]
    public void Unknown_settlement_is_rejected_and_creates_no_ledger_entry()
    {
        var result = Replay();
        var error = Assert.Single(result.Errors.Where(e => e.EventId == "E6"));

        Assert.Equal("UNKNOWN_AUTHORIZATION", error.Code);
        Assert.DoesNotContain(result.Entries, e => e.SourceEventId == "E6");
    }

    [Fact]
    public void AuthB_is_rejected_because_hold_would_make_available_balance_negative()
    {
        var result = Replay();
        var authB = result.Authorizations.Single(a => a.AuthorizationId == "Auth-B");

        Assert.Equal(AuthorizationState.Rejected, authB.State);
        Assert.DoesNotContain(result.Entries, e => e.SourceEventId == "E8");
        Assert.Contains(result.Errors, e => e.EventId == "E8" && e.Code == "AUTHORIZATION_DECLINED");
    }

    [Fact]
    public void E9_reverses_E7_but_does_not_remove_the_fee()
    {
        var result = Replay();

        var e7 = result.Entries.Single(e => e.SourceEventId == "E7");
        var e9 = result.Entries.Single(e => e.SourceEventId == "E9");
        Assert.Equal(-620.00m, e7.Amount.Amount);
        Assert.Equal(620.00m, e9.Amount.Amount);
        Assert.Equal(e7.EntryId, e9.ReversalOfEntryId);

        Assert.Single(result.FeeAssessments.Where(f => f.AccountId == "ACC-001" && f.AssessmentDay == 2));
    }

    [Fact]
    public void Bhd_installments_reconcile_exactly_to_ten_bhd()
    {
        var result = Replay();
        var installments = result.Entries.Where(e => e.SourceEventId == "E10").Select(e => e.Amount.Amount).ToArray();

        Assert.Equal(new[] { 3.333m, 3.333m, 3.334m }, installments);
        Assert.Equal(10.000m, installments.Sum());
    }

    [Fact]
    public void Interest_is_positive_balance_only_and_rounded_daily()
    {
        var result = Replay();
        var aed = result.InterestAccruals.Where(i => i.AccountId == "ACC-001").ToArray();
        var bhd = result.InterestAccruals.Where(i => i.AccountId == "ACC-002").ToArray();

        Assert.Equal(new[] { 0.10m, 0.09m, 0.25m, 0.18m, 0.18m, 0.42m },
            aed.Select(i => i.AccruedInterest.Amount));
        Assert.Equal(1.22m, aed.Sum(i => i.AccruedInterest.Amount));

        Assert.Equal(new[] { 0.000m, 0.000m, 0.000m, 0.000m, 0.004m, 0.004m },
            bhd.Select(i => i.AccruedInterest.Amount));
        Assert.Equal(0.008m, bhd.Sum(i => i.AccruedInterest.Amount));
    }

    [Fact]
    public void Day6_capitalization_is_a_single_entry_per_account()
    {
        var result = Replay();
        var aedInterest = result.Entries.Where(e => e.AccountId == "ACC-001" && e.Type == LedgerEntryType.InterestCapitalization).ToArray();
        var bhdInterest = result.Entries.Where(e => e.AccountId == "ACC-002" && e.Type == LedgerEntryType.InterestCapitalization).ToArray();

        Assert.Single(aedInterest);
        Assert.Equal(1.22m, aedInterest[0].Amount.Amount);
        Assert.Equal(6, aedInterest[0].ValueDay);

        Assert.Single(bhdInterest);
        Assert.Equal(0.008m, bhdInterest[0].Amount.Amount);
        Assert.Equal(6, bhdInterest[0].ValueDay);
    }

    [Fact]
    public void Account_record_requires_matching_currency()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Account("ACC-X", Currency.AED, new Money(Currency.BHD, 1m)));

        Assert.Contains("Opening balance currency", ex.Message);
    }

    [Fact]
    public void INTENTIONAL_FAILURE_rejected_criterion_E9_does_not_restore_fee()
    {
        var result = Replay();
        var fees = result.FeeAssessments.Where(f => f.AccountId == "ACC-001").Sum(f => f.Amount.Amount);

        // INTENTIONALLY FAILING TEST.
        // This encodes rejected acceptance criterion #6. The specification's
        // append-only rule means E9 can reverse E7's transaction, but cannot
        // erase a separately booked overdraft fee.
        Assert.Equal(0.00m, fees);
    }
}
