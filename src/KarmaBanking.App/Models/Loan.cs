using System;

public class Loan
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public LoanType LoanType { get; set; }
    public decimal Principal { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal InterestRate { get; set; }
    public decimal MonthlyInstallment { get; set; }
    public int RemainingMonths { get; set; }
    public LoanStatus LoanStatus { get; set; }
    public int TermInMonths { get; set; }
    public DateTime StartDate { get; set; }

}