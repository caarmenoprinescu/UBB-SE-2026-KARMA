// <copyright file="PaymentCalculationServiceTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Globalization;
using KarmaBanking.App.Services;
using Xunit;

namespace KarmaBanking.App.Tests.Services;

public class PaymentCalculationServiceTests
{
    private readonly PaymentCalculationService service;

    public PaymentCalculationServiceTests()
    {
        this.service = new PaymentCalculationService();
    }

    // CalculatePaymentPreview Tests
    [Theory]
    // Standard payment
    [InlineData(100, 1000, 10, true, 0, 900, 9)]
    // Custom payment > 0 (exactly 2 months worth)
    [InlineData(100, 1000, 10, false, 200, 800, 8)]
    // Custom payment <= 0
    [InlineData(100, 1000, 10, false, 0, 1000, 10)]
    // Custom payment < 0 (adds to balance)
    [InlineData(100, 1000, 10, false, -50, 1050, 10)]
    // Custom payment that exceeds outstanding balance (caps at 0)
    [InlineData(100, 500, 10, false, 600, 0, 4)]
    // Custom payment that exceeds remaining months (caps at 0)
    [InlineData(100, 500, 2, false, 300, 200, 0)]
    public void CalculatePaymentPreview_ReturnsExpectedTuple(
        decimal monthlyInstallment,
        decimal outstandingBalance,
        int remainingMonths,
        bool isStandardPayment,
        decimal customPaymentAmount,
        decimal expectedBalance,
        int expectedRemainingMonths)
    {
        var result = this.service.CalculatePaymentPreview(
            monthlyInstallment, outstandingBalance, remainingMonths, isStandardPayment, customPaymentAmount);

        Assert.Equal(expectedBalance, result.BalanceAfterPayment);
        Assert.Equal(expectedRemainingMonths, result.RemainingMonths);
    }

    // ParsePaymentAmount Tests
    [Theory]
    // Null input should fail and return 0
    [InlineData(null, false, 0)]
    // Empty string should fail and return 0
    [InlineData("", false, 0)]
    // Whitespace-only string should fail and return 0
    [InlineData("   ", false, 0)]
    // Non-numeric text should fail and return 0
    [InlineData("invalid_input", false, 0)]
    public void ParsePaymentAmount_InvalidOrEmptyInput_ReturnsFalse(string input, bool expectedSuccess, decimal expectedAmount)
    {
        var result = this.service.ParsePaymentAmount(input);

        Assert.Equal(expectedSuccess, result.Success);
        Assert.Equal(expectedAmount, result.Amount);
    }

    [Fact]
    public void ParsePaymentAmount_ValidInput_ReturnsParsedAmount()
    {
        string input = 1234.56m.ToString(CultureInfo.CurrentCulture);

        var result = this.service.ParsePaymentAmount(input);

        Assert.True(result.Success);
        Assert.Equal(1234.56m, result.Amount);
    }

    [Fact]
    public void ParsePaymentAmount_InvariantInput_ReturnsParsedAmount()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            // Force a culture where '.' is invalid as both a decimal and thousands separator.
            // fr-FR uses ',' for decimal and space for thousands. This guarantees CurrentCulture TryParse fails,
            // correctly forcing the fallback to InvariantCulture.
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            string input = "1234.56";

            var result = this.service.ParsePaymentAmount(input);

            Assert.True(result.Success);
            Assert.Equal(1234.56m, result.Amount);
        }
        finally
        {
            // Restore original culture so we don't break other tests
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    // ValidatePaymentAmount Tests
    [Theory]
    [InlineData(0, 1000, false, "Payment amount must be greater than 0.")]
    [InlineData(-50, 1000, false, "Payment amount must be greater than 0.")]
    public void ValidatePaymentAmount_LessOrEqualZero_ReturnsFalse(decimal payment, decimal balance, bool expectedValid, string expectedMsg)
    {
        var result = this.service.ValidatePaymentAmount(payment, balance);
        Assert.Equal(expectedValid, result.IsValid);
        Assert.Equal(expectedMsg, result.ValidationMessage);
    }

    [Fact]
    public void ValidatePaymentAmount_ExceedsBalance_ReturnsFalse()
    {
        decimal payment = 1500m;
        decimal balance = 1000m;
        string expectedMsg = $"Payment amount cannot exceed outstanding balance of {balance:C2}.";

        var result = this.service.ValidatePaymentAmount(payment, balance);

        Assert.False(result.IsValid);
        Assert.Equal(expectedMsg, result.ValidationMessage);
    }

    [Fact]
    public void ValidatePaymentAmount_ValidAmount_ReturnsTrue()
    {
        var result = this.service.ValidatePaymentAmount(500m, 1000m);

        Assert.True(result.IsValid);
        Assert.Empty(result.ValidationMessage);
    }

    // GetInitialCustomAmount Tests
    [Theory]
    // Uses currentCustomAmount (capped to balance)
    [InlineData(100.0, 500.0, 600.0, 500.0)]
    // Uses currentCustomAmount (under balance)
    [InlineData(100.0, 500.0, 250.0, 250.0)]
    // Uses monthlyInstallment (capped to balance)
    [InlineData(600.0, 500.0, null, 500.0)]
    // Uses monthlyInstallment (under balance)
    [InlineData(100.0, 500.0, null, 100.0)]
    public void GetInitialCustomAmount_ReturnsExpectedValue(
        double monthlyInstallment,
        double outstandingBalance,
        double? currentCustomAmount,
        double expectedAmount)
    {
        var result = service.GetInitialCustomAmount(
            (decimal)monthlyInstallment,
            (decimal)outstandingBalance,
            currentCustomAmount);

        Assert.Equal((decimal)expectedAmount, result);
    }

    // FormatCustomAmount Tests
    [Fact]
    public void FormatCustomAmount_FormatsAccordingToCulture()
    {
        decimal amount = 1234.5678m;
        string expected = amount.ToString("0.##", CultureInfo.CurrentCulture);

        var result = this.service.FormatCustomAmount(amount);

        Assert.Equal(expected, result);
    }
}