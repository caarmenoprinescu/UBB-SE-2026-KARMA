using System;

public class Loan
{
    public int id { get; set; }
    public int userId { get; set; }
    public string loanType { get; set; }
    public decimal principal { get; set; }
    public decimal outstandingBalance { get; set; }
    public decimal interestRate { get; set; }
    public decimal monthlyInstallment { get; set; }
    public int remainingMonths { get; set; }
    public string loanStatus { get; set; }
    public int TermInMonths { get; set; }
    public DateTime StartDate { get; set; }
}