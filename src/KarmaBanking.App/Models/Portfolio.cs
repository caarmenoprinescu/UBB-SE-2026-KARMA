namespace KarmaBanking.App.Models
{
    using System.Collections.Generic;

    public class Portfolio
    {
        public int IdentificationNumber { get; set; }

        public int UserIdentificationNumber { get; set; }

        public decimal TotalValue { get; set; }

        public decimal TotalGainLoss { get; set; }

        public decimal GainLossPercent { get; set; }

        public List<InvestmentHolding> Holdings { get; set; } = new List<InvestmentHolding>();
    }
}