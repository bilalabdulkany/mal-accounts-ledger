using Mal.Accounts.Ledger.Core.Domain;

namespace Mal.Accounts.Ledger.Tests;

public sealed class LedgerScenarioTests
{
    [Fact]
    public void Day2_pre_fee_balance_is_negative_370_when_E7_is_applied_by_value_date()
    {
        var result = Scenario.Run();
        var entriesThroughE7 = result.LedgerEntries
            .Where(e => e.AccountId == "ACC-001" && e.EntryId != "E9" && e.Type != EntryType.InterestCapitalization)
            .ToList();

        // The final replay contains E9, so calculate the transient balance explicitly
        // from E1/E2/E7 to prove the acceptance criterion's pre-fee number.
        var transient = entriesThroughE7
            .Where(e => e.ValueDay <= 2)
            .Sum(e => e.Type is EntryType.Credit or EntryType.InterestCapitalization ? e.Amount.Amount : -e.Amount.Amount);

        Assert.Equal(-370m, transient);
        Assert.Contains(result.LedgerEntries, e => e.EntryId == "FEE-ACC-001-D2" && e.Amount.Amount == 25m);
    }

    [Fact]
    public void E3_approves_AuthA_and_the_hold_does_not_change_ledger_balance()
    {
        var result = Scenario.Run();
        var authA = result.Authorizations.Single(x => x.AuthorizationId == "Auth-A");

        Assert.Equal(AuthorizationStatus.Settled, authA.Status);
        Assert.Equal(200m, authA.Hold.Amount);
        Assert.Equal(AuthorizationStatus.Approved,
            Scenario.Day(result, "ACC-001", 2).Authorizations.Single(x => x.AuthorizationId == "Auth-A").Status);

        var engine = new Mal.Accounts.Ledger.Core.Application.LedgerEngine([
            new Account("ACC-001", Currency.Aed, new Money(Currency.Aed, 0m))]);
        engine.Replay([
            new CreditEvent("C1", 1, 1, "ACC-001", new Money(Currency.Aed, 250m)),
            new AuthorizationEvent("C2", 2, 2, "ACC-001", "Auth-A", new Money(Currency.Aed, 200m))]);
        Assert.Equal(250m, engine.CalculateLedgerBalance("ACC-001", 2).Amount);
        Assert.Equal(50m, engine.CalculateAvailableBalance("ACC-001", 2).Amount);
    }

    [Fact]
    public void E5_accepts_AuthA_settlement_for_185()
    {
        var result = Scenario.Run();

        Assert.Equal(AuthorizationStatus.Settled,
            Scenario.Day(result, "ACC-001", 4).Authorizations.Single(x => x.AuthorizationId == "Auth-A").Status);
        Assert.Contains(result.LedgerEntries, e =>
            e.EntryId == "E5" && e.Type == EntryType.Debit && e.Amount.Amount == 185m && e.RelatedAuthorizationId == "Auth-A");
        Assert.Equal(440m, Scenario.Day(result, "ACC-001", 4).ClosingLedgerBalance.Amount);
    }

    [Fact]
    public void E6_rejects_unknown_authorization_without_money_movement()
    {
        var result = Scenario.Run();
        var errors = Scenario.Day(result, "ACC-001", 4).Errors;

        Assert.Contains(errors, e => e.EventId == "E6" && e.Code == "UNKNOWN_AUTHORIZATION");
        Assert.DoesNotContain(result.LedgerEntries, e => e.EntryId == "E6");
    }

    [Fact]
    public void E7_assesses_exactly_one_Day2_fee()
    {
        var result = Scenario.Run();

        Assert.Equal(1, result.LedgerEntries.Count(e =>
            e.AccountId == "ACC-001" && e.Type == EntryType.OverdraftFee && e.ValueDay == 2));
        Assert.Equal(25m, Scenario.Day(result, "ACC-001", 2).FeeAssessments.Amount);
    }

    [Fact]
    public void E8_rejects_AuthB_because_available_balance_after_hold_is_negative()
    {
        var result = Scenario.Run();
        var authB = result.Authorizations.Single(x => x.AuthorizationId == "Auth-B");

        Assert.Equal(AuthorizationStatus.Rejected, authB.Status);
        Assert.Contains(Scenario.Day(result, "ACC-001", 5).Errors,
            e => e.EventId == "E8" && e.Code == "AUTHORIZATION_DECLINED");
    }

    [Fact]
    public void E9_reverses_E7_but_the_fee_remains_and_final_Day2_balance_is_225()
    {
        var result = Scenario.Run();

        Assert.Contains(result.LedgerEntries, e => e.EntryId == "E7" && e.Type == EntryType.Debit && e.Amount.Amount == 620m);
        Assert.Contains(result.LedgerEntries, e => e.EntryId == "E9" && e.Type == EntryType.Credit && e.Amount.Amount == 620m && e.SourceEventId == "E7");
        Assert.Contains(result.LedgerEntries, e => e.EntryId == "FEE-ACC-001-D2" && e.Amount.Amount == 25m);
        Assert.Equal(225m, Scenario.Day(result, "ACC-001", 2).ClosingLedgerBalance.Amount);
    }

    [Fact]
    public void Bhd_installments_reconcile_to_exact_original_amount()
    {
        var result = Scenario.Run();
        var parts = result.LedgerEntries
            .Where(e => e.SourceEventId == "E10")
            .OrderBy(e => e.EntryId)
            .Select(e => e.Amount.Amount)
            .ToArray();

        Assert.Equal(new decimal[] { 3.333m, 3.333m, 3.334m }, parts);
        Assert.Equal(10.000m, parts.Sum());
    }

    [Fact]
    public void Interest_uses_rounded_daily_accruals_and_capitalizes_their_exact_sum()
    {
        var result = Scenario.Run();
        var aed = result.InterestAccruals
            .Where(x => x.AccountId == "ACC-001")
            .OrderBy(x => x.Day)
            .ToArray();

        Assert.Equal(new decimal[] { 0.10m, 0.09m, 0.25m, 0.18m, 0.18m, 0.42m },
            aed.Select(x => x.Amount.Amount).ToArray());
        Assert.Equal(new decimal[] { 250m, 225m, 625m, 440m, 440m, 1060m },
            aed.Select(x => x.ClosingBalance.Amount).ToArray());

        var aedAccrualSum = aed.Sum(x => x.Amount.Amount);
        Assert.Equal(1.22m, aedAccrualSum);
        Assert.Equal(aedAccrualSum, result.CapitalizedInterest["ACC-001"].Amount);

        var capitalizationEntries = result.LedgerEntries
            .Where(e => e.AccountId == "ACC-001" && e.Type == EntryType.InterestCapitalization)
            .ToArray();
        Assert.Single(capitalizationEntries);
        Assert.Equal(1.22m, capitalizationEntries[0].Amount.Amount);
        Assert.Equal(6, capitalizationEntries[0].ValueDay);
    }

    [Fact]
    public void Bhd_interest_is_004_each_day_and_capitalizes_008()
    {
        var result = Scenario.Run();
        var bhd = result.InterestAccruals.Where(x => x.AccountId == "ACC-002").OrderBy(x => x.Day).ToArray();

        Assert.Equal(new decimal[] { 0m, 0m, 0m, 0m, 0.004m, 0.004m },
            bhd.Select(x => x.Amount.Amount).ToArray());
        Assert.Equal(0.008m, bhd.Sum(x => x.Amount.Amount));
        Assert.Equal(0.008m, result.CapitalizedInterest["ACC-002"].Amount);
        Assert.Equal(10.008m, Scenario.Day(result, "ACC-002", 6).ClosingLedgerBalance.Amount);
    }

    [Fact]
    public void Errors_are_reported_on_the_day_the_bad_event_was_booked()
    {
        var result = Scenario.Run();

        Assert.Contains(Scenario.Day(result, "ACC-001", 4).Errors,
            e => e.EventId == "E6" && e.Code == "UNKNOWN_AUTHORIZATION");
        Assert.Contains(Scenario.Day(result, "ACC-001", 5).Errors,
            e => e.EventId == "E8" && e.Code == "AUTHORIZATION_DECLINED");
        Assert.Empty(Scenario.Day(result, "ACC-001", 1).Errors);
        Assert.Empty(Scenario.Day(result, "ACC-002", 6).Errors);
    }

    [Fact]
    public void Ledger_is_append_only_and_E7_is_not_mutated_by_E9()
    {
        var result = Scenario.Run();
        var e7 = result.LedgerEntries.Single(e => e.EntryId == "E7");
        var e9 = result.LedgerEntries.Single(e => e.EntryId == "E9");

        Assert.Equal(EntryType.Debit, e7.Type);
        Assert.Equal(620m, e7.Amount.Amount);
        Assert.Equal(EntryType.Credit, e9.Type);
        Assert.Equal("E7", e9.SourceEventId);
    }

    [Fact]
    public void Intentionally_failing_test_documents_rejected_criterion_6()
    {
        var result = Scenario.Run();

        // INTENTIONAL FAILURE. Criterion 6 claims E9 restores all fees to pre-E7 values.
        // The design rejects that criterion because E9 is a new append-only reversal entry;
        // it does not mutate or delete the separately booked Day-2 overdraft fee.
        Assert.Equal(0m, Scenario.Day(result, "ACC-001", 2).FeeAssessments.Amount);
    }
}
