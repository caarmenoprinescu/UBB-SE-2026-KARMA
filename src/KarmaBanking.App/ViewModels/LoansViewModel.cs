using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

public class LoansViewModel
{
    private readonly ILoanService _loanService;
    private readonly ILoanRepository _loanRepository;
    private readonly AmortizationCalculator _amortizationCalculator;

    public IEnumerable<Loan> loans { get; set; }
    public LoanEstimate currentEstimate { get; set; }
    public bool isLoading { get; set; }

    public ObservableCollection<AmortizationRow> AmortizationRows { get; set; }

    public LoansViewModel(ILoanService loanService, ILoanRepository loanRepository)
    {
        _loanService = loanService;
        _loanRepository = loanRepository;
        _amortizationCalculator = new AmortizationCalculator();
        AmortizationRows = new ObservableCollection<AmortizationRow>();
    }

    public void LoadAmortization(int loanId)
    {
        // 1. Load rows from repository (OK să rămână aici)
        var rows = _loanRepository.GetAmortization(loanId);

        // 2. Dacă nu există → generăm și salvăm
        if (rows == null || rows.Count == 0)
        {
            // folosim Service pentru Loan
            var loan = _loanService.GetLoanById(loanId);

            if (loan != null)
            {
                var generatedRows = _amortizationCalculator.generate(loan);
                _loanRepository.SaveAmortization(generatedRows);

                // reload din DB
                rows = _loanRepository.GetAmortization(loanId);
            }
        }

        // 3. Populate ObservableCollection
        AmortizationRows.Clear();

        if (rows != null)
        {
            foreach (var row in rows)
            {
                AmortizationRows.Add(row);
            }
        }
    }

    /// <summary>
    /// Stub — real PDF generation will be implemented in BA-47.
    /// </summary>
    public void downloadSchedulePdf(int loanId)
    {
        // TODO: BA-47 will implement actual PDF generation logic.
    }
}