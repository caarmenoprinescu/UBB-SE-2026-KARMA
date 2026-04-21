namespace KarmaBanking.App.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Repositories.Interfaces;
    using KarmaBanking.App.Services;
    using Moq;
    using Xunit;

    public class InvestmentServiceTests
    {
        private readonly Mock<IInvestmentRepository> mockRepository;
        private readonly InvestmentService investmentService;

        public InvestmentServiceTests()
        {
            this.mockRepository = new Mock<IInvestmentRepository>();
            this.investmentService = new InvestmentService(this.mockRepository.Object);
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_CalculatesPercentageFeeCorrectly()
        {
            //$1000 trade at 1.5% should be a $15.00 fee
            int portfolioId = 1;
            decimal quantity = 10m;
            decimal price = 100m; // Total value $1000
            this.mockRepository.Setup(repo => repo.GetPortfolio(portfolioId))
                .Returns(new Portfolio { TotalValue = 2000m });

            await this.investmentService.ExecuteCryptoTradeAsync(portfolioId, "BTC", "BUY", quantity, price);

            //Verify RecordCryptoTradeAsync was called with a $15.00 fee
            this.mockRepository.Verify(repo => repo.RecordCryptoTradeAsync(
                portfolioId, "BTC", "BUY", quantity, price, 15.00m), Times.Once);
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_AppliesMinimumFee_WhenTradeIsSmall()
        {
            //$10 trade at 1.5% is $0.15, which is below the $0.50 minimum
            int portfolioId = 1;
            this.mockRepository.Setup(repo => repo.GetPortfolio(portfolioId))
                .Returns(new Portfolio { TotalValue = 100m });

            await this.investmentService.ExecuteCryptoTradeAsync(portfolioId, "BTC", "BUY", 1m, 10m);

            //Verify the fee was bumped up to the $0.50 minimum
            this.mockRepository.Verify(repo => repo.RecordCryptoTradeAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<decimal>(), 0.50m), Times.Once);
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_ThrowsException_WhenFundsAreInsufficient()
        {
            //User has $10, but trade + fee costs more
            int portfolioId = 1;
            this.mockRepository.Setup(repo => repo.GetPortfolio(portfolioId))
                .Returns(new Portfolio { TotalValue = 10m });

            await Assert.ThrowsAsync<ArgumentException>(() =>
                this.investmentService.ExecuteCryptoTradeAsync(portfolioId, "BTC", "BUY", 1m, 20m));
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_ValidatesInputs_ThrowsOnZeroQuantity()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                this.investmentService.ExecuteCryptoTradeAsync(1, "BTC", "BUY", 0, 100));
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_ThrowsException_WhenPriceIsNegative()
        {

            await Assert.ThrowsAsync<ArgumentException>(() =>
                this.investmentService.ExecuteCryptoTradeAsync(1, "BTC", "BUY", 1m, -100m));
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_SellOrder_DoesNotCheckPortfolioBalance()
        {
            //User has $0, but they are SELLING, so it should still proceed
            int portfolioId = 1;
            this.mockRepository.Setup(repo => repo.GetPortfolio(portfolioId))
                .Returns(new Portfolio { TotalValue = 0m });


            var result = await this.investmentService.ExecuteCryptoTradeAsync(portfolioId, "BTC", "SELL", 1m, 50000m);

            Assert.True(result);
            this.mockRepository.Verify(repo => repo.RecordCryptoTradeAsync(
                portfolioId, "BTC", "SELL", 1m, 50000m, It.IsAny<decimal>()), Times.Once);
        }

        [Fact]
        public async Task GetInvestmentLogsAsync_ReturnsDataFromRepository()
        {
            int portfolioId = 1;
            var expectedLogs = new List<InvestmentTransaction>
            {
                new InvestmentTransaction { Ticker = "BTC", Quantity = 1.0m, PricePerUnit = 45000m }
            };

            this.mockRepository.Setup(repo => repo.GetInvestmentLogsAsync(portfolioId, null, null, null))
                .ReturnsAsync(expectedLogs);

            var result = await this.investmentService.GetInvestmentLogsAsync(portfolioId);

            Assert.Single(result);
            Assert.Equal("BTC", result[0].Ticker);
        }
    }
}