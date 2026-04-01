using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

public class LoansViewModel : INotifyPropertyChanged
{
    private readonly ILoanService _loanService;
    private readonly ILoanRepository _loanRepository;
    private readonly AmortizationCalculator _amortizationCalculator;
    private readonly PdfExporter _pdfExporter;

    public LoansViewModel(ILoanService loanService, ILoanRepository loanRepository)
    {
        _loanService = loanService;
        _loanRepository = loanRepository;
        _amortizationCalculator = new AmortizationCalculator();
        _pdfExporter = new PdfExporter();
        AmortizationRows = new ObservableCollection<AmortizationRow>();
    }

    private IEnumerable<Loan> _loans;
    public IEnumerable<Loan> loans
    {
        get => _loans;
        set { _loans = value; OnPropertyChanged(); }
    }

    private bool _isLoading;
    public bool isLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public void loadLoans()
    {
        isLoading = true;
        loans = _loanService.GetAllLoans();
        isLoading = false;
    }

    public ObservableCollection<AmortizationRow> AmortizationRows { get; set; }

    public void LoadAmortization(int loanId)
    {
        var rows = _loanRepository.GetAmortization(loanId);

        if (rows == null || rows.Count == 0)
        {
            var loan = _loanService.GetLoanById(loanId);

            if (loan != null)
            {
                var generatedRows = _amortizationCalculator.generate(loan);
                _loanRepository.SaveAmortization(generatedRows);
                rows = _loanRepository.GetAmortization(loanId);
            }
        }

        AmortizationRows.Clear();

        if (rows != null)
        {
            foreach (var row in rows)
            {
                AmortizationRows.Add(row);
            }
        }
    }

    public void downloadSchedulePdf(int loanId)
    {
        if (!AmortizationRows.Any())
            LoadAmortization(loanId);

        byte[] pdfBytes = _pdfExporter.exportAmortization(AmortizationRows.ToList());

        string desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string filePath = Path.Combine(desktopFolder, $"amortization_schedule_{loanId}.pdf");

        File.WriteAllBytes(filePath, pdfBytes);

        Process.Start(new ProcessStartInfo(filePath)
        {
            UseShellExecute = true
        });
    }

    public double GetProgress(Loan loan)
    {
        return _loanService.CalculateRepaymentProgress(loan);
    }

    public void makePayment(int loanId, decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Payment amount must be greater than zero.");
        }

        Loan loan = _loanRepository.GetById(loanId);

        if (loan == null)
        {
            throw new InvalidOperationException("Loan not found.");
        }

        if (string.Equals(loan.loanStatus, LoanStatus.Passed.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(loan.loanStatus, "Closed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(loan.loanStatus, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This loan is already closed.");
        }

        if (amount > loan.outstandingBalance)
        {
            throw new InvalidOperationException("Payment amount exceeds the outstanding balance.");
        }

        _loanRepository.pay(loanId, amount);
        loadLoans();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private string _statusMessage;
    public string statusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public void PayLoan(int loanId)
    {
        try
        {
            _loanService.PayInstallment(loanId);
            statusMessage = "Payment successful!";
            loadLoans();
        }
        catch (Exception ex)
        {
            statusMessage = ex.Message;
        }
    }

    public List<LoanType> LoanTypes =>
    Enum.GetValues(typeof(LoanType)).Cast<LoanType>().ToList();
}
