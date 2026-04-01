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

        _selectedLoanType = LoanType.Personal;
<<<<<<< HEAD
        _desiredAmount = 1000;

        _preferredTermMonths = AvailableTerms.First();

=======
        _preferredTermMonths = 12;
        _desiredAmount = 1000;

>>>>>>> 7253ac183dd1f623eaa2b0b8514a85a86c4b23ae
        OnPropertyChanged(nameof(selectedLoanType));
        OnPropertyChanged(nameof(preferredTermMonths));
        OnPropertyChanged(nameof(desiredAmount));
        OnPropertyChanged(nameof(AvailableTerms));

        UpdateEstimate();
    }

    public List<LoanType> LoanTypes =>
        Enum.GetValues(typeof(LoanType)).Cast<LoanType>().ToList();

    public List<int> AvailableTerms =>
        selectedLoanType switch
        {
            LoanType.Personal => new List<int> { 6, 12, 24, 36, 48 },
            LoanType.Auto => new List<int> { 12, 24, 36, 48, 60 },
            LoanType.Mortgage => new List<int> { 120, 180, 240 },
            LoanType.Student => new List<int> { 12, 24, 36 },
            _ => new List<int> { 6, 12 }
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

            preferredTermMonths = AvailableTerms.First();

            UpdateEstimate();
        }
    }

<<<<<<< HEAD
    private double _desiredAmount;
    public double desiredAmount
=======
    private decimal _desiredAmount;
    public decimal desiredAmount
>>>>>>> 7253ac183dd1f623eaa2b0b8514a85a86c4b23ae
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
            if (desiredAmount <= 0 || preferredTermMonths <= 0)
            {
                currentEstimate = null;
                OnPropertyChanged(nameof(currentEstimate));
                return;
            }

            var request = new LoanApplicationRequest
            {
                loanType = selectedLoanType,
<<<<<<< HEAD
                desiredAmount = (decimal)desiredAmount,
=======
                desiredAmount = desiredAmount,
>>>>>>> 7253ac183dd1f623eaa2b0b8514a85a86c4b23ae
                preferredTermMonths = preferredTermMonths,
                purpose = purpose
            };

            currentEstimate = _loanService.GetLoanEstimate(request);
            OnPropertyChanged(nameof(currentEstimate));
        }
        catch (Exception ex)
        {
            statusMessage = ex.Message;
        }
    }

    public void Submit()
    {
        try
        {
            if (desiredAmount <= 0)
            {
                statusMessage = "Invalid amount";
                return;
            }

            var request = new LoanApplicationRequest
            {
                loanType = selectedLoanType,
<<<<<<< HEAD
                desiredAmount = (decimal)desiredAmount,
=======
                desiredAmount = desiredAmount,
>>>>>>> 7253ac183dd1f623eaa2b0b8514a85a86c4b23ae
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