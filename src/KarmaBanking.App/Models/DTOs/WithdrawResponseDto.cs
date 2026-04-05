using System;

namespace KarmaBanking.App.Models.DTOs
{
    public class WithdrawResponseDto
    {
        public bool Success { get; set; }
        public decimal AmountWithdrawn { get; set; }
        public decimal PenaltyApplied { get; set; }
        public decimal NewBalance { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
    }
}
