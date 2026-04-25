// <copyright file="InvestmentLogsViewModelTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Services.Interfaces;
    using KarmaBanking.App.ViewModels;
    using Moq;
    using Xunit;

    public class InvestmentLogsViewModelTests
    {
        [Fact]
        public async Task LoadLogsAsync_SelectedTickerIsAll_CallsServiceWithNullTicker()
        {
            // Arrange
            var investmentServiceMock = new Mock<IInvestmentService>();
            investmentServiceMock.Setup(service => service.GetInvestmentLogsAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                null))
                .ReturnsAsync(new List<InvestmentTransaction>());

            var investmentLogsViewModel = new InvestmentLogsViewModel(investmentServiceMock.Object);
            investmentLogsViewModel.SelectedTicker = "All";

            // Act
            await investmentLogsViewModel.LoadLogsAsync();

            // Assert
            investmentServiceMock.Verify(service => service.GetInvestmentLogsAsync(
                1,
                null,
                null,
                null), Times.Once);
        }
    }
}