using KarmaBanking.App.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace KarmaBanking.App.Services
{
    public class ApiService
    {
        private readonly string baseUrl = "https://localhost:5001";
        private readonly string authToken = "";

        public async Task<AttachmentUploadResponse?> UploadAttachmentAsync(int messageId, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is required.");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            using var client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };

            if (!string.IsNullOrWhiteSpace(authToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", authToken);
            }

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
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }

        public void SubmitFeedback(int sessionId, int rating, string feedback)
        {
            ChatSessionRepository repo = new ChatSessionRepository();
            repo.SaveSessionRatingAndFeedback(sessionId, rating, feedback);
        }

        private string GetContentType(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            return ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };
        }
    }
}