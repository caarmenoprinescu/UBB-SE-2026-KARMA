using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

public class LoansViewModel
{
    private readonly ILoanRepository _loanRepository;
    private readonly AmortizationCalculator _amortizationCalculator;

    public IEnumerable<Loan> loans { get; set; }
    public LoanEstimate currentEstimate { get; set; }
    public bool isLoading { get; set; }

    public ObservableCollection<AmortizationRow> AmortizationRows { get; set; }

    public LoansViewModel(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
        _amortizationCalculator = new AmortizationCalculator();
        AmortizationRows = new ObservableCollection<AmortizationRow>();
    }

    public void LoadAmortization(int loanId)
    {
        // 1. Load rows from repository
        var rows = _loanRepository.GetAmortization(loanId);

        // 2. If empty -> generate using AmortizationCalculator, save, and reload
        if (rows == null || rows.Count == 0)
        {
            // We need the Loan details. We fetch it from the repository.
            var loan = _loanRepository.getById(loanId);

            if (loan != null)
            {
                var generatedRows = _amortizationCalculator.generate(loan);
                _loanRepository.SaveAmortization(generatedRows);
                
                // Reload to match exact DB state and memory flags (IsCurrent)
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
}
