using System;

namespace KarmaBanking.App.Models
{
    public class SavingsAccount
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string SavingsType { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal AccruedInterest { get; set; }
        public decimal Apy { get; set; }
        public DateTime? MaturityDate { get; set; }
        public string AccountStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? AccountName { get; set; }
        public int? FundingAccountId { get; set; }
        public decimal? TargetAmount { get; set; }
        public DateTime? TargetDate { get; set; }

        // Computed properties (not stored in DB)
        public decimal MonthlyInterestProjection => Balance * Apy / 12m;

        public double ProgressPercent =>
            TargetAmount.HasValue && TargetAmount.Value > 0
                ? (double)(Balance / TargetAmount.Value * 100m)
                : 0;

        public string FormattedBalance => $"${Balance:N2}";

        public bool IsGoalSavings => SavingsType == "GoalSavings";

        public string DisplayStatus =>
            SavingsType == "FixedDeposit" &&
            MaturityDate.HasValue &&
            MaturityDate.Value <= DateTime.UtcNow
                ? "Matured"
                : AccountStatus;
    }
}
