// <copyright file="SavingsUiRulesServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Services;
    using Xunit;

    public class SavingsUiRulesServiceTests
    {
        private readonly SavingsUiRulesService savingsUiRulesService;

        public SavingsUiRulesServiceTests()
        {
            this.savingsUiRulesService = new SavingsUiRulesService();
        }

        [Theory]
        [InlineData("150.75", true, 150.75)]
        [InlineData("0", false, 0)]
        [InlineData("-50", false, 0)]
        [InlineData("invalid", false, 0)]
        public void TryParsePositiveAmount_ReturnsExpectedResult(
            string amountInputText,
            bool expectedParsingSuccess,
            double expectedParsedAmountValue)
        {
            // Act
            bool actualParsingSuccess = this.savingsUiRulesService.TryParsePositiveAmount(amountInputText, out decimal actualParsedAmount);

            // Assert
            Assert.Equal(expectedParsingSuccess, actualParsingSuccess);
            Assert.Equal((decimal)expectedParsedAmountValue, actualParsedAmount);
        }

        [Fact]
        public void BuildDepositPreview_ValidInput_ReturnsFormattedString()
        {
            // Arrange
            var savingsAccountInstance = new SavingsAccount { Balance = 500m };
            string expectedPreviewMessage = $"New balance will be: ${650.50m:N2}";

            // Act
            string actualPreviewMessage = this.savingsUiRulesService.BuildDepositPreview("150.50", savingsAccountInstance);

            // Assert
            Assert.Equal(expectedPreviewMessage, actualPreviewMessage);
        }
    }
}