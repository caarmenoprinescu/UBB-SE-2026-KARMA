using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KarmaBanking.App.Models;
using KarmaBanking.App.Services.Interfaces;
using KarmaBanking.App.ViewModels;
using Moq;
using Xunit;

namespace KarmaBanking.App.Tests.Services
{
    public class InvestmentLogsViewModelTests
    {
        [Fact]
        public async Task LoadLogsAsync_SelectedTickerIsAll_CallsServiceWithNullTicker()
        {
            var mockInvestmentService = new Mock<IInvestmentService>();
            mockInvestmentService.Setup(service => service.GetInvestmentLogsAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                null))
                .ReturnsAsync(new List<InvestmentTransaction>());

            var viewModel = new InvestmentLogsViewModel(mockInvestmentService.Object);
            viewModel.SelectedTicker = "All";

            await viewModel.LoadLogsAsync();

            mockInvestmentService.Verify(service => service.GetInvestmentLogsAsync(
                1,
                null,
                null,
                null), Times.Once);
        }
    }
}