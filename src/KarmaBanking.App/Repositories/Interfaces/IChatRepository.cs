namespace KarmaBanking.App.Repositories.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using KarmaBanking.App.Models;
    public interface IChatRepository
    {
        Task<List<ChatMessage>> GetChatMessagesAsync(int chatSessionId);
        void SaveSessionRatingAndFeedback(int sessionId, int rating, string feedback);
        Task<int> CreateChatSessionAsync(int userId, string issueCategory);
        Task AddChatMessageAsync(ChatMessage message);
        Task<List<ChatSession>> GetChatSessionsAsync();
    }
}
