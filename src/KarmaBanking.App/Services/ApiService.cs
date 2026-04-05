using KarmaBanking.App.Models;
using KarmaBanking.App.Models.DTOs;
using KarmaBanking.App.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KarmaBanking.App.Services
{
    public class ApiService
    {
        private readonly string baseUrl = "https://localhost:5001";
        private readonly string authToken = "";

        // ── Savings API ──────────────────────────────────────────────────────

        // GET /api/savings?userId={userId}&includesClosed={includesClosed}
        public async Task<List<SavingsAccount>> GetSavingsAccountsAsync(int userId, bool includesClosed = false)
        {
            using var client = BuildClient();
            var response = await client.GetAsync(
                $"/api/savings?userId={userId}&includesClosed={includesClosed}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<SavingsAccount>>(json, JsonOptions)
                   ?? new List<SavingsAccount>();
        }

        // POST /api/savings
        public async Task<SavingsAccount> CreateSavingsAccountAsync(CreateSavingsAccountDto dto)
        {
            using var client = BuildClient();
            var body = new StringContent(
                JsonSerializer.Serialize(dto, JsonOptions),
                Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/savings", body);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SavingsAccount>(json, JsonOptions)!;
        }

        // POST /api/savings/{id}/deposit
        public async Task<DepositResponseDto> DepositAsync(int accountId, decimal amount, string source)
        {
            using var client = BuildClient();
            var dto = new DepositRequestDto { AccountId = accountId, Amount = amount, Source = source };
            var body = new StringContent(
                JsonSerializer.Serialize(dto, JsonOptions),
                Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"/api/savings/{accountId}/deposit", body);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DepositResponseDto>(json, JsonOptions)!;
        }

        // ── Chat / Attachment ────────────────────────────────────────────────

        public async Task<AttachmentUploadResponse?> UploadAttachmentAsync(int messageId, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is required.");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

            if (!string.IsNullOrWhiteSpace(authToken))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", authToken);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(messageId.ToString()), "messageId");

            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(filePath));
            form.Add(fileContent, "file", Path.GetFileName(filePath));

            HttpResponseMessage response = await client.PostAsync("/attachments", form);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Upload failed: {response.StatusCode} - {error}");
            }

            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AttachmentUploadResponse>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<int> CreateChatSessionAsync(int userId, string issueCategory)
        {
            ChatSessionRepository repo = new ChatSessionRepository();
            return await repo.CreateChatSessionAsync(userId, issueCategory);
        }

        public void SubmitFeedback(int sessionId, int rating, string feedback)
        {
            ChatSessionRepository repo = new ChatSessionRepository();
            repo.SaveSessionRatingAndFeedback(sessionId, rating, feedback);
        }

        public void EmailSessionTranscript(int sessionId, string recipientEmail)
        {
            EmailTranscriptService emailService = new EmailTranscriptService();
            emailService.SendSessionTranscript(sessionId, recipientEmail);
        }

        public virtual async Task<List<ChatMessage>?> GetChatHistoryAsync(int sessionId)
        {
            using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

            if (!string.IsNullOrWhiteSpace(authToken))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", authToken);

            HttpResponseMessage response = await client.GetAsync($"/chat/{sessionId}/history");

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to load chat history: {response.StatusCode} - {error}");
            }

            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ChatMessage>>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<bool> SaveAutoDepositSettingsAsync(int savingsAccountId, decimal amount, string frequency)
        {
            await Task.CompletedTask;
            return true;
        }

        private HttpClient BuildClient()
        {
            var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
            if (!string.IsNullOrWhiteSpace(authToken))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", authToken);
            return client;
        }

        private static readonly JsonSerializerOptions JsonOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private string GetContentType(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".pdf"  => "application/pdf",
                ".png"  => "image/png",
                ".jpg"  => "image/jpeg",
                ".jpeg" => "image/jpeg",
                _       => "application/octet-stream"
            };
        }
    }
}
