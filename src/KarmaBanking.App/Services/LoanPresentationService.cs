using KarmaBanking.App.Utils;

namespace KarmaBanking.App.Services
{
    public class LoanPresentationService
    {
        public double GetRepaymentProgress(Loan loan)
        {
            return (double)AmortizationCalculator.ComputeRepaymentProgress(
                loan.Principal,
                loan.OutstandingBalance);
        }
    }
}
