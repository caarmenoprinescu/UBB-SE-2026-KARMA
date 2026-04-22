namespace KarmaBanking.App.Tests.Services
{
    using KarmaBanking.App.Services;
    using Xunit;

    public class CryptoTradeCalculationServiceTests
    {
        [Fact]
        public void TryParsePositiveQuantity_ValidPositiveQuantity_ReturnsTrueAndParsedValue()
        {
            var service = new CryptoTradeCalculationService();
            var quantityText = "10.5";

            var success = service.TryParsePositiveQuantity(quantityText, out var quantity);

            Assert.True(success);
            Assert.Equal(10.5m, quantity);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-5")]
        [InlineData("abc")]
        [InlineData("")]
        [InlineData(null)]
        public void TryParsePositiveQuantity_InvalidOrNonPositiveQuantity_ReturnsFalseAndZero(string quantityText)
        {
            var service = new CryptoTradeCalculationService();

            var success = service.TryParsePositiveQuantity(quantityText, out var quantity);

            Assert.False(success);
            Assert.Equal(0m, quantity);
        }

        [Fact]
        public void GetMockMarketPrice_BtcTicker_Returns65000()
        {
            var service = new CryptoTradeCalculationService();
            var price = service.GetMockMarketPrice("BTC");
            Assert.Equal(65000m, price);
        }

        [Fact]
        public void GetMockMarketPrice_OtherTicker_Returns3000()
        {
            var service = new CryptoTradeCalculationService();
            var price = service.GetMockMarketPrice("ETH");
            Assert.Equal(3000m, price);
        }

        [Fact]
        public void CalculateTradePreview_BuyActionAboveMinimumFee_CalculatesCorrectly()
        {
            var service = new CryptoTradeCalculationService();
            var (estimatedFee, totalAmount) = service.CalculateTradePreview("BTC", "BUY", 1m);

            Assert.Equal(975m, estimatedFee);
            Assert.Equal(65975m, totalAmount);
        }

        [Fact]
        public void CalculateTradePreview_SellActionBelowMinimumFee_AppliesMinimumFee()
        {
            var service = new CryptoTradeCalculationService();
            var (estimatedFee, totalAmount) = service.CalculateTradePreview("ETH", "SELL", 0.001m);

            Assert.Equal(0.50m, estimatedFee);
            Assert.Equal(2.50m, totalAmount);
        }

        [Fact]
        public void CanExecuteTrade_IsSubmittingTrue_ReturnsFalse()
        {
            var service = new CryptoTradeCalculationService();
            var result = service.CanExecuteTrade(true, "1", "BUY", 1000m, 5000m);
            Assert.False(result);
        }

        [Fact]
        public void CanExecuteTrade_InvalidQuantity_ReturnsFalse()
        {
            var service = new CryptoTradeCalculationService();
            var result = service.CanExecuteTrade(false, "abc", "BUY", 1000m, 5000m);
            Assert.False(result);
        }

        [Fact]
        public void CanExecuteTrade_BuyWithInsufficientFunds_ReturnsFalse()
        {
            var service = new CryptoTradeCalculationService();
            var result = service.CanExecuteTrade(false, "1", "BUY", 6000m, 5000m);
            Assert.False(result);
        }

        [Fact]
        public void CanExecuteTrade_BuyWithSufficientFunds_ReturnsTrue()
        {
            var service = new CryptoTradeCalculationService();
            var result = service.CanExecuteTrade(false, "1", "BUY", 4000m, 5000m);
            Assert.True(result);
        }

        [Fact]
        public void CanExecuteTrade_SellAction_ReturnsTrueRegardlessOfBalance()
        {
            var service = new CryptoTradeCalculationService();
            var result = service.CanExecuteTrade(false, "1", "SELL", 10000m, 5000m);
            Assert.True(result);
        }
    }
}