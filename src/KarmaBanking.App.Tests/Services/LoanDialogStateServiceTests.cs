// <copyright file="LoanDialogStateServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using KarmaBanking.App.Services;
    using Xunit;

    public class LoanDialogStateServiceTests
    {
        [Fact]
        public void ShouldComputeEstimate_ValidInputs_ReturnsTrue()
        {
            // Arrange
            var loanDialogStateService = new LoanDialogStateService();

            // Act
            bool result = loanDialogStateService.ShouldComputeEstimate(5000, 12, "Home Renovation");

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(0, 12, "Purpose")]
        [InlineData(-100, 12, "Purpose")]
        public void ShouldComputeEstimate_InvalidAmount_ReturnsFalse(double loanAmount, int loanTermMonths, string loanPurpose)
        {
            // Arrange
            var loanDialogStateService = new LoanDialogStateService();

            // Act
            bool result = loanDialogStateService.ShouldComputeEstimate(loanAmount, loanTermMonths, loanPurpose);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(5000, 12, "")]
        [InlineData(5000, 12, "   ")]
        [InlineData(5000, 12, null)]
        public void ShouldComputeEstimate_InvalidPurpose_ReturnsFalse(double loanAmount, int loanTermMonths, string loanPurpose)
        {
            // Arrange
            var loanDialogStateService = new LoanDialogStateService();

            // Act
            bool result = loanDialogStateService.ShouldComputeEstimate(loanAmount, loanTermMonths, loanPurpose);

            // Assert
            Assert.False(result);
        }
    }
}