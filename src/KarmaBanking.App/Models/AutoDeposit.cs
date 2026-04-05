using System;
using KarmaBanking.App.Models.Enums;

namespace KarmaBanking.App.Models
{
    public class AutoDeposit
    {
        public int Id { get; set; }
        public int SavingsAccountId { get; set; }
        public decimal Amount { get; set; }
        public DepositFrequency Frequency { get; set; }
        public DateTime NextRunDate { get; set; }
        public bool IsActive { get; set; }
    }
}
