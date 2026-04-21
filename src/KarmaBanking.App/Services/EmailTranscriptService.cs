// <copyright file="EmailTranscriptService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Text;
using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories;

public class EmailTranscriptService
{
    private readonly ChatMessageRepository chatMessageRepository;

    public EmailTranscriptService()
    {
        this.chatMessageRepository = new ChatMessageRepository();
    }

    public void SendSessionTranscript(int sessionIdentificationNumber, string recipientEmailAddress)
    {
        // Syncing with the refactored repository method name
        var messages = this.chatMessageRepository.GetMessagesBySessionIdentificationNumber(sessionIdentificationNumber);

        var emailSubject = $"Karma Banking - Chat Transcript #{sessionIdentificationNumber}";
        var emailBody = this.BuildTranscriptBody(sessionIdentificationNumber, messages);

        this.SendEmail(recipientEmailAddress, emailSubject, emailBody);
    }

    private string BuildTranscriptBody(int sessionIdentificationNumber, List<ChatMessage> messages)
    {
        var transcriptBuilder = new StringBuilder();

        transcriptBuilder.AppendLine($"Chat Transcript for Session #{sessionIdentificationNumber}");
        transcriptBuilder.AppendLine($"Generated at: {DateTime.Now}");
        transcriptBuilder.AppendLine(new string('-', 50));

        foreach (var message in messages)
        {
            // Note: Ensure ChatMessage.IdentificationNumber/Content are updated in the model
            transcriptBuilder.AppendLine(
                $"[{message.SentAt:yyyy-MM-dd HH:mm:ss}] {message.SenderType}: {message.Content}");
        }

        return transcriptBuilder.ToString();
    }

    private void SendEmail(string recipientEmailAddress, string emailSubject, string emailBody)
    {
        // Note: In a production environment, these credentials would be moved to a secure configuration file
        var mailMessage = new MailMessage
        {
            From = new MailAddress("noreply@karmabanking.com"),
        };

        mailMessage.To.Add(recipientEmailAddress);
        mailMessage.Subject = emailSubject;
        mailMessage.Body = emailBody;

        using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
        {
            smtpClient.Credentials = new NetworkCredential("your_email_here", "your_password_here");
            smtpClient.EnableSsl = true;

            try
            {
                smtpClient.Send(mailMessage);
            }
            catch (Exception exception)
            {
                // Logging the exception for diagnostic purposes
                Debug.WriteLine($"Email delivery failed: {exception.Message}");
            }
        }
    }
}