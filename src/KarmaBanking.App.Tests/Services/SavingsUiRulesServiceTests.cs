// <copyright file="SavingsUiRulesServiceTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using KarmaBanking.App.Models;
using KarmaBanking.App.Models.Enums;
using KarmaBanking.App.Services;
using Xunit;

namespace KarmaBanking.App.Tests.Services;

public class SavingsUiRulesServiceTests
{
    private readonly SavingsUiRulesService service;

    public SavingsUiRulesServiceTests()
    {
        this.service = new SavingsUiRulesService();
    }

    // TryParsePositiveAmount Tests
    [Theory]
    [InlineData("150.75", true, 150.75)] // Valid positive
    [InlineData("0", false, 0)] // Zero is invalid based on logic
    [InlineData("-50", false, 0)]// Negative is invalid
    [InlineData("invalid", false, 0)]// Text is invalid
    [InlineData(null, false, 0)] // Null check
    public void TryParsePositiveAmount_ReturnsExpectedResult(string input, bool expectedSuccess, double expectedAmountDouble)
    {
        var result = this.service.TryParsePositiveAmount(input, out var amount);

        Assert.Equal(expectedSuccess, result);
        Assert.Equal((decimal)expectedAmountDouble, amount);
    }

    // BuildDepositPreview Tests
    [Fact]
    public void BuildDepositPreview_NullAccount_ReturnsEmpty()
    {
        var result = this.service.BuildDepositPreview("100", null);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildDepositPreview_InvalidAmount_ReturnsEmpty()
    {
        var account = new SavingsAccount { Balance = 500m };

        var result = this.service.BuildDepositPreview("-50", account); // Negative amount is invalid

        Assert.Empty(result);
    }

    [Fact]
    public void BuildDepositPreview_ValidInput_ReturnsFormattedString()
    {
        var account = new SavingsAccount { Balance = 500m };
        string expected = $"New balance will be: ${650.50m:N2}"; // 500 + 150.50

        var result = this.service.BuildDepositPreview("150.50", account);

        Assert.Equal(expected, result);
    }

    // CalculateWithdrawNetAmount Tests
    [Fact]
    public void CalculateWithdrawNetAmount_CalculatesCorrectly()
    {
        var result = this.service.CalculateWithdrawNetAmount(500m, 15.50m);

        Assert.Equal(484.50m, result);
    }

    // TryParseDepositFrequency Tests
    [Fact]
    public void TryParseDepositFrequency_InvalidEnumString_ReturnsFalse()
    {
        var result = this.service.TryParseDepositFrequency("DefinitelyNotAnEnumMember", out var frequency);

        Assert.False(result);
    }

    [Fact]
    public void TryParseDepositFrequency_ValidEnumString_ReturnsTrue()
    {
        var result = this.service.TryParseDepositFrequency("0", out var frequency);

        Assert.True(result);
    }

    // CalculateTotalPages Tests
    [Theory]
    [InlineData(10, 0, 0)]// Zero page size
    [InlineData(10, -5, 0)] // Negative page size
    [InlineData(20, 10, 2)] // Exact division
    [InlineData(21, 10, 3)] // Ceiling rounding needed
    [InlineData(0, 10, 0)] // Zero total items
    public void CalculateTotalPages_ReturnsExpectedCount(int totalCount, int pageSize, int expectedPages)
    {
        var result = this.service.CalculateTotalPages(totalCount, pageSize);

        Assert.Equal(expectedPages, result);
    }

    // ValidateCreateAccount Tests
    [Fact]
    public void ValidateCreateAccount_AllValid_NonGoal_ReturnsEmptyDictionary()
    {
        var errors = this.service.ValidateCreateAccount(
            selectedSavingsType: "Standard",
            accountName: "My Savings",
            initialDepositText: "100.00",
            hasFundingSource: true,
            selectedFrequency: "Monthly",
            targetAmount: null,
            targetDate: null,
            isGoalSavings: false);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateCreateAccount_AllValid_Goal_ReturnsEmptyDictionary()
    {
        var errors = this.service.ValidateCreateAccount(
            selectedSavingsType: "Goal",
            accountName: "Vacation",
            initialDepositText: "100.00",
            hasFundingSource: true,
            selectedFrequency: "Weekly",
            targetAmount: 5000m,
            targetDate: DateTimeOffset.UtcNow.AddDays(10), // Future date
            isGoalSavings: true);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateCreateAccount_MissingBaseFields_ReturnsErrors()
    {
        var errors = this.service.ValidateCreateAccount(
            selectedSavingsType: " ",
            accountName: "   ",
            initialDepositText: "invalid",
            hasFundingSource: false,
            selectedFrequency: null,
            targetAmount: null,
            targetDate: null,
            isGoalSavings: false);

        Assert.Equal(5, errors.Count);
        Assert.Contains("SavingsType", errors.Keys);
        Assert.Contains("AccountName", errors.Keys);
        Assert.Contains("InitialDeposit", errors.Keys);
        Assert.Contains("FundingSource", errors.Keys);
        Assert.Contains("Frequency", errors.Keys);
    }

    [Theory]
    // Negative target amount, null date (ADDED .0 to -100)
    [InlineData(-100.0, null, 2)]
    // Null target amount, date is today
    [InlineData(null, 0, 2)]
    // Null target amount, date in the past
    [InlineData(null, -5, 2)]
    public void ValidateCreateAccount_InvalidGoalFields_ReturnsErrors(double? targetAmount, int? daysToAdd, int expectedErrorCount)
    {
        DateTimeOffset? targetDate = daysToAdd.HasValue ? DateTimeOffset.Now.AddDays(daysToAdd.Value) : null;

        var errors = this.service.ValidateCreateAccount(
            selectedSavingsType: "Goal",
            accountName: "Vacation",
            initialDepositText: "100.00", // Valid base fields
            hasFundingSource: true,
            selectedFrequency: "Weekly",
            targetAmount: targetAmount.HasValue ? (decimal)targetAmount.Value : null,
            targetDate: targetDate,
            isGoalSavings: true); // Triggers goal validation branch

        Assert.Equal(expectedErrorCount, errors.Count);
        Assert.Contains("TargetAmount", errors.Keys);
        Assert.Contains("TargetDate", errors.Keys);
    }
}