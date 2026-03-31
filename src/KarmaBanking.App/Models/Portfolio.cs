using System.Collections.Generic;

namespace KarmaBanking.App.Models
{
    public class Portfolio
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal TotalValue { get; set; }
        public decimal TotalGainLoss { get; set; }
        public decimal GainLossPercent { get; set; }
        public List<InvestmentHolding> Holdings { get; set; } = new List<InvestmentHolding>();
    }
}
