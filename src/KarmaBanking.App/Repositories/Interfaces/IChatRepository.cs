using KarmaBanking.App.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KarmaBanking.App.Repositories.Interfaces
{
    public interface IChatRepository
    {
        Task<List<ChatMessage>> GetChatMessagesAsync(int chatSessionId);
    }
}
