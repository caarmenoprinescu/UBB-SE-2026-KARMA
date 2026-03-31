using System;

namespace KarmaBanking.App.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string SenderType { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
    }
}