// <copyright file="SavingsPresentationService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using KarmaBanking.App.Models;

public class SavingsPresentationService
{
    public string BuildTotalSavedAmount(IEnumerable<SavingsAccount> accounts)
    {
        return $"${accounts.Sum(account => account.Balance):F2}";
    }

    public string BuildNumberOfAccountsText(int accountCount)
    {
        return $"across {accountCount} account{(accountCount == 1 ? string.Empty : "s")}";
    }

    public string BuildBestInterestRate(IEnumerable<SavingsAccount> accounts)
    {
        var bestApy = accounts.Any() ? accounts.Max(account => account.Apy) : 0m;
        return $"{bestApy * 100:F2}%";
    }

    public bool HasClosePenaltyRisk(SavingsAccount? selectedAccount)
    {
        return selectedAccount?.SavingsType == "FixedDeposit" &&
               selectedAccount.MaturityDate.HasValue &&
               selectedAccount.MaturityDate.Value > DateTime.UtcNow;
    }
}