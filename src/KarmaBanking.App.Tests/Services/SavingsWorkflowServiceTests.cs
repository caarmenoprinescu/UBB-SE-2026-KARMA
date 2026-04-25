// <copyright file="SavingsWorkflowServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using System.Collections.Generic;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Models.DTOs;
    using KarmaBanking.App.Services;
    using Xunit;

    public class SavingsWorkflowServiceTests
    {
        private readonly SavingsWorkflowService savingsWorkflowService;

        public SavingsWorkflowServiceTests()
        {
            this.savingsWorkflowService = new SavingsWorkflowService();
        }

        [Fact]
        public void GetDefaultFundingSource_PopulatedList_ReturnsFirstItem()
        {
            // Arrange
            var firstFundingSourceOption = new FundingSourceOption();
            var fundingSourcesList = new List<FundingSourceOption> { firstFundingSourceOption, new FundingSourceOption() };

            // Act
            var actualFundingSource = this.savingsWorkflowService.GetDefaultFundingSource(fundingSourcesList);

            // Assert
            Assert.Same(firstFundingSourceOption, actualFundingSource);
        }

        [Fact]
        public void BuildWithdrawResultMessage_SuccessWithPenalty_FormatsProperly()
        {
            // Arrange
            var withdrawalResponseDataTransferObject = new WithdrawResponseDto
            {
                Success = true,
                AmountWithdrawn = 500m,
                PenaltyApplied = 25.50m,
                NewBalance = 1474.50m
            };
            string expectedWithdrawalMessage = $"Withdrawn: ${500m:N2} (penalty: ${25.50m:N2}). New balance: ${1474.50m:N2}";

            // Act
            string actualWithdrawalMessage = this.savingsWorkflowService.BuildWithdrawResultMessage(withdrawalResponseDataTransferObject);

            // Assert
            Assert.Equal(expectedWithdrawalMessage, actualWithdrawalMessage);
        }
    }
}