using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class LoansViewModel
{
    private readonly ILoanService _loanService;
    private readonly ILoanRepository _loanRepository;
    private readonly AmortizationCalculator _amortizationCalculator;
    private readonly PdfExporter _pdfExporter;

    public IEnumerable<Loan> loans { get; set; }

    public void loadLoans()
    {
        isLoading = true;
        loans = _loanService.GetAllLoans();
        isLoading = false;
    }
    public LoanEstimate currentEstimate { get; set; }
    public bool isLoading { get; set; }

    public ObservableCollection<AmortizationRow> AmortizationRows { get; set; }

    public LoansViewModel(ILoanService loanService, ILoanRepository loanRepository)
    {
        _loanService = loanService;
        _loanRepository = loanRepository;
        _amortizationCalculator = new AmortizationCalculator();
        _pdfExporter = new PdfExporter();
        AmortizationRows = new ObservableCollection<AmortizationRow>();
    }

    public void LoadAmortization(int loanId)
    {
<<<<<<< HEAD

        var rows = _loanRepository.GetAmortization(loanId);


        if (rows == null || rows.Count == 0)
        {

=======
        var rows = _loanRepository.GetAmortization(loanId);

        if (rows == null || rows.Count == 0)
        {
>>>>>>> 1012272d27e537cb088af2a64c10926dbdcdbca2
            var loan = _loanService.GetLoanById(loanId);

            if (loan != null)
            {
                var generatedRows = _amortizationCalculator.generate(loan);
                _loanRepository.SaveAmortization(generatedRows);
<<<<<<< HEAD


=======
>>>>>>> 1012272d27e537cb088af2a64c10926dbdcdbca2
                rows = _loanRepository.GetAmortization(loanId);
            }
        }

<<<<<<< HEAD

=======
>>>>>>> 1012272d27e537cb088af2a64c10926dbdcdbca2
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
        {
            LoadAmortization(loanId);
        }

        byte[] pdfBytes = _pdfExporter.exportAmortization(AmortizationRows.ToList());

        string desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string filePath = Path.Combine(desktopFolder, $"amortization_schedule_{loanId}.pdf");
        File.WriteAllBytes(filePath, pdfBytes);

        Process.Start(new ProcessStartInfo(filePath)
        {
            UseShellExecute = true
        });
    }
}
