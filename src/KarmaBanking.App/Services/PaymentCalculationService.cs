namespace KarmaBanking.App.Services
{
    using System;
    using System.Globalization;

    public class PaymentCalculationService
    {
        public (decimal BalanceAfterPayment, int RemainingMonths) CalculatePaymentPreview(
            decimal monthlyInstallment,
            decimal outstandingBalance,
            int remainingMonths,
            bool isStandardPayment,
            decimal customPaymentAmount = 0)
        {
            decimal paymentAmount = isStandardPayment ? monthlyInstallment : customPaymentAmount;
            decimal balanceAfterPayment = Math.Max(0m, outstandingBalance - paymentAmount);

            int monthsPaid = isStandardPayment
                ? 1
                : (paymentAmount <= 0m ? 0 : (int)Math.Floor(paymentAmount / monthlyInstallment));

            int newRemainingMonths = Math.Max(0, remainingMonths - monthsPaid);
            return (balanceAfterPayment, newRemainingMonths);
        }

        public (bool Success, decimal Amount) ParsePaymentAmount(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return (false, 0m);
            }

            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal currentCultureResult))
            {
                return (true, currentCultureResult);
            }

            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal invariantCultureResult))
            {
                return (true, invariantCultureResult);
            }

            return (false, 0m);
        }

        public (bool IsValid, string ValidationMessage) ValidatePaymentAmount(decimal paymentAmount, decimal outstandingBalance)
        {
            if (paymentAmount <= 0)
            {
                return (false, "Payment amount must be greater than 0.");
            }

            if (paymentAmount > outstandingBalance)
            {
                return (false, $"Payment amount cannot exceed outstanding balance of {outstandingBalance:C2}.");
            }

            return (true, string.Empty);
        }
    }
}