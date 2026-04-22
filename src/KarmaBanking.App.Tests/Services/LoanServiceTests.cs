namespace KarmaBanking.App.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Moq;
    using Xunit;

    public class LoanServiceTests
    {
        [Fact]
        public async Task ProcessApplicationStatusAsync_WhenUserHasFiveActiveLoans_RejectsApplication()
        {
            var repository = new Mock<ILoanRepository>();
            repository.Setup(r => r.GetLoansByUserAsync(1)).ReturnsAsync(new List<Loan>
            {
                new() { LoanStatus = LoanStatus.Active, OutstandingBalance = 1000m },
                new() { LoanStatus = LoanStatus.Active, OutstandingBalance = 1000m },
                new() { LoanStatus = LoanStatus.Active, OutstandingBalance = 1000m },
                new() { LoanStatus = LoanStatus.Active, OutstandingBalance = 1000m },
                new() { LoanStatus = LoanStatus.Active, OutstandingBalance = 1000m },
            });

            var service = new LoanService(repository.Object);
            var application = new LoanApplication
            {
                Id = 10,
                UserId = 1,
                LoanType = LoanType.Personal,
                DesiredAmount = 1000m,
                PreferredTermMonths = 12,
                Purpose = "Home office",
            };

            var (status, reason) = await service.ProcessApplicationStatusAsync(application);

            Assert.Equal(LoanApplicationStatus.Rejected, status);
            Assert.Equal("Maximum number of active loans reached.", reason);
            repository.Verify(r => r.UpdateLoanApplicationStatusAsync(
                10,
                LoanApplicationStatus.Rejected,
                "Maximum number of active loans reached."), Times.Once);
        }

        [Fact]
        public async Task ProcessApplicationStatusAsync_WhenDebtLimitExceeded_RejectsApplication()
        {
            var repository = new Mock<ILoanRepository>();
            repository.Setup(r => r.GetLoansByUserAsync(1)).ReturnsAsync(new List<Loan>
            {
                new() { LoanStatus = LoanStatus.Active, OutstandingBalance = 190000m },
            });

            var service = new LoanService(repository.Object);
            var application = new LoanApplication
            {
                Id = 11,
                UserId = 1,
                LoanType = LoanType.Personal,
                DesiredAmount = 10000m,
                PreferredTermMonths = 24,
                Purpose = "Consolidation",
            };

            var (status, reason) = await service.ProcessApplicationStatusAsync(application);

            Assert.Equal(LoanApplicationStatus.Rejected, status);
            Assert.Equal("Total debt limit exceeded.", reason);
            repository.Verify(r => r.UpdateLoanApplicationStatusAsync(
                11,
                LoanApplicationStatus.Rejected,
                "Total debt limit exceeded."), Times.Once);
        }

        [Fact]
        public async Task ProcessApplicationStatusAsync_WhenRulesPass_ApprovesApplication()
        {
            var repository = new Mock<ILoanRepository>();
            repository.Setup(r => r.GetLoansByUserAsync(1)).ReturnsAsync(new List<Loan>
            {
                new() { LoanStatus = LoanStatus.Active, OutstandingBalance = 5000m },
            });

            var service = new LoanService(repository.Object);
            var application = new LoanApplication
            {
                Id = 12,
                UserId = 1,
                LoanType = LoanType.Auto,
                DesiredAmount = 10000m,
                PreferredTermMonths = 36,
                Purpose = "Car",
            };

            var (status, reason) = await service.ProcessApplicationStatusAsync(application);

            Assert.Equal(LoanApplicationStatus.Approved, status);
            Assert.Null(reason);
            repository.Verify(r => r.UpdateLoanApplicationStatusAsync(12, LoanApplicationStatus.Approved, null), Times.Once);
        }

        [Fact]
        public async Task PayInstallmentAsync_StandardPayment_UpdatesBalanceAndRemainingMonths()
        {
            var repository = new Mock<ILoanRepository>();
            repository.Setup(r => r.GetLoanByIdAsync(20)).ReturnsAsync(new Loan
            {
                Id = 20,
                OutstandingBalance = 1000m,
                MonthlyInstallment = 200m,
                RemainingMonths = 5,
                LoanStatus = LoanStatus.Active,
            });

            var service = new LoanService(repository.Object);

            await service.PayInstallmentAsync(20, null);

            repository.Verify(r => r.UpdateLoanAfterPaymentAsync(20, 800m, 4, LoanStatus.Active), Times.Once);
        }

        [Fact]
        public async Task PayInstallmentAsync_CustomPaymentBelowInstallment_Throws()
        {
            var repository = new Mock<ILoanRepository>();
            repository.Setup(r => r.GetLoanByIdAsync(21)).ReturnsAsync(new Loan
            {
                Id = 21,
                OutstandingBalance = 1000m,
                MonthlyInstallment = 200m,
                RemainingMonths = 5,
                LoanStatus = LoanStatus.Active,
            });

            var service = new LoanService(repository.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.PayInstallmentAsync(21, 150m));
            repository.Verify(r => r.UpdateLoanAfterPaymentAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<LoanStatus>()), Times.Never);
        }

        [Fact]
        public async Task PayInstallmentAsync_WhenLoanGetsPaidOff_ClosesLoan()
        {
            var repository = new Mock<ILoanRepository>();
            repository.Setup(r => r.GetLoanByIdAsync(22)).ReturnsAsync(new Loan
            {
                Id = 22,
                OutstandingBalance = 600m,
                MonthlyInstallment = 200m,
                RemainingMonths = 3,
                LoanStatus = LoanStatus.Active,
            });

            var service = new LoanService(repository.Object);

            await service.PayInstallmentAsync(22, 600m);

            repository.Verify(r => r.UpdateLoanAfterPaymentAsync(22, 0m, 0, LoanStatus.Passed), Times.Once);
        }

        [Fact]
        public void CalculatePaymentPreview_WithCustomAmount_ComputesPreviewValues()
        {
            var repository = new Mock<ILoanRepository>();
            var service = new LoanService(repository.Object);
            var loan = new Loan
            {
                MonthlyInstallment = 250m,
                OutstandingBalance = 1000m,
                RemainingMonths = 6,
            };

            var (balanceAfterPayment, remainingMonths) = service.CalculatePaymentPreview(loan, 500m);

            Assert.Equal(500m, balanceAfterPayment);
            Assert.Equal(4, remainingMonths);
        }

        [Fact]
        public void ParseCustomPaymentAmount_InvalidInput_ReturnsNull()
        {
            var repository = new Mock<ILoanRepository>();
            var service = new LoanService(repository.Object);

            var amount = service.ParseCustomPaymentAmount("not-a-number");

            Assert.Null(amount);
        }

        [Fact]
        public async Task SubmitLoanApplicationAsync_WhenApproved_CreatesLoanAndAmortization()
        {
            var repository = new Mock<ILoanRepository>();
            repository.Setup(r => r.CreateLoanApplicationAsync(It.IsAny<LoanApplicationRequest>())).ReturnsAsync(30);
            repository.Setup(r => r.GetLoansByUserAsync(1)).ReturnsAsync(new List<Loan>());
            repository.Setup(r => r.CreateLoanAsync(It.IsAny<Loan>())).ReturnsAsync(40);
            repository.Setup(r => r.GetLoanByIdAsync(40)).ReturnsAsync(new Loan
            {
                Id = 40,
                UserId = 1,
                LoanType = LoanType.Personal,
                Principal = 12000m,
                OutstandingBalance = 12000m,
                InterestRate = 8.5m,
                MonthlyInstallment = 376.92m,
                RemainingMonths = 36,
                LoanStatus = LoanStatus.Active,
                TermInMonths = 36,
                StartDate = DateTime.Today,
            });

            var service = new LoanService(repository.Object);
            var request = new LoanApplicationRequest
            {
                UserId = 1,
                LoanType = LoanType.Personal,
                DesiredAmount = 12000m,
                PreferredTermMonths = 36,
                Purpose = "Renovation",
            };

            var (status, rejectionReason) = await service.SubmitLoanApplicationAsync(request);

            Assert.Equal(LoanApplicationStatus.Approved, status);
            Assert.Null(rejectionReason);
            repository.Verify(r => r.UpdateLoanApplicationStatusAsync(30, LoanApplicationStatus.Approved, null), Times.Once);
            repository.Verify(r => r.CreateLoanAsync(It.IsAny<Loan>()), Times.Once);
            repository.Verify(r => r.SaveAmortizationAsync(It.Is<List<AmortizationRow>>(rows => rows.Count == 36)), Times.Once);
        }

        [Fact]
        public async Task SubmitLoanApplicationAsync_WhenRejected_DoesNotCreateLoanOrAmortization()
        {
            var repository = new Mock<ILoanRepository>();
            repository.Setup(r => r.CreateLoanApplicationAsync(It.IsAny<LoanApplicationRequest>())).ReturnsAsync(31);
            repository.Setup(r => r.GetLoansByUserAsync(1)).ReturnsAsync(new List<Loan>
            {
                new() { LoanStatus = LoanStatus.Active, OutstandingBalance = 199500m },
            });

            var service = new LoanService(repository.Object);
            var request = new LoanApplicationRequest
            {
                UserId = 1,
                LoanType = LoanType.Personal,
                DesiredAmount = 1000m,
                PreferredTermMonths = 12,
                Purpose = "Emergency",
            };

            var (status, rejectionReason) = await service.SubmitLoanApplicationAsync(request);

            Assert.Equal(LoanApplicationStatus.Rejected, status);
            Assert.Equal("Total debt limit exceeded.", rejectionReason);
            repository.Verify(r => r.UpdateLoanApplicationStatusAsync(31, LoanApplicationStatus.Rejected, "Total debt limit exceeded."), Times.Once);
            repository.Verify(r => r.CreateLoanAsync(It.IsAny<Loan>()), Times.Never);
            repository.Verify(r => r.SaveAmortizationAsync(It.IsAny<List<AmortizationRow>>()), Times.Never);
        }

        [Fact]
        public async Task PayInstallmentAsync_WhenPaymentExceedsOutstanding_Throws()
        {
            var repository = new Mock<ILoanRepository>();
            repository.Setup(r => r.GetLoanByIdAsync(23)).ReturnsAsync(new Loan
            {
                Id = 23,
                OutstandingBalance = 500m,
                MonthlyInstallment = 100m,
                RemainingMonths = 5,
                LoanStatus = LoanStatus.Active,
            });

            var service = new LoanService(repository.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.PayInstallmentAsync(23, 600m));
            repository.Verify(r => r.UpdateLoanAfterPaymentAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<LoanStatus>()), Times.Never);
        }

        [Fact]
        public async Task PayInstallmentAsync_WhenLoanAlreadyClosed_Throws()
        {
            var repository = new Mock<ILoanRepository>();
            repository.Setup(r => r.GetLoanByIdAsync(24)).ReturnsAsync(new Loan
            {
                Id = 24,
                OutstandingBalance = 0m,
                MonthlyInstallment = 100m,
                RemainingMonths = 0,
                LoanStatus = LoanStatus.Passed,
            });

            var service = new LoanService(repository.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.PayInstallmentAsync(24, null));
        }

        [Fact]
        public void NormalizeCustomPaymentAmount_WhenOverBalance_CapsToOutstanding()
        {
            var repository = new Mock<ILoanRepository>();
            var service = new LoanService(repository.Object);
            var loan = new Loan
            {
                MonthlyInstallment = 200m,
                OutstandingBalance = 150m,
                RemainingMonths = 1,
            };

            var normalized = service.NormalizeCustomPaymentAmount(loan, 300m);

            Assert.Equal(150m, normalized);
        }

        [Fact]
        public void GetRepaymentProgress_WhenPrincipalIsZero_ReturnsZero()
        {
            var repository = new Mock<ILoanRepository>();
            var service = new LoanService(repository.Object);
            var loan = new Loan
            {
                Principal = 0m,
                OutstandingBalance = 0m,
            };

            var progress = service.GetRepaymentProgress(loan);

            Assert.Equal(0d, progress);
        }
    }
}
