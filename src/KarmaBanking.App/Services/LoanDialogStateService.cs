// <copyright file="LoanDialogStateService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

public class LoanDialogStateService
{
    public bool ShouldComputeEstimate(double desiredAmount, int preferredTermMonths, string purpose)
    {
        return desiredAmount > 0 &&
               preferredTermMonths > 0 &&
               !string.IsNullOrWhiteSpace(purpose);
    }
}