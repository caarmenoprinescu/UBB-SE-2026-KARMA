// <copyright file="SavingsWorkflowService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

using System.Collections.Generic;
using System.Linq;
using KarmaBanking.App.Models;
using KarmaBanking.App.Models.DTOs;

public class SavingsWorkflowService
{
    public FundingSourceOption? GetDefaultFundingSource(IEnumerable<FundingSourceOption> fundingSources)
    {
        return fundingSources.FirstOrDefault();
    }

    public int GetDefaultCloseDestinationId(IEnumerable<SavingsAccount> destinationAccounts)
    {
        return destinationAccounts.FirstOrDefault()?.IdentificationNumber ?? 0;
    }

    public (bool IsValid, string ErrorMessage) ValidateWithdrawRequest(decimal amount, FundingSourceOption? destination)
    {
        if (amount <= 0m)
        {
            return (false, "Please enter a valid amount.");
        }

        if (destination == null)
        {
            return (false, "Please select a destination account.");
        }

        return (true, string.Empty);
    }

    public string BuildWithdrawResultMessage(WithdrawResponseDto response)
    {
        if (!response.Success)
        {
            return response.Message;
        }

        var penaltyText = response.PenaltyApplied > 0 ? $" (penalty: ${response.PenaltyApplied:N2})" : string.Empty;
        return $"Withdrawn: ${response.AmountWithdrawn:N2}{penaltyText}. New balance: ${response.NewBalance:N2}";
    }

    public (bool IsValid, string ErrorMessage) ValidateCloseConfirmation(bool userConfirmed, int destinationId)
    {
        if (!userConfirmed)
        {
            return (false, "Please confirm account closure.");
        }

        if (destinationId == 0)
        {
            return (false, "Please select a destination account.");
        }

        return (true, string.Empty);
    }

    public bool CanMoveToNextPage(int currentPage, int totalPages)
    {
        return currentPage < totalPages;
    }

    public bool CanMoveToPreviousPage(int currentPage)
    {
        return currentPage > 1;
    }
}