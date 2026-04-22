// <copyright file="LoanApplicationValidator.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

using System;

/// <summary>
/// Provides validation rules for incoming loan application requests.
/// </summary>
public class LoanApplicationValidator
{
    /// <summary>
    /// Validates a loan application request and throws when invalid.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    public void Validate(LoanApplicationRequest request)
    {
        if (request == null)
        {
            throw new Exception("Request cannot be null");
        }

        if (request.DesiredAmount <= 0)
        {
            throw new Exception("Desired amount must be greater than 0");
        }

        if (!Enum.IsDefined(typeof(LoanType), request.LoanType))
        {
            throw new Exception("Invalid Loan Type");
        }

        if (request.PreferredTermMonths <= 0)
        {
            throw new Exception("Term must be greater than 0");
        }

        if (string.IsNullOrWhiteSpace(request.Purpose))
        {
            throw new Exception("Purpose is required");
        }
    }
}