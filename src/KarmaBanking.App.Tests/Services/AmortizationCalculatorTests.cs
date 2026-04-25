// <copyright file="AmortizationCalculatorTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using System;
    using System.Linq;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Utils;
    using Xunit;

    public class AmortizationCalculatorTests
    {
        [Fact]
        public void ComputeEstimate_WithZeroInterest_SplitsPrincipalEvenly()
        {
            var estimate = AmortizationCalculator.ComputeEstimate(1200m, 0m, 12);

            Assert.Equal(0m, estimate.IndicativeRate);
            Assert.Equal(100m, estimate.MonthlyInstallment);
            Assert.Equal(1200m, estimate.TotalRepayable);
        }

        [Fact]
        public void ComputeEstimate_WithInterest_ComputesRoundedInstallmentAndTotal()
        {
            var estimate = AmortizationCalculator.ComputeEstimate(10000m, 12m, 12);

            Assert.Equal(12m, estimate.IndicativeRate);
            Assert.Equal(888.49m, estimate.MonthlyInstallment);
            Assert.Equal(10661.88m, estimate.TotalRepayable);
        }

        [Fact]
        public void Generate_BuildsExpectedNumberOfRowsAndEndsAtZeroBalance()
        {
            var loan = new Loan
            {
                IdentificationNumber = 7,
                Principal = 1200m,
                OutstandingBalance = 1200m,
                InterestRate = 0m,
                MonthlyInstallment = 100m,
                RemainingMonths = 12,
                TermInMonths = 12,
                StartDate = new DateTime(2026, 1, 1),
            };

            var rows = AmortizationCalculator.Generate(loan);

            Assert.Equal(12, rows.Count);
            Assert.Equal(0m, rows[^1].RemainingBalance);
        }
    }
}