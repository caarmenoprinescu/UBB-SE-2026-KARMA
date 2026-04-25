// <copyright file="LoanApplicationPresentationServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using KarmaBanking.App.Services;
    using Xunit;

    public class LoanApplicationPresentationServiceTests
    {
        [Fact]
        public void BuildApplicationOutcome_NullRejectionReason_ReturnsApproved()
        {
            // Arrange
            var loanApplicationPresentationService = new LoanApplicationPresentationService();

            // Act
            var (isApproved, applicationMessage) = loanApplicationPresentationService.BuildApplicationOutcome(null);

            // Assert
            Assert.True(isApproved);
            Assert.Equal("Your loan application has been approved!", applicationMessage);
        }

        [Fact]
        public void BuildApplicationOutcome_WithRejectionReason_ReturnsRejectedWithMessage()
        {
            // Arrange
            var loanApplicationPresentationService = new LoanApplicationPresentationService();
            string rejectionReasonMessage = "Credit score too low";

            // Act
            var (isApproved, applicationMessage) = loanApplicationPresentationService.BuildApplicationOutcome(rejectionReasonMessage);

            // Assert
            Assert.False(isApproved);
            Assert.Equal($"Application rejected: {rejectionReasonMessage}", applicationMessage);
        }
    }
}