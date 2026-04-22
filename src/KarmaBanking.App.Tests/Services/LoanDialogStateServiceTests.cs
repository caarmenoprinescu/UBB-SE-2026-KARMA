namespace KarmaBanking.App.Tests.Services
{
    using KarmaBanking.App.Services;
    using Xunit;

    public class LoanDialogStateServiceTests
    {
        [Fact]
        public void ShouldComputeEstimate_ValidInputs_ReturnsTrue()
        {
            var service = new LoanDialogStateService();
            var result = service.ShouldComputeEstimate(5000, 12, "Home Renovation");
            Assert.True(result);
        }

        [Theory]
        [InlineData(0, 12, "Purpose")]
        [InlineData(-100, 12, "Purpose")]
        public void ShouldComputeEstimate_InvalidAmount_ReturnsFalse(double amount, int term, string purpose)
        {
            var service = new LoanDialogStateService();
            var result = service.ShouldComputeEstimate(amount, term, purpose);
            Assert.False(result);
        }

        [Theory]
        [InlineData(5000, 0, "Purpose")]
        [InlineData(5000, -5, "Purpose")]
        public void ShouldComputeEstimate_InvalidTerm_ReturnsFalse(double amount, int term, string purpose)
        {
            var service = new LoanDialogStateService();
            var result = service.ShouldComputeEstimate(amount, term, purpose);
            Assert.False(result);
        }

        [Theory]
        [InlineData(5000, 12, "")]
        [InlineData(5000, 12, "   ")]
        [InlineData(5000, 12, null)]
        public void ShouldComputeEstimate_InvalidPurpose_ReturnsFalse(double amount, int term, string purpose)
        {
            var service = new LoanDialogStateService();
            var result = service.ShouldComputeEstimate(amount, term, purpose);
            Assert.False(result);
        }
    }
}