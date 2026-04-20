using CommunityToolkit.Mvvm.ComponentModel;

namespace KarmaBanking.App.ViewModels
{
    public partial class LoanViewModel : ObservableObject
    {
        private readonly Loan _loan;

        public Loan Loan => _loan;

        public double RepaymentProgress =>
           (double)AmortizationCalculator.ComputeRepaymentProgress(
                _loan.Principal,
                _loan.OutstandingBalance);

        public LoanViewModel(Loan loan)
        {
            _loan = loan;
        }

    }
}
