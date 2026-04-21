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
            // Arrange: $1000 trade at 1.5% should be a $15.00 fee. 
            // Starting with 0 holdings, final average price should be $100.
            int portfolioId = 1;
            decimal quantity = 10m;
            decimal price = 100m;
            this.mockRepository.Setup(repo => repo.GetPortfolio(portfolioId))
                .Returns(new Portfolio { TotalValue = 2000m, Holdings = new List<InvestmentHolding>() });

            // Act
            await this.investmentService.ExecuteCryptoTradeAsync(portfolioId, "BTC", "BUY", quantity, price);

            // Assert: Verify RecordCryptoTradeAsync is called with the fee AND calculated final values
            this.mockRepository.Verify(repo => repo.RecordCryptoTradeAsync(
                portfolioId, "BTC", "BUY", quantity, price, 15.00m, 10m, 100m), Times.Once);
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_AppliesMinimumFee_WhenTradeIsSmall()
        {
            // Arrange
            int portfolioId = 1;
            this.mockRepository.Setup(repo => repo.GetPortfolio(portfolioId))
                .Returns(new Portfolio { TotalValue = 100m, Holdings = new List<InvestmentHolding>() });

            // Act
            await this.investmentService.ExecuteCryptoTradeAsync(portfolioId, "BTC", "BUY", 1m, 10m);

            // Assert: Using It.IsAny<decimal>() for the final quantity/price to focus on the fee check
            this.mockRepository.Verify(repo => repo.RecordCryptoTradeAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(),
                It.IsAny<decimal>(), 0.50m, It.IsAny<decimal>(), It.IsAny<decimal>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_ThrowsException_WhenFundsAreInsufficient()
        {
            // Arrange
            int portfolioId = 1;
            this.mockRepository.Setup(repo => repo.GetPortfolio(portfolioId))
                .Returns(new Portfolio { TotalValue = 10m, Holdings = new List<InvestmentHolding>() });

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                this.investmentService.ExecuteCryptoTradeAsync(portfolioId, "BTC", "BUY", 1m, 20m));
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_ValidatesInputs_ThrowsOnZeroQuantity()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                this.investmentService.ExecuteCryptoTradeAsync(1, "BTC", "BUY", 0, 100));
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_ThrowsException_WhenPriceIsNegative()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                this.investmentService.ExecuteCryptoTradeAsync(1, "BTC", "BUY", 1m, -100m));
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_SellOrder_ValidatesAssetQuantity()
        {
            // Arrange: User has 5 BTC, tries to sell 10. Should throw error in Service logic.
            int portfolioId = 1;
            var portfolio = new Portfolio { TotalValue = 1000m };
            portfolio.Holdings.Add(new InvestmentHolding { Ticker = "BTC", Quantity = 5m });

            this.mockRepository.Setup(repo => repo.GetPortfolio(portfolioId))
                .Returns(portfolio);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                this.investmentService.ExecuteCryptoTradeAsync(portfolioId, "BTC", "SELL", 10m, 50000m));
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_SellOrder_SucceedsWithSufficientAssets()
        {
            // Arrange: User has 10 BTC, sells 1. Final quantity should be 9.
            int portfolioId = 1;
            var portfolio = new Portfolio { TotalValue = 1000m };
            portfolio.Holdings.Add(new InvestmentHolding { Ticker = "BTC", Quantity = 10m, AveragePurchasePrice = 20000m });

            this.mockRepository.Setup(repo => repo.GetPortfolio(portfolioId))
                .Returns(portfolio);

            // Act
            var result = await this.investmentService.ExecuteCryptoTradeAsync(portfolioId, "BTC", "SELL", 1m, 50000m);

            // Assert
            Assert.True(result);
            this.mockRepository.Verify(repo => repo.RecordCryptoTradeAsync(
                portfolioId, "BTC", "SELL", 1m, 50000m, It.IsAny<decimal>(), 9m, 20000m), Times.Once);
        }

        [Fact]
        public async Task GetInvestmentLogsAsync_ReturnsDataFromRepository()
        {
            // Arrange
            int portfolioId = 1;
            var expectedLogs = new List<InvestmentTransaction>
            {
                new InvestmentTransaction { Ticker = "BTC", Quantity = 1.0m, PricePerUnit = 45000m }
            };

            this.mockRepository.Setup(repo => repo.GetInvestmentLogsAsync(portfolioId, null, null, null))
                .ReturnsAsync(expectedLogs);

            // Act
            var result = await this.investmentService.GetInvestmentLogsAsync(portfolioId);

            // Assert
            Assert.Single(result);
            Assert.Equal("BTC", result[0].Ticker);
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_ThrowsOnEmptyTicker()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                this.investmentService.ExecuteCryptoTradeAsync(1, " ", "BUY", 1m, 100m));
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_ThrowsOnInvalidAction()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                this.investmentService.ExecuteCryptoTradeAsync(1, "BTC", "HOLD", 1m, 100m));
        }

        [Fact]
        public void GetPortfolio_ReturnsPortfolioFromRepository()
        {
            // Arrange
            int userId = 123;
            var expectedPortfolio = new Portfolio { IdentificationNumber = 1, TotalValue = 500m };
            this.mockRepository.Setup(repo => repo.GetPortfolio(userId)).Returns(expectedPortfolio);

            // Act
            var result = this.investmentService.GetPortfolio(userId);

            // Assert
            Assert.Equal(expectedPortfolio.IdentificationNumber, result.IdentificationNumber);
            this.mockRepository.Verify(repo => repo.GetPortfolio(userId), Times.Once);
        }

        [Fact]
        public async Task GetInvestmentLogsAsync_ThrowsOnInvalidPortfolioId()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                this.investmentService.GetInvestmentLogsAsync(0));
        }

        [Fact]
        public async Task GetInvestmentLogsAsync_ThrowsWhenStartDateAfterEndDate()
        {
            // Arrange
            DateTime start = DateTime.Now;
            DateTime end = start.AddDays(-1);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                this.investmentService.GetInvestmentLogsAsync(1, start, end));
        }
    }
}