// <copyright file="SavingsWorkflowServiceTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Collections.Generic;
using KarmaBanking.App.Models;
using KarmaBanking.App.Models.DTOs;
using KarmaBanking.App.Services;
using Xunit;

namespace KarmaBanking.App.Tests.Services;

public class SavingsWorkflowServiceTests
{
    private readonly SavingsWorkflowService service;

    public SavingsWorkflowServiceTests()
    {
        this.service = new SavingsWorkflowService();
    }

    // GetDefaultFundingSource Tests
    [Fact]
    public void GetDefaultFundingSource_PopulatedList_ReturnsFirstItem()
    {
        var expectedSource = new FundingSourceOption();
        var sources = new List<FundingSourceOption> { expectedSource, new FundingSourceOption() };

        var result = this.service.GetDefaultFundingSource(sources);

        Assert.Same(expectedSource, result);
    }

    [Fact]
    public void GetDefaultFundingSource_EmptyList_ReturnsNull()
    {
        var result = this.service.GetDefaultFundingSource(new List<FundingSourceOption>());
        Assert.Null(result);
    }

    // GetDefaultCloseDestinationId Tests
    [Fact]
    public void GetDefaultCloseDestinationId_PopulatedList_ReturnsFirstId()
    {
        var accounts = new List<SavingsAccount>
        {
            new SavingsAccount { IdentificationNumber = 42 },
            new SavingsAccount { IdentificationNumber = 99 }
        };

        var result = this.service.GetDefaultCloseDestinationId(accounts);

        Assert.Equal(42, result);
    }

    [Fact]
    public void GetDefaultCloseDestinationId_EmptyList_ReturnsZero()
    {
        var result = this.service.GetDefaultCloseDestinationId(new List<SavingsAccount>());

        Assert.Equal(0, result);
    }

    // ValidateWithdrawRequest Tests
    [Theory]
    // Negative amount
    [InlineData(-50.0, true, false, "Please enter a valid amount.")]
    // Zero amount
    [InlineData(0.0, true, false, "Please enter a valid amount.")]
    // Valid amount, but missing destination
    [InlineData(100.0, false, false, "Please select a destination account.")]
    // Valid amount and destination
    [InlineData(100.0, true, true, "")]
    public void ValidateWithdrawRequest_ReturnsExpectedTuple(
        double amountDouble,
        bool hasDestination,
        bool expectedValid,
        string expectedError)
    {
        decimal amount = (decimal)amountDouble;
        var destination = hasDestination ? new FundingSourceOption() : null;

        var result = this.service.ValidateWithdrawRequest(amount, destination);

        Assert.Equal(expectedValid, result.IsValid);
        Assert.Equal(expectedError, result.ErrorMessage);
    }

    // BuildWithdrawResultMessage Tests
    [Fact]
    public void BuildWithdrawResultMessage_NotSuccessful_ReturnsMessage()
    {
        var response = new WithdrawResponseDto
        {
            Success = false,
            Message = "Insufficient funds."
        };

        var result = this.service.BuildWithdrawResultMessage(response);

        Assert.Equal("Insufficient funds.", result);
    }

    [Fact]
    public void BuildWithdrawResultMessage_SuccessWithoutPenalty_FormatsProperly()
    {
        var response = new WithdrawResponseDto
        {
            Success = true,
            AmountWithdrawn = 500m,
            PenaltyApplied = 0m,
            NewBalance = 1500m
        };
        string expected = $"Withdrawn: ${500m:N2}. New balance: ${1500m:N2}";

        var result = this.service.BuildWithdrawResultMessage(response);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildWithdrawResultMessage_SuccessWithPenalty_FormatsProperly()
    {
        var response = new WithdrawResponseDto
        {
            Success = true,
            AmountWithdrawn = 500m,
            PenaltyApplied = 25.50m,
            NewBalance = 1474.50m
        };
        string expected = $"Withdrawn: ${500m:N2} (penalty: ${25.50m:N2}). New balance: ${1474.50m:N2}";

        var result = this.service.BuildWithdrawResultMessage(response);

        Assert.Equal(expected, result);
    }

    // ValidateCloseConfirmation Tests
    [Theory]
    // User didn't confirm
    [InlineData(false, 1, false, "Please confirm account closure.")]
    // User confirmed, but didn't pick an account (ID is 0)
    [InlineData(true, 0, false, "Please select a destination account.")]
    // Valid combination
    [InlineData(true, 42, true, "")]
    public void ValidateCloseConfirmation_ReturnsExpectedTuple(
        bool userConfirmed,
        int destinationId,
        bool expectedValid,
        string expectedError)
    {
        var result = this.service.ValidateCloseConfirmation(userConfirmed, destinationId);

        Assert.Equal(expectedValid, result.IsValid);
        Assert.Equal(expectedError, result.ErrorMessage);
    }

    // Pagination Tests
    [Theory]
    [InlineData(1, 5, true)] // Page 1 of 5
    [InlineData(5, 5, false)] // On last page
    [InlineData(6, 5, false)] // Past total pages
    public void CanMoveToNextPage_ReturnsExpectedResult(int current, int total, bool expected)
    {
        Assert.Equal(expected, this.service.CanMoveToNextPage(current, total));
    }

    [Theory]
    [InlineData(1, false)] // First page
    [InlineData(2, true)] // Second page
    [InlineData(5, true)] // Fifth page
    public void CanMoveToPreviousPage_ReturnsExpectedResult(int current, bool expected)
    {
        Assert.Equal(expected, this.service.CanMoveToPreviousPage(current));
    }
}