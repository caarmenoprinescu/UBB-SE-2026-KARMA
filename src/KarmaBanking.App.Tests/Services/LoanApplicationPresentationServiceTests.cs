namespace KarmaBanking.App.Tests.Services
{
    using KarmaBanking.App.Services;
    using Xunit;

    public class LoanApplicationPresentationServiceTests
    {
        [Fact]
        public void BuildApplicationOutcome_NullRejectionReason_ReturnsApproved()
        {
            var service = new LoanApplicationPresentationService();

            var (approved, message) = service.BuildApplicationOutcome(null);

            Assert.True(approved);
            Assert.Equal("Your loan application has been approved!", message);
        }

        [Fact]
        public void BuildApplicationOutcome_WithRejectionReason_ReturnsRejectedWithMessage()
        {
            var service = new LoanApplicationPresentationService();
            var reason = "Credit score too low";

            var (approved, message) = service.BuildApplicationOutcome(reason);

            Assert.False(approved);
            Assert.Equal($"Application rejected: {reason}", message);
        }
    }
}