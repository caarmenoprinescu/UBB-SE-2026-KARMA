namespace KarmaBanking.App.Repositories.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KarmaBanking.App.Models;

/// <summary>
/// Defines persistence operations for investment portfolios and trades.
/// </summary>
public interface IInvestmentRepository
{
    /// <summary>
    /// Gets a user's portfolio snapshot including holdings.
    /// </summary>
    /// <param name="userIdentificationNumber">The owning user identifier.</param>
    /// <returns>The portfolio data.</returns>
    Portfolio GetPortfolio(int userIdentificationNumber);

    /// <summary>
    /// Records a crypto buy or sell trade and updates holdings using final calculated values.
    /// </summary>
    /// <param name="portfolioIdentificationNumber">The portfolio identifier.</param>
    /// <param name="ticker">The traded ticker symbol.</param>
    /// <param name="actionType">The action type (buy or sell).</param>
    /// <param name="quantity">The traded quantity.</param>
    /// <param name="pricePerUnit">The execution price per unit.</param>
    /// <param name="fees">The applied trade fees.</param>
    /// <param name="finalQuantity">The post-trade total quantity calculated by the service.</param>
    /// <param name="finalAveragePrice">The post-trade average price calculated by the service.</param>
    /// <returns>A task that completes when the trade is persisted.</returns>
    Task RecordCryptoTradeAsync(
        int portfolioIdentificationNumber,
        string ticker,
        string actionType,
        decimal quantity,
        decimal pricePerUnit,
        decimal fees,
        decimal finalQuantity,
        decimal finalAveragePrice);

    /// <summary>
    /// Gets paginated investment transaction logs with optional filters.
    /// </summary>
    /// <param name="portfolioIdentificationNumber">The portfolio identifier.</param>
    /// <param name="startDate">The optional start date filter.</param>
    /// <param name="endDate">The optional end date filter.</param>
    /// <param name="ticker">The optional ticker filter.</param>
    /// <returns>A filtered list of investment transactions.</returns>
    Task<List<InvestmentTransaction>> GetInvestmentLogsAsync(
        int portfolioIdentificationNumber,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? ticker = null);
}