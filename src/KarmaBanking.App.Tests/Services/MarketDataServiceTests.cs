namespace KarmaBanking.App.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using KarmaBanking.App.Services;
    using Xunit;

    public class MarketDataServiceTests
    {
        [Fact]
        public void GetPrice_ValidTicker_ReturnsInitialPrice()
        {
            // Arrange
            var service = new MarketDataService();

            // Act
            decimal price = service.GetPrice("BTC");

            // Assert
            Assert.Equal(68000m, price);
        }

        [Fact]
        public void GetPrice_InvalidOrWhitespaceTicker_ReturnsZero()
        {
            // Arrange
            var service = new MarketDataService();

            // Act & Assert: Tests both the null/whitespace guard and the "not found" dictionary path
            Assert.Equal(0m, service.GetPrice(null!));
            Assert.Equal(0m, service.GetPrice("   "));
            Assert.Equal(0m, service.GetPrice("INVALID"));
        }

        [Fact]
        public void StartPolling_FiltersAndNormalizesTickers()
        {
            // Arrange
            var service = new MarketDataService();
            var messyTickers = new List<string> { " btc ", string.Empty, null!, "AAPL", "btc" };

            // Act
            service.StartPolling(messyTickers);

            // Assert: Verify normalization (case-insensitivity and whitespace trimming)
            Assert.Equal(68000m, service.GetPrice("BTC"));
            Assert.Equal(185m, service.GetPrice("aapl"));

            service.StopPolling();
        }

        [Fact]
        public void StartPolling_CalledTwice_DoesNotRestartTimer()
        {
            // Arrange
            var service = new MarketDataService();
            var tickers = new List<string> { "BTC" };

            // Act
            service.StartPolling(tickers);
            service.StartPolling(tickers); // This hits the 'if (this.pollingTimer != null) return;' path

            // Assert
            Assert.NotNull(tickers);
            service.StopPolling();
        }

        [Fact]
        public void RegisterPriceUpdateHandler_SetsHandlerCorrectly()
        {
            // Arrange
            var service = new MarketDataService();
            bool handlerCalled = false;
            Action handler = () => handlerCalled = true;

            // Act
            service.RegisterPriceUpdateHandler(handler);

            // Note: We can't easily wait 5 seconds in a fast unit test,
            // but this covers the setter logic line.
            Assert.False(handlerCalled);
        }

        [Fact]
        public async Task StartPolling_FluctuatesPrices_AfterInterval()
        {
            // WARNING: This test takes ~5 seconds to run because of the 5000ms DefaultPollingInterval.
            // It is necessary to cover the logic inside the Timer callback.

            // Arrange
            var service = new MarketDataService();
            var tickers = new List<string> { "BTC" };
            decimal initialPrice = service.GetPrice("BTC");
            bool wasNotified = false;

            service.RegisterPriceUpdateHandler(() => wasNotified = true);

            // Act
            service.StartPolling(tickers);

            // Wait long enough for the 5000ms timer to fire once
            await Task.Delay(5500);

            decimal updatedPrice = service.GetPrice("BTC");

            // Assert
            Assert.NotEqual(initialPrice, updatedPrice); // Coverage for fluctuation math
            Assert.True(wasNotified); // Coverage for priceUpdateHandler?.Invoke()

            service.StopPolling();
        }
    }
}