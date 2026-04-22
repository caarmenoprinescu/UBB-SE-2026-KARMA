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
            var service = new LoanPresentationService();
            var loan = new Loan
            {
                Principal = 10000m,
                OutstandingBalance = 2500m
            };

            var result = service.GetRepaymentProgress(loan);
            var expected = (double)AmortizationCalculator.ComputeRepaymentProgress(loan.Principal, loan.OutstandingBalance);

            Assert.Equal(expected, result);
        }
    }
}