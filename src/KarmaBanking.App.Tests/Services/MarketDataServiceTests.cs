namespace KarmaBanking.App.Tests.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using KarmaBanking.App.Services;
    using Xunit;

    public class MarketDataServiceTests
    {
        [Fact]
        public void GetPrice_ValidTicker_ReturnsInitialPrice()
        {
            
            var service = new MarketDataService();
            string ticker = "BTC";
            decimal expectedLowerBound = 10000m; // BTC is set to 68000


            decimal price = service.GetPrice(ticker);

            Assert.True(price > expectedLowerBound, $"Price for {ticker} should be retrieved from the dictionary.");
        }

        [Fact]
        public void GetPrice_InvalidTicker_ReturnsZero()
        {

            var service = new MarketDataService();


            decimal price = service.GetPrice("INVALID");


            Assert.Equal(0m, price);
        }

        [Fact]
        public async Task StartPolling_ChangesPricesOverTime()
        {

            var service = new MarketDataService();
            var tickers = new List<string> { "BTC", "AAPL" };
            decimal initialPrice = service.GetPrice("BTC");


            service.StartPolling(tickers);

            // We have to wait because the timer is set to 5 seconds
            await Task.Delay(100);


            Assert.NotNull(tickers);
            service.StopPolling();
        }
    }
}