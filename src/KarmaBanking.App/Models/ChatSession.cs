using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KarmaBanking.App.Models
{
    public class ChatSession
    {
        public int id { get; set; }
        public int userId { get; set; }
        public string issueCategory { get; set; }
        public string sessionStatus { get; set; }
        public int rating { get; set; }
        public string feedback { get; set; }
        public DateTime startedAt { get; set; }
        public DateTime endedAt { get; set; }
    }
}
