// <copyright file="PaymentCalculationService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

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
        var paymentAmount = isStandardPayment ? monthlyInstallment : customPaymentAmount;
        var balanceAfterPayment = Math.Max(0m, outstandingBalance - paymentAmount);

        var monthsPaid = isStandardPayment
            ? 1
            : paymentAmount <= 0m
                ? 0
                : (int)Math.Floor(paymentAmount / monthlyInstallment);

        var newRemainingMonths = Math.Max(0, remainingMonths - monthsPaid);
        return (balanceAfterPayment, newRemainingMonths);
    }

    public (bool Success, decimal Amount) ParsePaymentAmount(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return (false, 0m);
        }

        if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out var currentCultureResult))
        {
            return (true, currentCultureResult);
        }

        if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantCultureResult))
        {
            return (true, invariantCultureResult);
        }

        return (false, 0m);
    }

    public (bool IsValid, string ValidationMessage) ValidatePaymentAmount(
        decimal paymentAmount,
        decimal outstandingBalance)
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

    public decimal GetInitialCustomAmount(
        decimal monthlyInstallment,
        decimal outstandingBalance,
        double? currentCustomAmount)
    {
        var amount = currentCustomAmount.HasValue ? (decimal)currentCustomAmount.Value : monthlyInstallment;
        return amount > outstandingBalance ? outstandingBalance : amount;
    }

    public string FormatCustomAmount(decimal amount)
    {
        return amount.ToString("0.##", CultureInfo.CurrentCulture);
    }
}