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

    private LoanEstimate _currentEstimate;
    public LoanEstimate currentEstimate
    {
        get => _currentEstimate;
        set { _currentEstimate = value; OnPropertyChanged(); }
    }
    
    private LoanType _selectedLoanType;
    public LoanType selectedLoanType
    {
        get => _selectedLoanType;
        set
        {
            _selectedLoanType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AvailableTerms));
            UpdateEstimate();
        }
    }

    private decimal _desiredAmount;
    public decimal desiredAmount
    {
        get => _desiredAmount;
        set
        {
            _desiredAmount = value;
            OnPropertyChanged();
            UpdateEstimate();
        }
    }

    private int _preferredTermMonths;
    public int preferredTermMonths
    {
        get => _preferredTermMonths;
        set
        {
            _preferredTermMonths = value;
            OnPropertyChanged();
            UpdateEstimate();
        }
    }

    private string _purpose;
    public string purpose
    {
        get => _purpose;
        set
        {
            _purpose = value;
            OnPropertyChanged();
        }
    }

    private string _statusMessage;
    public string statusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public List<int> AvailableTerms =>
    selectedLoanType switch
    {
        LoanType.Personal => new List<int> { 12, 24, 36, 48, 60 },
        LoanType.Auto => new List<int> { 12, 24, 36, 48, 60, 72 },
        LoanType.Mortgage => new List<int> { 120, 180, 240, 300, 360 },
        LoanType.Student => new List<int> { 12, 24, 36, 48 },
        _ => new List<int> { 12, 24 }
    };
    public void ApplyForLoan()
    {
        try
        {
            var request = new LoanApplicationRequest
            {
                loanType = selectedLoanType,
                desiredAmount = desiredAmount,
                preferredTermMonths = preferredTermMonths,
                purpose = purpose
            };

            _loanService.ApplyForLoan(request);
            statusMessage = "Application submitted successfully!";
        }
        catch (Exception ex)
        {
            statusMessage = ex.Message;
        }
    }

    public void UpdateEstimate()
    {
        try
        {
            var request = new LoanApplicationRequest
            {
                loanType = selectedLoanType,
                desiredAmount = desiredAmount,
                preferredTermMonths = preferredTermMonths,
                purpose = purpose
            };

            currentEstimate = _loanService.GetLoanEstimate(request);
        }
        catch
        {
            currentEstimate = null;
        }
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public List<LoanType> LoanTypes =>
    Enum.GetValues(typeof(LoanType)).Cast<LoanType>().ToList();
}