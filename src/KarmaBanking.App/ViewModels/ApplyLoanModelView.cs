using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

public class ApplyLoanViewModel : INotifyPropertyChanged
{
    private readonly ILoanService _loanService;

    public ApplyLoanViewModel(ILoanService loanService)
    {
        _loanService = loanService;
    }

    public List<LoanType> LoanTypes =>
        Enum.GetValues(typeof(LoanType)).Cast<LoanType>().ToList();

    public List<int> AvailableTerms =>
        selectedLoanType switch
        {
            LoanType.Personal => new List<int> { 12, 24, 36, 48, 60 },
            LoanType.Auto => new List<int> { 12, 24, 36, 48, 60, 72 },
            LoanType.Mortgage => new List<int> { 120, 180, 240, 300, 360 },
            LoanType.Student => new List<int> { 12, 24, 36, 48 },
            _ => new List<int> { 12, 24 }
        };

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

    private string _desiredAmount;
    public string desiredAmount
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

    private LoanEstimate _currentEstimate;
    public LoanEstimate currentEstimate
    {
        get => _currentEstimate;
        set
        {
            _currentEstimate = value;
            OnPropertyChanged();
        }
    }

    private string _statusMessage;
    public string statusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public void UpdateEstimate()
    {
        try
        {
            if (!decimal.TryParse(desiredAmount, out decimal amount))
            {
                currentEstimate = null;
                OnPropertyChanged(nameof(currentEstimate));
                return;
            }

            var request = new LoanApplicationRequest
            {
                loanType = selectedLoanType,
                desiredAmount = amount,
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

    public void Submit()
    {
        try
        {
            if (!decimal.TryParse(desiredAmount, out decimal amount))
            {
                statusMessage = "Invalid amount";
                return;
            }

            var request = new LoanApplicationRequest
            {
                loanType = selectedLoanType,
                desiredAmount = amount,
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

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}