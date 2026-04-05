public class LoanApplication
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public LoanType LoanType { get; set; }

    public decimal DesiredAmount { get; set; }

    public int PreferredTermMonths { get; set; }

    public string Purpose { get; set; }

    public LoanApplicationStatus ApplicationStatus { get; set; }

    public string? RejectionReason { get; set; }

}