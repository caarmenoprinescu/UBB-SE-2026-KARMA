// <copyright file="SavingsPresentationServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using System.Collections.Generic;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Services;
    using Xunit;

    public class SavingsPresentationServiceTests
    {
        private readonly SavingsPresentationService savingsPresentationService;

        public SavingsPresentationServiceTests()
        {
            this.savingsPresentationService = new SavingsPresentationService();
        }

        [Fact]
        public void BuildTotalSavedAmount_CalculatesSumAndFormatsProperly()
        {
            // Arrange
            var savingsAccountsList = new List<SavingsAccount>
            {
                new SavingsAccount { Balance = 1500.50m },
                new SavingsAccount { Balance = 2500.25m }
            };
            string expectedFormattedBalance = $"${4000.75m:F2}";

            // Act
            string actualFormattedBalance = this.savingsPresentationService.BuildTotalSavedAmount(savingsAccountsList);

            // Assert
            Assert.Equal(expectedFormattedBalance, actualFormattedBalance);
        }

        [Theory]
        [InlineData(0, "across 0 accounts")]
        [InlineData(1, "across 1 account")]
        [InlineData(2, "across 2 accounts")]
        public void BuildNumberOfAccountsText_HandlesPluralization(int accountsCount, string expectedPluralizedText)
        {
            // Act
            string actualPluralizedText = this.savingsPresentationService.BuildNumberOfAccountsText(accountsCount);

            // Assert
            Assert.Equal(expectedPluralizedText, actualPluralizedText);
        }
    }
}