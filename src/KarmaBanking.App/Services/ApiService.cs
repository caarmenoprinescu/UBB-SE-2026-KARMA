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
        private readonly ILoanService _loanService;

        public ApiService() { }
        public ApiService(ILoanService loanService)
        {
            _loanService = loanService;
        }

        protected static readonly Dictionary<string, string> DefaultChatbotResponses = new Dictionary<string, string>
        {
            ["How do I reset my password?"] =
                "You can reset your password from the login screen by choosing Forgot password and following the verification steps.",
            ["Why was my card declined?"] =
                "A card can be declined because of insufficient funds, an expired card, a blocked card, or a merchant validation issue. Please check the card status in the app first.",
            ["How long does a transfer take?"] =
                "Internal transfers are usually immediate, while interbank transfers can take up to one business day depending on the destination bank.",
            ["How do I upload documents for support?"] =
                "Use the Attach File button in this chat after contacting the team. Your selected file will be included with the support request summary.",
            ["I found a technical problem in the app."] =
                "Please contact the team from this chat and include a short description of what happened. Screenshots or PDFs can help the team investigate faster."
        };


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

        // POST /api/savings/{id}/close
        public async Task<ClosureResult> CloseAccountAsync(int accountId, int destinationAccountId)
        {
            using var client = BuildClient();

            var dto = new
            {
                destinationAccountId = destinationAccountId,
                confirmClosure = true
            };

            var body = new StringContent(
                JsonSerializer.Serialize(dto, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync($"/api/savings/{accountId}/close", body);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Close failed: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ClosureResult>(json, JsonOptions)!;
        }

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

        public virtual Task<List<string>> GetChatbotPresetQuestionsAsync()
        {
            return Task.FromResult(new List<string>(DefaultChatbotResponses.Keys));
        }

        public virtual Task<string> GetChatbotPresetAnswerAsync(string question)
        {
            if (DefaultChatbotResponses.TryGetValue(question, out string? response))
            {
                return Task.FromResult(response);
            }

            return Task.FromResult("Please contact the team for more help with this topic.");
        }

        public virtual async Task<bool> SendChatToSupportAsync(string transcript, string customerMessage, SelectedAttachment? attachment)
        {
            await Task.CompletedTask;
            return !string.IsNullOrWhiteSpace(transcript) || !string.IsNullOrWhiteSpace(customerMessage) || attachment != null;
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
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };
        }


        public async Task<List<Loan>> GetAllLoansAsync()
        {
            return await _loanService.GetAllLoansAsync();
        }

        public async Task<Loan> GetLoanByIdAsync(int id)
        {
            return await _loanService.GetLoanByIdAsync(id);
        }

        public async Task<List<Loan>> GetLoansByUserAsync(int userId)
        {
            return await _loanService.GetLoansByUserAsync(userId);
        }

        public async Task<List<Loan>> GetLoansByStatusAsync(LoanStatus loanStatus)
        {
            return await _loanService.GetLoansByStatusAsync(loanStatus);
        }


        public async Task<List<Loan>> GetLoansByTypeAsync(LoanType loanType)
        {
            return await _loanService.GetLoansByTypeAsync(loanType);
        }


        public async Task<string?> ApplyForLoanAsync(LoanApplicationRequest request)
        {

            var newApplication = await _loanService.ApplyForLoanAsync(request);

            var (status, rejectionReason) = await _loanService.ProcessApplicationStatusAsync(newApplication);

            if (status == LoanApplicationStatus.Approved)
            {
               int loanId = await _loanService.AddLoanAsync(newApplication);
                await _loanService.GenerateAmortizationAsync(loanId);
            }

            return rejectionReason;


        }

        public LoanEstimate GetLoanEstimate(LoanApplicationRequest request)
        {
            return _loanService.GetLoanEstimate(request);
        }



        public async Task PayInstallmentAsync(int loanId, decimal? amount = null)
        {
            await _loanService.PayInstallmentAsync(loanId, amount);
        }



        public async Task<List<AmortizationRow>> GetAmortizationAsync(int loanId)
        {
            return await _loanService.GetAmortizationAsync(loanId);

        }



    }
}
