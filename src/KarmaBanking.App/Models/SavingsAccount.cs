using System;

namespace KarmaBanking.App.Models
{
    public class SavingsAccount
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int FundingAccountId { get; set; }
        public string AccountName { get; set; }
        public string SavingsType { get; set; }
        public decimal Balance { get; set; }
        public decimal AccruedInterest { get; set; }
        public decimal InterestRate { get; set; }
        public decimal? TargetAmount { get; set; }
        public DateTime? TargetDate { get; set; }
        public DateTime? MaturityDate { get; set; }
        public string? DepositFrequency { get; set; }
        public decimal? AutoDepositAmount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}