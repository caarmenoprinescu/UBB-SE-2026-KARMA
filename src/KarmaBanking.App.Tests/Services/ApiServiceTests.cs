// <copyright file="ApiServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Models.Enums;
    using KarmaBanking.App.Repositories.Interfaces;
    using KarmaBanking.App.Services;
    using KarmaBanking.App.Services.Interfaces;
    using Moq;
    using Xunit;

    public class ApiServiceTests
    {
        private readonly Mock<ILoanService> mockLoanService;
        private readonly Mock<IChatRepository> mockChatRepository;
        private readonly ApiService apiService;

        public ApiServiceTests()
        {
            this.mockLoanService = new Mock<ILoanService>();
            this.mockChatRepository = new Mock<IChatRepository>();
            this.apiService = new ApiService(this.mockLoanService.Object, this.mockChatRepository.Object);
        }

        [Fact]
        public async Task GetAllLoansAsync_CallsLoanService()
        {
            var expectedLoans = new List<Loan> { new Loan { IdentificationNumber = 1 } };
            this.mockLoanService.Setup(s => s.GetAllLoansAsync()).ReturnsAsync(expectedLoans);

            var result = await this.apiService.GetAllLoansAsync();

            Assert.Equal(expectedLoans, result);
            this.mockLoanService.Verify(s => s.GetAllLoansAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAmortizationAsync_CallsLoanService()
        {
            var expectedRows = new List<AmortizationRow> { new AmortizationRow { InstallmentNumber = 1 } };
            this.mockLoanService.Setup(s => s.GetAmortizationAsync(1)).ReturnsAsync(expectedRows);

            var result = await this.apiService.GetAmortizationAsync(1);

            Assert.Equal(expectedRows, result);
            this.mockLoanService.Verify(s => s.GetAmortizationAsync(1), Times.Once);
        }
    }
}