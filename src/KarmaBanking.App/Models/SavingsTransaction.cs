using System;
using KarmaBanking.App.Models.Enums;

namespace KarmaBanking.App.Models
{
    public class SavingsTransaction
    {
        public int Id { get; set; }
        public int SavingsAccountId { get; set; }
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public string? Source { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int AccountId { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? Description { get; set; }
    }
}
