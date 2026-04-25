// <copyright file="LoanPresentationServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Services;
    using KarmaBanking.App.Utils;
    using Xunit;

    public class LoanPresentationServiceTests
    {
        [Fact]
        public void GetRepaymentProgress_ValidLoan_ReturnsExpectedProgress()
        {
            // Arrange
            var loanPresentationService = new LoanPresentationService();
            var loanInstance = new Loan
            {
                Principal = 10000m,
                OutstandingBalance = 2500m
            };

            // Act
            double repaymentProgressResult = loanPresentationService.GetRepaymentProgress(loanInstance);
            double expectedProgressValue = (double)AmortizationCalculator.ComputeRepaymentProgress(loanInstance.Principal, loanInstance.OutstandingBalance);

            // Assert
            Assert.Equal(expectedProgressValue, repaymentProgressResult);
        }
    }
}