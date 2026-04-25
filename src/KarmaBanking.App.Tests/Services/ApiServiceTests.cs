namespace KarmaBanking.App.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Models.DTOs;
    using KarmaBanking.App.Models.Enums;
    using KarmaBanking.App.Repositories.Interfaces;
    using KarmaBanking.App.Services;
    using KarmaBanking.App.Services.Interfaces;
    using NSubstitute;
    using Xunit;

    public class ApiServiceTests
    {
        [Fact]
        public async Task GetChatbotPresetQuestionsAsync_ReturnsExpectedQuestions()
        {
            var service = new ApiService();

            var result = await service.GetChatbotPresetQuestionsAsync();

            Assert.Contains("How do I reset my password?", result);
            Assert.Contains("Why was my card declined?", result);
            Assert.Contains("How long does a transfer take?", result);
            Assert.True(result.Count >= 5);
        }

        [Theory]
        [InlineData("How do I reset my password?", "You can reset your password from the login screen by choosing Forgot password and following the verification steps.")]
        [InlineData("Why was my card declined?", "A card can be declined because of insufficient funds, an expired card, a blocked card, or a merchant validation issue. Please check the card status in the app first.")]
        public async Task GetChatbotPresetAnswerAsync_KnownQuestion_ReturnsExpectedAnswer(string question, string expectedAnswer)
        {
            var service = new ApiService();

            var result = await service.GetChatbotPresetAnswerAsync(question);

            Assert.Equal(expectedAnswer, result);
        }

        [Fact]
        public async Task GetChatbotPresetAnswerAsync_UnknownQuestion_ReturnsFallbackMessage()
        {
            var service = new ApiService();

            var result = await service.GetChatbotPresetAnswerAsync("What is the meaning of life?");

            Assert.Equal("Please contact the team for more help with this topic.", result);
        }

        [Fact]
        public async Task SendChatToSupportAsync_WithContent_ReturnsTrue()
        {
            var service = new ApiService();
            var attachment = new SelectedAttachment { FileName = "test.png" };

            var result = await service.SendChatToSupportAsync("Transcript data", "User message", attachment);

            Assert.True(result);
        }

        [Fact]
        public async Task SendChatToSupportAsync_NoContent_ReturnsFalse()
        {
            var service = new ApiService();

            var result = await service.SendChatToSupportAsync(string.Empty, string.Empty, null);

            Assert.False(result);
        }

        [Fact]
        public async Task GetAllLoansAsync_CallsLoanService()
        {
            var mockLoanService = Substitute.For<ILoanService>();
            var mockChatRepo = Substitute.For<IChatRepository>();
            var expectedLoans = new List<Loan> { new Loan { IdentificationNumber = 1 } };
            mockLoanService.GetAllLoansAsync().Returns(Task.FromResult(expectedLoans));

            var service = new ApiService(mockLoanService, mockChatRepo);
            var result = await service.GetAllLoansAsync();

            Assert.Equal(expectedLoans, result);
            await mockLoanService.Received(1).GetAllLoansAsync();
        }

        [Fact]
        public async Task ApplyForLoanAsync_CallsLoanServiceAndReturnsRejectionReason()
        {
            var mockLoanService = Substitute.For<ILoanService>();
            var mockChatRepo = Substitute.For<IChatRepository>();

            // FIX 1: Added the required 'Purpose' property
            var request = new LoanApplicationRequest
            {
                DesiredAmount = 5000,
                Purpose = "Home Renovation"
            };

            // FIX 2: Replaced 'false' with 'LoanApplicationStatus.Rejected'
            mockLoanService.SubmitLoanApplicationAsync(request)
                .Returns(Task.FromResult((LoanApplicationStatus.Rejected, "Credit score too low")));

            var service = new ApiService(mockLoanService, mockChatRepo);
            var result = await service.ApplyForLoanAsync(request);

            Assert.Equal("Credit score too low", result);
            await mockLoanService.Received(1).SubmitLoanApplicationAsync(request);
        }

        [Fact]
        public async Task CreateChatSessionAsync_CallsChatRepository()
        {
            var mockLoanService = Substitute.For<ILoanService>();
            var mockChatRepo = Substitute.For<IChatRepository>();
            mockChatRepo.CreateChatSessionAsync(1, "Account").Returns(Task.FromResult(99));

            var service = new ApiService(mockLoanService, mockChatRepo);
            var result = await service.CreateChatSessionAsync(1, "Account");

            Assert.Equal(99, result);
            await mockChatRepo.Received(1).CreateChatSessionAsync(1, "Account");
        }

        [Fact]
        public void SubmitFeedback_CallsChatRepository()
        {
            var mockLoanService = Substitute.For<ILoanService>();
            var mockChatRepo = Substitute.For<IChatRepository>();

            var service = new ApiService(mockLoanService, mockChatRepo);
            service.SubmitFeedback(1, 5, "Great service");

            mockChatRepo.Received(1).SaveSessionRatingAndFeedback(1, 5, "Great service");
        }

        [Fact]
        public async Task GetAmortizationAsync_CallsLoanService()
        {
            var mockLoanService = Substitute.For<ILoanService>();
            var mockChatRepo = Substitute.For<IChatRepository>();
            var expectedRows = new List<AmortizationRow> { new AmortizationRow { InstallmentNumber = 1 } };
            mockLoanService.GetAmortizationAsync(1).Returns(Task.FromResult(expectedRows));

            var service = new ApiService(mockLoanService, mockChatRepo);
            var result = await service.GetAmortizationAsync(1);

            Assert.Equal(expectedRows, result);
            await mockLoanService.Received(1).GetAmortizationAsync(1);
        }
    }
}