namespace KarmaBanking.App.Tests.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Services;
    using Xunit;

    public class InvestmentFilteringServiceTests
    {
        [Fact]
        public void FilterHoldingsByAssetType_NullHoldings_ReturnsEmpty()
        {
            var service = new InvestmentFilteringService();
            var result = service.FilterHoldingsByAssetType(null, "Stocks");
            Assert.Empty(result);
        }

        [Fact]
        public void FilterHoldingsByAssetType_NullHoldingElement_IgnoresNull()
        {
            var service = new InvestmentFilteringService();
            var holdings = new List<InvestmentHolding> { null };

            var result = service.FilterHoldingsByAssetType(holdings, "Stocks");

            Assert.Empty(result);
        }

        [Theory]
        [InlineData("Stock", "Stocks", true)]
        [InlineData("Stocks", "Stocks", true)]
        [InlineData("ETF", "Stocks", false)]
        [InlineData("ETF", "ETFs", true)]
        [InlineData("ETFs", "ETFs", true)]
        [InlineData("Bond", "Bonds", true)]
        [InlineData("Bonds", "Bonds", true)]
        [InlineData("Crypto", "Crypto", true)]
        [InlineData("Real Estate", "Other", true)]
        [InlineData("Stock", "Other", false)]
        [InlineData("Commodities", "All", true)]
        public void FilterHoldingsByAssetType_VariousFilters_ReturnsExpectedMatch(string assetType, string filter, bool shouldMatch)
        {
            var service = new InvestmentFilteringService();
            var holdings = new List<InvestmentHolding>
            {
                new InvestmentHolding { AssetType = assetType }
            };

            var result = service.FilterHoldingsByAssetType(holdings, filter);

            if (shouldMatch)
            {
                Assert.Single(result);
            }
            else
            {
                Assert.Empty(result);
            }
        }
    }
}