// <copyright file="CryptoTradeCalculationServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using KarmaBanking.App.Services;
    using Xunit;

    public class CryptoTradeCalculationServiceTests
    {
        [Fact]
        public void TryParsePositiveQuantity_ValidPositiveQuantity_ReturnsTrueAndParsedValue()
        {
            // Arrange
            var service = new CryptoTradeCalculationService();
            string quantityTextValue = "10.5";

            // Act
            bool isParsedSuccessfully = service.TryParsePositiveQuantity(quantityTextValue, out decimal parsedQuantity);

            // Assert
            Assert.True(isParsedSuccessfully);
            Assert.Equal(10.5m, parsedQuantity);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-5")]
        [InlineData("abc")]
        [InlineData("")]
        [InlineData(null)]
        public void TryParsePositiveQuantity_InvalidOrNonPositiveQuantity_ReturnsFalseAndZero(string quantityTextValue)
        {
            // Arrange
            var service = new CryptoTradeCalculationService();

            // Act
            bool isParsedSuccessfully = service.TryParsePositiveQuantity(quantityTextValue, out decimal parsedQuantity);

            // Assert
            Assert.False(isParsedSuccessfully);
            Assert.Equal(0m, parsedQuantity);
        }

        [Fact]
        public void GetMockMarketPrice_BitcoinTicker_ReturnsExpectedPrice()
        {
            // Arrange
            var service = new CryptoTradeCalculationService();

            // Act
            decimal marketPrice = service.GetMockMarketPrice("BTC");

            // Assert
            Assert.Equal(65000m, marketPrice);
        }

        [Fact]
        public void GetMockMarketPrice_EthereumTicker_ReturnsExpectedPrice()
        {
            // Arrange
            var service = new CryptoTradeCalculationService();

            // Act
            decimal marketPrice = service.GetMockMarketPrice("ETH");

            // Assert
            Assert.Equal(3000m, marketPrice);
        }

        [Fact]
        public void CalculateTradePreview_BuyActionAboveMinimumFee_CalculatesCorrectly()
        {
            // Arrange
            var service = new CryptoTradeCalculationService();

            // Act
            var (estimatedFee, totalAmount) = service.CalculateTradePreview("BTC", "BUY", 1m);

            // Assert
            Assert.Equal(975m, estimatedFee);
            Assert.Equal(65975m, totalAmount);
        }

        [Fact]
        public void CalculateTradePreview_SellActionBelowMinimumFee_AppliesMinimumFee()
        {
            // Arrange
            var service = new CryptoTradeCalculationService();

            // Act
            var (estimatedFee, totalAmount) = service.CalculateTradePreview("ETH", "SELL", 0.001m);

            // Assert
            Assert.Equal(0.50m, estimatedFee);
            Assert.Equal(2.50m, totalAmount);
        }

        [Fact]
        public void CanExecuteTrade_IsSubmittingTrue_ReturnsFalse()
        {
            // Arrange
            var service = new CryptoTradeCalculationService();

            // Act
            bool canExecute = service.CanExecuteTrade(true, "1", "BUY", 1000m, 5000m);

            // Assert
            Assert.False(canExecute);
        }

        [Fact]
        public void CanExecuteTrade_InvalidQuantity_ReturnsFalse()
        {
            // Arrange
            var service = new CryptoTradeCalculationService();

            // Act
            bool canExecute = service.CanExecuteTrade(false, "abc", "BUY", 1000m, 5000m);

            // Assert
            Assert.False(canExecute);
        }

        [Fact]
        public void CanExecuteTrade_BuyWithInsufficientFunds_ReturnsFalse()
        {
            // Arrange
            var service = new CryptoTradeCalculationService();

            // Act
            bool canExecute = service.CanExecuteTrade(false, "1", "BUY", 6000m, 5000m);

            // Assert
            Assert.False(canExecute);
        }

        [Fact]
        public void CanExecuteTrade_BuyWithSufficientFunds_ReturnsTrue()
        {
            // Arrange
            var service = new CryptoTradeCalculationService();

            // Act
            bool canExecute = service.CanExecuteTrade(false, "1", "BUY", 4000m, 5000m);

            // Assert
            Assert.True(canExecute);
        }

        [Fact]
        public void CanExecuteTrade_SellAction_ReturnsTrueRegardlessOfBalance()
        {
            // Arrange
            var service = new CryptoTradeCalculationService();

            // Act
            bool canExecute = service.CanExecuteTrade(false, "1", "SELL", 10000m, 5000m);

            // Assert
            Assert.True(canExecute);
        }

        [Fact]
        public void CalculateTradePreview_BuyActionBelowMinimumFee_AppliesMinimumFee()
        {
            // Arrange
            var service = new CryptoTradeCalculationService();

            // Act
            var (estimatedFee, totalAmount) = service.CalculateTradePreview("ETH", "BUY", 0.001m);

            // Assert
            Assert.Equal(0.50m, estimatedFee);
            Assert.Equal(3.50m, totalAmount);
        }

        [Fact]
        public void CalculateTradePreview_SellActionAboveMinimumFee_CalculatesCorrectly()
        {
            // Arrange
            var service = new CryptoTradeCalculationService();

            // Act
            var (estimatedFee, totalAmount) = service.CalculateTradePreview("BTC", "SELL", 1m);

            // Assert
            Assert.Equal(975m, estimatedFee);
            Assert.Equal(64025m, totalAmount);
        }

        [Fact]
        public void CanExecuteTrade_OtherActionType_ReturnsTrue()
        {
            // Arrange
            var service = new CryptoTradeCalculationService();

            // Act
            bool canExecute = service.CanExecuteTrade(false, "1", "CONVERT", 10000m, 5000m);

            // Assert
            Assert.True(canExecute);
        }
    }
}