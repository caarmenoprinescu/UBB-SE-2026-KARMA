// <copyright file="LoansViewModel.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KarmaBanking.App.Services;

public partial class LoansViewModel : ObservableObject
{
    private readonly ApiService apiService;
    private readonly AmortizationCalculator calculator;
    private readonly LoanApplicationPresentationService loanApplicationPresentationService;
    private readonly LoanDialogStateService loanDialogStateService;

    private readonly PaymentCalculationService paymentCalculationService = new();
    private readonly PdfExporter pdfExporter;

    [ObservableProperty]
    private ObservableCollection<AmortizationRow> amortizationRows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ApplicationWasApproved))]
    private string applicationResult = string.Empty;

    [ObservableProperty]
    private bool applicationWasApproved;

    [ObservableProperty]
    private LoanEstimate currentEstimate;
    [ObservableProperty]
    private double? customAmount;
    [ObservableProperty]
    private double desiredAmount;
    [ObservableProperty]
    private string dialogActionText = "Continue";

    // --- Proprietăți noi pentru controlul UI-ului din Dialog ---
    [ObservableProperty]
    private string dialogTitle = "Apply for Loan";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isEstimateVisible;
    [ObservableProperty]
    private bool isFormVisible = true;
    [ObservableProperty]
    private bool isLoading;
    [ObservableProperty]
    private bool isReviewVisible;

    // --- Aici e fix-ul pentru lista care nu apărea la deschidere ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredLoans))]
    private ObservableCollection<LoanViewModel> loans = [];

    // --- Payment preview properties ---
    [ObservableProperty]
    private decimal paymentPreviewBalance;
    [ObservableProperty]
    private int paymentPreviewRemainingMonths;
    [ObservableProperty]
    private int preferredTermMonths;
    [ObservableProperty]
    private string purpose = string.Empty;
    [ObservableProperty]
    private LoanViewModel selectedLoan;

    [ObservableProperty]
    private LoanType selectedLoanType;

    [NotifyPropertyChangedFor(nameof(FilteredLoans))]
    [ObservableProperty]
    private LoanStatus? statusFilter;

    [NotifyPropertyChangedFor(nameof(FilteredLoans))]
    [ObservableProperty]
    private LoanType? typeFilter;

    public LoansViewModel()
    {
        this.apiService = new ApiService(new LoanService(new LoanRepository()));
        this.calculator = new AmortizationCalculator();
        this.pdfExporter = new PdfExporter();
        this.loanDialogStateService = new LoanDialogStateService();
        this.loanApplicationPresentationService = new LoanApplicationPresentationService();
        _ = this.LoadLoansAsync();
    }

    public IEnumerable<LoanType> LoanTypes => Enum.GetValues<LoanType>();

    public bool HasError => !string.IsNullOrEmpty(this.ErrorMessage);

    public IEnumerable<LoanViewModel> FilteredLoans =>
        this.loans.Where(l =>
            (this.statusFilter == null || l.Loan.LoanStatus == this.statusFilter) &&
            (this.typeFilter == null || l.Loan.LoanType == this.typeFilter));

    [RelayCommand]
    public async Task LoadLoansAsync()
    {
        this.IsLoading = true;
        this.ErrorMessage = string.Empty;
        try
        {
            var result = await this.apiService.GetLoansByUserAsync(CurrentUser.Id);
            this.Loans = new ObservableCollection<LoanViewModel>(result.Select(l => new LoanViewModel(l)));
        }
        catch (Exception ex)
        {
            this.ErrorMessage = ex.Message;
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ApplyForLoanAsync()
    {
        this.IsLoading = true;
        this.ErrorMessage = string.Empty;
        try
        {
            var request = new LoanApplicationRequest
            {
                UserId = CurrentUser.Id,
                LoanType = this.SelectedLoanType,
                DesiredAmount = (decimal)this.DesiredAmount,
                PreferredTermMonths = this.PreferredTermMonths,
                Purpose = this.Purpose,
            };

            var rejectionReason = await this.apiService.ApplyForLoanAsync(request);

            var applicationOutcome = this.loanApplicationPresentationService.BuildApplicationOutcome(rejectionReason);
            this.ApplicationResult = applicationOutcome.Message;
            this.ApplicationWasApproved = applicationOutcome.Approved;
            if (applicationOutcome.Approved)
            {
                await this.LoadLoansAsync();
                this.OnPropertyChanged(nameof(this.FilteredLoans));
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = ex.Message;
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    [RelayCommand]
    public void ComputeLiveEstimate()
    {
        this.ErrorMessage = string.Empty;
        try
        {
            var request = new LoanApplicationRequest
            {
                UserId = CurrentUser.Id,
                LoanType = this.SelectedLoanType,
                DesiredAmount = (decimal)this.DesiredAmount,
                PreferredTermMonths = this.PreferredTermMonths,
                Purpose = this.Purpose,
            };
            this.CurrentEstimate = this.apiService.GetLoanEstimate(request);
        }
        catch (Exception e)
        {
            this.ErrorMessage = e.Message;
        }
    }

    public async Task PayInstallmentAsync()
    {
        this.IsLoading = true;
        this.ErrorMessage = string.Empty;
        try
        {
            var amount = this.CustomAmount.HasValue
                ? (decimal?)this.CustomAmount.Value
                : null;
            await this.apiService.PayInstallmentAsync(this.SelectedLoan.Loan.Id, amount);
            await this.LoadLoansAsync();

            this.OnPropertyChanged(nameof(this.FilteredLoans));
        }
        catch (Exception e)
        {
            this.ErrorMessage = e.Message;
            throw;
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    /// <summary>
    ///     Updates the payment preview (balance and remaining term) based on selected payment method.
    /// </summary>
    public void UpdatePaymentPreview(bool isStandardPayment, string customAmountText = "")
    {
        if (this.SelectedLoan == null)
        {
            this.PaymentPreviewBalance = 0m;
            this.PaymentPreviewRemainingMonths = 0;
            return;
        }

        var loan = this.SelectedLoan.Loan;
        var customAmount = 0m;

        if (!isStandardPayment && !string.IsNullOrWhiteSpace(customAmountText))
        {
            var (success, amount) = this.paymentCalculationService.ParsePaymentAmount(customAmountText);
            if (!success)
            {
                customAmount = 0m;
            }
            else
            {
                customAmount = amount;
            }
        }

        var (balance, months) = this.paymentCalculationService.CalculatePaymentPreview(
            loan.MonthlyInstallment,
            loan.OutstandingBalance,
            loan.RemainingMonths,
            isStandardPayment,
            customAmount);

        this.PaymentPreviewBalance = balance;
        this.PaymentPreviewRemainingMonths = months;
    }

    public void SelectStandardPayment()
    {
        this.CustomAmount = null;
        this.UpdatePaymentPreview(true);
    }

    public string SelectCustomPayment()
    {
        if (this.SelectedLoan == null)
        {
            this.CustomAmount = null;
            this.UpdatePaymentPreview(false, string.Empty);
            return string.Empty;
        }

        if (!this.CustomAmount.HasValue)
        {
            var initialCustomAmount = this.paymentCalculationService.GetInitialCustomAmount(
                this.SelectedLoan.Loan.MonthlyInstallment,
                this.SelectedLoan.Loan.OutstandingBalance,
                this.CustomAmount);
            this.CustomAmount = (double)initialCustomAmount;
        }
        else
        {
            var normalizedCustomAmount = this.paymentCalculationService.GetInitialCustomAmount(
                this.SelectedLoan.Loan.MonthlyInstallment,
                this.SelectedLoan.Loan.OutstandingBalance,
                this.CustomAmount);
            this.CustomAmount = (double)normalizedCustomAmount;
        }

        var currentText = this.paymentCalculationService.FormatCustomAmount((decimal)(this.CustomAmount ?? 0d));
        this.UpdatePaymentPreview(false, currentText);
        return currentText;
    }

    public void UpdateCustomPayment(string customAmountText)
    {
        var (success, amount) = this.paymentCalculationService.ParsePaymentAmount(customAmountText);
        this.CustomAmount = success ? (double)amount : null;
        this.UpdatePaymentPreview(false, customAmountText);
    }

    public async Task LoadAmortizationAsync()
    {
        if (this.SelectedLoan == null)
        {
            return;
        }

        this.IsLoading = true;
        this.ErrorMessage = string.Empty;
        try
        {
            var rows = await this.apiService.GetAmortizationAsync(this.SelectedLoan.Loan.Id);
            this.AmortizationRows = new ObservableCollection<AmortizationRow>(rows);
        }
        catch (Exception e)
        {
            this.ErrorMessage = e.Message;
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DownloadSchedulePdfAsync()
    {
        try
        {
            var rows = await this.apiService.GetAmortizationAsync(this.SelectedLoan.Loan.Id);
            var pdfBytes = this.pdfExporter.exportAmortization(rows);
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var fileName = $"amortization_schedule_{this.SelectedLoan.Loan.Id}.pdf";
            var filePath = Path.Combine(desktopPath, fileName);

            await File.WriteAllBytesAsync(filePath, pdfBytes);

            Process.Start(
                new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                });
        }
        catch (Exception e)
        {
            this.ErrorMessage = e.Message;
        }
    }

    // --- Metode de control pentru Dialog ---
    public void SwitchToReviewStage()
    {
        this.IsFormVisible = false;
        this.IsReviewVisible = true;
        this.DialogTitle = "Application Review";
        this.DialogActionText = "Submit";
    }

    public void ResetDialogState()
    {
        this.IsFormVisible = true;
        this.IsReviewVisible = false;
        this.DialogTitle = "Apply for Loan";
        this.DialogActionText = "Continue";
        this.ApplicationResult = string.Empty;
        this.ApplicationWasApproved = false;

        this.DesiredAmount = 0;
        this.PreferredTermMonths = 0;
        this.Purpose = string.Empty;
        this.CurrentEstimate = null;
        this.IsEstimateVisible = false;
    }

    partial void OnDesiredAmountChanged(double value)
    {
        this.TryComputeEstimate();
    }

    partial void OnPreferredTermMonthsChanged(int value)
    {
        this.TryComputeEstimate();
    }

    partial void OnSelectedLoanTypeChanged(LoanType value)
    {
        this.TryComputeEstimate();
    }

    partial void OnPurposeChanged(string value)
    {
        this.TryComputeEstimate();
    }

    private void TryComputeEstimate()
    {
        var isFullyFilled = this.loanDialogStateService.ShouldComputeEstimate(
            this.DesiredAmount,
            this.PreferredTermMonths,
            this.Purpose);

        if (isFullyFilled)
        {
            this.ComputeLiveEstimate();
            this.IsEstimateVisible = true;
        }
        else
        {
            this.CurrentEstimate = null;
            this.IsEstimateVisible = false;
        }
    }
}