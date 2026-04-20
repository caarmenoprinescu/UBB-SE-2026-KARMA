using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories.Interfaces;
using KarmaBanking.App.Services;
using KarmaBanking.App.Services.Interfaces;
using Moq;
using Xunit;

namespace KarmaBanking.App.Tests.Services
{
    public class InvestmentServiceTests
    {
        [Fact]
        public async Task ExecuteCryptoTradeAsync_InsufficientBalance_ThrowsArgumentException()
        {
            // 1. ARRANGE
            var mockRepository = new Mock<IInvestmentRepository>();

            // We mock a portfolio that has only $100.00
            var fakePortfolio = new Portfolio
            {
                IdentificationNumber = 1,
                TotalValue = 100.00m
            };

            mockRepository.Setup(repo => repo.GetPortfolio(It.IsAny<int>()))
                          .Returns(fakePortfolio);

            var service = new InvestmentService(mockRepository.Object);

            // 2. ACT & 3. ASSERT
            // We attempt to buy $1,000 worth of BTC when we only have $100
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.ExecuteCryptoTradeAsync(
                    portfolioIdentificationNumber: 1,
                    ticker: "BTC",
                    actionType: "BUY",
                    quantity: 1.0m,
                    pricePerUnit: 1000.0m);
            });
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_SufficientBalance_ReturnsTrue()
        {
            // 1. ARRANGE
            var mockRepository = new Mock<IInvestmentRepository>();

            // User has $10,000
            var fakePortfolio = new Portfolio { IdentificationNumber = 1, TotalValue = 10000.00m };

            mockRepository.Setup(repo => repo.GetPortfolio(It.IsAny<int>()))
                          .Returns(fakePortfolio);

            var service = new InvestmentService(mockRepository.Object);

            // 2. ACT
            // Buying $100 worth of BTC
            bool result = await service.ExecuteCryptoTradeAsync(1, "BTC", "BUY", 1.0m, 100.0m);

            // 3. ASSERT
            Assert.True(result);
        }

        [Fact]
        public async Task ExecuteCryptoTradeAsync_VerifyFeeCalculation_CallsRepositoryWithCorrectFee()
        {
            // 1. ARRANGE
            var mockRepository = new Mock<IInvestmentRepository>();
            var fakePortfolio = new Portfolio { IdentificationNumber = 1, TotalValue = 1000.00m };
            mockRepository.Setup(repo => repo.GetPortfolio(It.IsAny<int>())).Returns(fakePortfolio);

            var service = new InvestmentService(mockRepository.Object);

            // $100 trade * 1.5% fee = $1.50 fee
            decimal expectedFee = 1.50m;

            // 2. ACT
            await service.ExecuteCryptoTradeAsync(1, "BTC", "BUY", 1.0m, 100.0m);

            // 3. ASSERT
            // We verify that the repository's RecordCryptoTradeAsync was called with exactly $1.50 as the fee
            mockRepository.Verify(repo => repo.RecordCryptoTradeAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                expectedFee), Times.Once);
        }
    }
}