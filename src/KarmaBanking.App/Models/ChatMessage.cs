using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KarmaBanking.App.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int SessionId { get; set; } 
        public string SenderType { get; set; }  // user/bot/agent
        public int? SenderId { get; set; }
        public DateTime SentAt { get; set; }
        public string AttachmentType { get; set; }
    }
}
