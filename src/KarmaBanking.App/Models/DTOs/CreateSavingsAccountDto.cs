using System;
using KarmaBanking.App.Models.Enums;

namespace KarmaBanking.App.Models.DTOs
{
    public class CreateSavingsAccountDto
    {
        public int UserId { get; set; }
        public string SavingsType { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal InitialDeposit { get; set; }
        public int FundingAccountId { get; set; }
        public decimal? TargetAmount { get; set; }
        public DateTime? TargetDate { get; set; }
        public DateTime? MaturityDate { get; set; }
        public DepositFrequency? DepositFrequency { get; set; }
    }
}
