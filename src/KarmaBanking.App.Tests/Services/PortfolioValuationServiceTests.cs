// <copyright file="PortfolioValuationServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using System.Collections.Generic;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Services;
    using Xunit;

    public class PortfolioValuationServiceTests
    {
        private readonly PortfolioValuationService portfolioValuationService;

        public PortfolioValuationServiceTests()
        {
            this.portfolioValuationService = new PortfolioValuationService();
        }

        [Fact]
        public void UpdateHoldingValuation_UpdatesPriceAndCalculatesGainLoss()
        {
            // Arrange
            var investmentHoldingInstance = new InvestmentHolding
            {
                AveragePurchasePrice = 50m,
                Quantity = 10m
            };
            decimal newMarketPrice = 75m;

            // Act
            this.portfolioValuationService.UpdateHoldingValuation(investmentHoldingInstance, newMarketPrice);

            // Assert
            Assert.Equal(75m, investmentHoldingInstance.CurrentPrice);
            Assert.Equal(250m, investmentHoldingInstance.UnrealizedGainLoss);
        }

        [Fact]
        public void UpdatePortfolioTotals_WithPositiveTotalCost_CalculatesTotalsCorrectly()
        {
            // Arrange
            var userPortfolio = new Portfolio
            {
                Holdings = new List<InvestmentHolding>
                {
                    new InvestmentHolding { Quantity = 10m, AveragePurchasePrice = 100m, CurrentPrice = 150m, UnrealizedGainLoss = 500m },
                    new InvestmentHolding { Quantity = 5m, AveragePurchasePrice = 50m, CurrentPrice = 40m, UnrealizedGainLoss = -50m }
                }
            };

            // Act
            this.portfolioValuationService.UpdatePortfolioTotals(userPortfolio);

            // Assert
            Assert.Equal(1700m, userPortfolio.TotalValue);
            Assert.Equal(450m, userPortfolio.TotalGainLoss);
            Assert.Equal(36m, userPortfolio.GainLossPercent);
        }
    }
}