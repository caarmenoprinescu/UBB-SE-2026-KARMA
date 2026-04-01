public class LoanApplication
{
    public int id { get; set; }

    public LoanType loanType { get; set; }

    public decimal desiredAmount { get; set; }

    public int preferredTermMonths { get; set; }

    public string purpose { get; set; }

    public LoanApplicationStatus applicationStatus { get; set; }

    public string rejectionReason { get; set; }
}