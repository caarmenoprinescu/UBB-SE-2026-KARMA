// <copyright file="PaymentCalculationServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using System.Globalization;
    using KarmaBanking.App.Services;
    using Xunit;

    public class PaymentCalculationServiceTests
    {
        private readonly PaymentCalculationService paymentCalculationService;

        public PaymentCalculationServiceTests()
        {
            this.paymentCalculationService = new PaymentCalculationService();
        }

        [Theory]
        [InlineData(100, 1000, 10, true, 0, 900, 9)]
        [InlineData(100, 1000, 10, false, 200, 800, 8)]
        [InlineData(100, 500, 10, false, 600, 0, 4)]
        public void CalculatePaymentPreview_ReturnsExpectedValues(
            decimal monthlyInstallmentAmount,
            decimal currentOutstandingBalance,
            int remainingMonthsCount,
            bool isStandardPaymentSelected,
            decimal customPaymentAmountValue,
            decimal expectedBalanceAfterPayment,
            int expectedRemainingMonthsAfterPayment)
        {
            // Act
            var calculationResult = this.paymentCalculationService.CalculatePaymentPreview(
                monthlyInstallmentAmount,
                currentOutstandingBalance,
                remainingMonthsCount,
                isStandardPaymentSelected,
                customPaymentAmountValue);

            // Assert
            Assert.Equal(expectedBalanceAfterPayment, calculationResult.BalanceAfterPayment);
            Assert.Equal(expectedRemainingMonthsAfterPayment, calculationResult.RemainingMonths);
        }

        [Fact]
        public void ParsePaymentAmount_ValidInput_ReturnsParsedAmount()
        {
            // Arrange
            string paymentInputText = 1234.56m.ToString(CultureInfo.CurrentCulture);

            // Act
            var parseResult = this.paymentCalculationService.ParsePaymentAmount(paymentInputText);

            // Assert
            Assert.True(parseResult.Success);
            Assert.Equal(1234.56m, parseResult.Amount);
        }

        [Fact]
        public void ValidatePaymentAmount_ExceedsBalance_ReturnsFalse()
        {
            // Arrange
            decimal paymentAmountToValidate = 1500m;
            decimal currentOutstandingBalance = 1000m;
            string expectedValidationErrorMessage = $"Payment amount cannot exceed outstanding balance of {currentOutstandingBalance:C2}.";

            // Act
            var validationResult = this.paymentCalculationService.ValidatePaymentAmount(paymentAmountToValidate, currentOutstandingBalance);

            // Assert
            Assert.False(validationResult.IsValid);
            Assert.Equal(expectedValidationErrorMessage, validationResult.ValidationMessage);
        }
    }
}