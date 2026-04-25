// <copyright file="InvestmentFilteringServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using System.Collections.Generic;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Services;
    using Xunit;

    public class InvestmentFilteringServiceTests
    {
        [Fact]
        public void FilterHoldingsByAssetType_NullHoldings_ReturnsEmpty()
        {
            // Arrange
            var investmentFilteringService = new InvestmentFilteringService();

            // Act
            var filteredHoldings = investmentFilteringService.FilterHoldingsByAssetType(null, "Stocks");

            // Assert
            Assert.Empty(filteredHoldings);
        }

        [Theory]
        [InlineData("Stock", "Stocks", true)]
        [InlineData("ETF", "Stocks", false)]
        [InlineData("ETFs", "ETFs", true)]
        [InlineData("Crypto", "Crypto", true)]
        [InlineData("Commodities", "All", true)]
        public void FilterHoldingsByAssetType_VariousFilters_ReturnsExpectedMatch(
            string assetType,
            string filterType,
            bool shouldMatch)
        {
            // Arrange
            var investmentFilteringService = new InvestmentFilteringService();
            var investmentHoldings = new List<InvestmentHolding>
            {
                new InvestmentHolding { AssetType = assetType }
            };

            // Act
            var filteredHoldings = investmentFilteringService.FilterHoldingsByAssetType(investmentHoldings, filterType);

            // Assert
            if (shouldMatch)
            {
                Assert.Single(filteredHoldings);
            }
            else
            {
                Assert.Empty(filteredHoldings);
            }
        }
    }
}