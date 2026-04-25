// <copyright file="ChatServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using System.Threading.Tasks;
    using KarmaBanking.App.Repositories.Interfaces;
    using KarmaBanking.App.Services;
    using KarmaBanking.App.Services.Interfaces;
    using Moq;
    using Xunit;

    public class ApiServiceChatTests
    {
        private readonly Mock<IChatRepository> mockChatRepository;
        private readonly Mock<ILoanService> mockLoanService;
        private readonly ApiService apiService;

        public ApiServiceChatTests()
        {
            this.mockChatRepository = new Mock<IChatRepository>();
            this.mockLoanService = new Mock<ILoanService>();
            this.apiService = new ApiService(this.mockLoanService.Object, this.mockChatRepository.Object);
        }

        [Fact]
        public async Task CreateChatSessionAsync_CallsRepositoryAndReturnsId()
        {
            this.mockChatRepository.Setup(repo => repo.CreateChatSessionAsync(1, "Support")).ReturnsAsync(100);

            var result = await this.apiService.CreateChatSessionAsync(1, "Support");

            Assert.Equal(100, result);
            this.mockChatRepository.Verify(repo => repo.CreateChatSessionAsync(1, "Support"), Times.Once);
        }
    }
}