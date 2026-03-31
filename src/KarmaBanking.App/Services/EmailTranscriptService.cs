using KarmaBanking.App.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace KarmaBanking.App.Services
{
    public class EmailTranscriptService
    {
        private readonly ChatMessageRepository chatMessageRepository;

        public EmailTranscriptService()
        {
            chatMessageRepository = new ChatMessageRepository();
        }

        public void SendSessionTranscript(int sessionId, string recipientEmail)
        {
            List<ChatMessage> messages = chatMessageRepository.GetMessagesBySessionId(sessionId);

            string subject = $"Karma Banking - Chat Transcript #{sessionId}";
            string body = BuildTranscriptBody(sessionId, messages);

            SendEmail(recipientEmail, subject, body);
        }

        private string BuildTranscriptBody(int sessionId, List<ChatMessage> messages)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Chat Transcript for Session #{sessionId}");
            sb.AppendLine($"Generated at: {DateTime.Now}");
            sb.AppendLine(new string('-', 50));

            foreach (ChatMessage message in messages)
            {
                sb.AppendLine($"[{message.SentAt:yyyy-MM-dd HH:mm:ss}] {message.SenderType}: {message.Content}");
            }

            return sb.ToString();
        }

        private void SendEmail(string recipientEmail, string subject, string body)
        {
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("noreply@karmabanking.com");
            mail.To.Add(recipientEmail);
            mail.Subject = subject;
            mail.Body = body;

            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.Credentials = new NetworkCredential("your_email_here", "your_password_here");
            smtp.EnableSsl = true;

            smtp.Send(mail);
        }
    }
}