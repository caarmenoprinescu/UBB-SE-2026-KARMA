// <copyright file="LoanServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Models.Enums;
    using KarmaBanking.App.Repositories.Interfaces;
    using KarmaBanking.App.Services;
    using Moq;
    using Xunit;

    public class LoanServiceTests
    {
        [Fact]
        public async Task ProcessApplicationStatusAsync_WhenUserHasFiveActiveLoans_RejectsApplication()
        {
            // Arrange
            var loanRepositoryMock = new Mock<ILoanRepository>();
            loanRepositoryMock.Setup(repository => repository.GetLoansByUserAsync(1)).ReturnsAsync(new List<Loan>
            {
                new() { LoanStatus = LoanStatus.Active, OutstandingBalance = 1000m },
                new() { LoanStatus = LoanStatus.Active, OutstandingBalance = 1000m },
                new() { LoanStatus = LoanStatus.Active, OutstandingBalance = 1000m },
                new() { LoanStatus = LoanStatus.Active, OutstandingBalance = 1000m },
                new() { LoanStatus = LoanStatus.Active, OutstandingBalance = 1000m },
            });

            var loanService = new LoanService(loanRepositoryMock.Object);
            var loanApplicationInstance = new LoanApplication
            {
                IdentificationNumber = 10,
                UserIdentificationNumber = 1,
                LoanType = LoanType.Personal,
                DesiredAmount = 1000m,
                PreferredTermMonths = 12,
                Purpose = "Home office",
            };

            // Act
            var (loanApplicationStatus, rejectionReason) = await loanService.ProcessApplicationStatusAsync(loanApplicationInstance);

            // Assert
            Assert.Equal(LoanApplicationStatus.Rejected, loanApplicationStatus);
            Assert.Equal("Maximum number of active loans reached.", rejectionReason);
            loanRepositoryMock.Verify(repository => repository.UpdateLoanApplicationStatusAsync(
                10,
                LoanApplicationStatus.Rejected,
                "Maximum number of active loans reached."), Times.Once);
        }

        [Fact]
        public async Task PayInstallmentAsync_StandardPayment_UpdatesBalanceAndRemainingMonths()
        {
            // Arrange
            var loanRepositoryMock = new Mock<ILoanRepository>();
            loanRepositoryMock.Setup(repository => repository.GetLoanByIdAsync(20)).ReturnsAsync(new Loan
            {
                IdentificationNumber = 20,
                OutstandingBalance = 1000m,
                MonthlyInstallment = 200m,
                RemainingMonths = 5,
                LoanStatus = LoanStatus.Active,
            });

            var loanService = new LoanService(loanRepositoryMock.Object);

            // Act
            await loanService.PayInstallmentAsync(20, null);

            // Assert
            loanRepositoryMock.Verify(repository => repository.UpdateLoanAfterPaymentAsync(20, 800m, 4, LoanStatus.Active), Times.Once);
        }

        [Fact]
        public void CalculatePaymentPreview_WithCustomAmount_ComputesPreviewValues()
        {
            // Arrange
            var loanRepositoryMock = new Mock<ILoanRepository>();
            var loanService = new LoanService(loanRepositoryMock.Object);
            var loanInstance = new Loan
            {
                MonthlyInstallment = 250m,
                OutstandingBalance = 1000m,
                RemainingMonths = 6,
            };

            // Act
            var (balanceAfterPayment, remainingMonthsValue) = loanService.CalculatePaymentPreview(loanInstance, 500m);

            // Assert
            Assert.Equal(500m, balanceAfterPayment);
            Assert.Equal(4, remainingMonthsValue);
        }
    }
}