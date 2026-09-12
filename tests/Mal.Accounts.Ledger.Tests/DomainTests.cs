using Mal.Accounts.Ledger.Core.Domain;

namespace Mal.Accounts.Ledger.Tests;

public sealed class DomainTests
{
    [Fact]
    public void Account_record_constructs_without_duplicate_constructor()
    {
        var account = new Account("ACC-001", Currency.Aed, new Money(Currency.Aed, 0m));

        Assert.Equal("ACC-001", account.Id);
        Assert.Equal(Currency.Aed, account.Currency);
        Assert.Equal(0m, account.OpeningBalance.Amount);
    }

    [Fact]
    public void Money_rounds_to_currency_precision()
    {
        Assert.Equal(12.35m, new Money(Currency.Aed, 12.345m).Amount);
        Assert.Equal(12.346m, new Money(Currency.Bhd, 12.3456m).Amount);
    }

    [Fact]
    public void Account_rejects_mismatched_opening_currency()
    {
        Assert.Throws<ArgumentException>(() =>
            new Account("ACC-001", Currency.Aed, new Money(Currency.Bhd, 0m)));
    }
}
