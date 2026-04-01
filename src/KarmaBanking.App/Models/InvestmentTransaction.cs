using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KarmaBanking.App.Models
{
    public class InvestmentTransaction
    {
        public int Id { get; set; }
        public int HoldingId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal Fees { get; set; }
        public string OrderType { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; }
    }
}
