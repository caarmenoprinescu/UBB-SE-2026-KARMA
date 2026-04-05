using System;

namespace KarmaBanking.App.Models.DTOs
{
    public class ClosureResult
    {
        public bool Success { get; set; }
        public decimal TransferredAmount { get; set; }
        public decimal PenaltyApplied { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime ClosedAt { get; set; }
    }
}