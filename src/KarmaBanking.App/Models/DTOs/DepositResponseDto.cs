using System;

namespace KarmaBanking.App.Models.DTOs
{
    public class DepositResponseDto
    {
        public decimal NewBalance { get; set; }
        public int TransactionId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
