namespace KarmaBanking.App.Models.DTOs
{
    public class DepositRequestDto
    {
        public int AccountId { get; set; }
        public decimal Amount { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
