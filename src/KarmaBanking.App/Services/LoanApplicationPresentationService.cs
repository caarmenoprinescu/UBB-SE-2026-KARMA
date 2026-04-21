// <copyright file="LoanApplicationPresentationService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

public class LoanApplicationPresentationService
{
    public (bool Approved, string Message) BuildApplicationOutcome(string? rejectionReason)
    {
        return rejectionReason == null
            ? (true, "Your loan application has been approved!")
            : (false, $"Application rejected: {rejectionReason}");
    }
}