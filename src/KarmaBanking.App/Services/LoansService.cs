using System.Collections.Generic;
using Windows.System;

public class LoanService : ILoanService
{
    private readonly ILoanRepository _loanRepository;

    public LoanService(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public List<Loan> GetAllLoans()
    {
        return _loanRepository.GetAllLoans();
    }

    public Loan GetLoanById(int id)
    {
        if (id <= 0)
            return new Loan();
        return _loanRepository.GetById(id);
    }

    public List<Loan> GetLoansByUser(int userId)
    {
        if (userId <= 0)
            return new List<Loan>();
        return _loanRepository.GetLoansByUser(userId);
    }

    public List<Loan> GetLoansByStatus(LoanStatus loanStatus) {
        string statusString = loanStatus.ToString();
        return _loanRepository.GetLoansByStatus(statusString);
    
    }

    public List<Loan> GetLoansByType(LoanType loanType) {
        string typeString = loanType.ToString();
        return _loanRepository.GetLoansByType(typeString);
    }

}