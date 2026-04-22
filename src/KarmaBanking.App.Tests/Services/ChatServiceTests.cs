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
            int userId = 1;
            string category = "Technical Support";
            int expectedSessionId = 100;
            this.mockChatRepository.Setup(repo => repo.CreateChatSessionAsync(userId, category))
                .ReturnsAsync(expectedSessionId);

            int result = await this.apiService.CreateChatSessionAsync(userId, category);

            Assert.Equal(expectedSessionId, result);
            this.mockChatRepository.Verify(repo => repo.CreateChatSessionAsync(userId, category), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_CallsRepositoryWithCorrectData()
        {
            var message = new ChatMessage
            {
                SessionId = 100, // You defined it as 100 here
                Content = "Test message",
                SenderType = "User",
                SentAt = DateTime.Now
            };

            await this.apiService.SendMessageAsync(message);

            this.mockChatRepository.Verify(repo => repo.AddChatMessageAsync(It.Is<ChatMessage>(m =>
                m.SessionId == 100 &&
                m.Content == "Test message" &&
                m.SenderType == "User")), Times.Once);
        }

        [Fact]
        public async Task GetUserChatSessionsAsync_ReturnsListFromRepository()
        {
            var expectedSessions = new List<ChatSession>
            {
                new ChatSession { Id = 1, IssueCategory = "Billing" },
                new ChatSession { Id = 2, IssueCategory = "Security" }
            };
            this.mockChatRepository.Setup(repo => repo.GetChatSessionsAsync())
                .ReturnsAsync(expectedSessions);

            var result = await this.apiService.GetUserChatSessionsAsync();

            Assert.Equal(2, result.Count);
            Assert.Equal("Billing", result[0].IssueCategory);
            this.mockChatRepository.Verify(repo => repo.GetChatSessionsAsync(), Times.Once);
        }

        [Fact]
        public void SubmitFeedback_CallsRepositoryWithCorrectParameters()
        {
            int sessionId = 100;
            int rating = 5;
            string feedback = "Great service!";

            this.apiService.SubmitFeedback(sessionId, rating, feedback);

            this.mockChatRepository.Verify(repo => repo.SaveSessionRatingAndFeedback(
                sessionId, rating, feedback), Times.Once);
        }

        [Fact]
        public async Task GetChatbotPresetQuestionsAsync_ReturnsKeysFromDefaultDictionary()
        {
            var questions = await this.apiService.GetChatbotPresetQuestionsAsync();

            Assert.Contains("How do I reset my password?", questions);
            Assert.Contains("Why was my card declined?", questions);
            Assert.True(questions.Count >= 5);
        }

        [Fact]
        public async Task GetChatbotPresetAnswerAsync_ReturnsCorrectAnswerForKnownQuestion()
        {
            string question = "How long does a transfer take?";

            var answer = await this.apiService.GetChatbotPresetAnswerAsync(question);

            Assert.Contains("Internal transfers are usually immediate", answer);
        }

        [Fact]
        public async Task GetChatbotPresetAnswerAsync_ReturnsDefaultMessageForUnknownQuestion()
        {
            string question = "What is the meaning of life?";

            var answer = await this.apiService.GetChatbotPresetAnswerAsync(question);

            Assert.Equal("Please contact the team for more help with this topic.", answer);
        }

        [Fact]
        public async Task SendChatToSupportAsync_ReturnsTrue_WhenInputIsValid()
        {
            var result = await this.apiService.SendChatToSupportAsync("Transcript text", "Help me", null);

            Assert.True(result);
        }

        [Fact]
        public async Task SendChatToSupportAsync_ReturnsFalse_WhenInputsAreEmpty()
        {
            var result = await this.apiService.SendChatToSupportAsync(string.Empty, " ", null);

            Assert.False(result);
        }
    }
}