using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KarmaBanking.App.Repositories
{
    internal class InvestmentRepository : IInvestmentRepository
    {
        public async Task RecordCryptoTradeAsync(int portfolioId, string ticker, string actionType, decimal quantity, decimal pricePerUnit, decimal fees) 
        {
            using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();
            await connection.OpenAsync();

            using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                int? holdingId = null;
                decimal currentQuantity = 0;
                decimal currentAvgPrice = 0;

                string checkHoldingQuery = "SELECT id, quantity, avgPurchasePrice FROM InvestmentHolding WHERE portfolioId = @PortfolioId AND ticker = @Ticker";
                using (SqlCommand checkCmd = new SqlCommand(checkHoldingQuery, connection, transaction))
                {
                    checkCmd.Parameters.AddWithValue("@PortfolioId", portfolioId);
                    checkCmd.Parameters.AddWithValue("@Ticker", ticker);

                    using SqlDataReader reader = await checkCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        holdingId = reader.GetInt32(0);
                        currentQuantity = reader.GetDecimal(1);
                        currentAvgPrice = reader.GetDecimal(2);
                    }
                }

                if (actionType.Equals("BUY", StringComparison.OrdinalIgnoreCase))
                {
                    if (holdingId.HasValue)
                    {
                        decimal totalCost = (currentQuantity * currentAvgPrice) + (quantity * pricePerUnit);
                        decimal newQuantity = currentQuantity + quantity;
                        decimal newAvgPrice = totalCost / newQuantity;

                        string updateHoldingQuery = "UPDATE InvestmentHolding SET quantity = @NewQuantity, avgPurchasePrice = @NewAvgPrice WHERE id = @HoldingId";
                        using SqlCommand updateCmd = new SqlCommand(updateHoldingQuery, connection, transaction);
                        updateCmd.Parameters.AddWithValue("@NewQuantity", newQuantity);
                        updateCmd.Parameters.AddWithValue("@NewAvgPrice", newAvgPrice);
                        updateCmd.Parameters.AddWithValue("@HoldingId", holdingId.Value);

                        await updateCmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        string insertHoldingQuery = @"
                            INSERT INTO InvestmentHolding (portfolioId, ticker, assetType, quantity, avgPurchasePrice, currentPrice, unrealizedGainLoss)
                            OUTPUT INSERTED.id
                            VALUES (@PortfolioId, @Ticker, 'Crypto', @Quantity, @AvgPrice, @AvgPrice, 0)";
                        using SqlCommand insertCmd = new SqlCommand(insertHoldingQuery, connection, transaction);
                        insertCmd.Parameters.AddWithValue("@PortfolioId", portfolioId);
                        insertCmd.Parameters.AddWithValue("@Ticker", ticker);
                        insertCmd.Parameters.AddWithValue("@Quantity", quantity);
                        insertCmd.Parameters.AddWithValue("@AvgPrice", pricePerUnit);

                        holdingId = (int)await insertCmd.ExecuteScalarAsync();
                    }
                }
                else if (actionType.Equals("SELL", StringComparison.OrdinalIgnoreCase))
                {
                    if (!holdingId.HasValue || currentQuantity < quantity)
                    {
                        throw new InvalidOperationException("Insufficient wallet balance to execute this sell order.");
                    }

                    decimal newQuantity = currentQuantity - quantity;
                    string updateHoldingQuery = "UPDATE InvestmentHolding SET quantity = @NewQuantity WHERE id = @HoldingId";
                    using SqlCommand updateCmd = new SqlCommand(updateHoldingQuery, connection, transaction);
                    updateCmd.Parameters.AddWithValue("@NewQuantity", newQuantity);
                    updateCmd.Parameters.AddWithValue("@HoldingId", holdingId.Value);

                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    throw new ArgumentException("ActionType must be either 'BUY' or 'SELL'.");
                }

                string insertTxQuery = @"
                    INSERT INTO InvestmentTransaction (holdingId, ticker, actionType, quantity, pricePerUnit, fees, orderType, executedAt)
                    VALUES (@HoldingId, @Ticker, @ActionType, @Quantity, @PricePerUnit, @Fees, 'Market', @ExecutedAt)";
                using (SqlCommand txCmd = new SqlCommand(insertTxQuery, connection, transaction))
                {
                    txCmd.Parameters.AddWithValue("@HoldingId", holdingId.Value);
                    txCmd.Parameters.AddWithValue("@Ticker", ticker);
                    txCmd.Parameters.AddWithValue("@ActionType", actionType.ToUpper());
                    txCmd.Parameters.AddWithValue("@Quantity", quantity);
                    txCmd.Parameters.AddWithValue("@PricePerUnit", pricePerUnit);
                    txCmd.Parameters.AddWithValue("@Fees", fees);
                    txCmd.Parameters.AddWithValue("@ExecutedAt", DateTime.Now);

                    await txCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public Portfolio GetPortfolio(int userId)
        {
            const string selectPortfolioQuery = @"
                SELECT id, userId, totalValue, totalGainLoss, gainLossPercent
                FROM Portfolio
                WHERE userId = @UserId";

            const string selectHoldingsQuery = @"
                SELECT id, ticker, assetType, quantity, avgPurchasePrice, currentPrice, unrealizedGainLoss
                FROM InvestmentHolding
                WHERE portfolioId = @PortfolioId
                ORDER BY id";

            Portfolio portfolio = new Portfolio
            {
                UserId = userId
            };

            using SqlConnection openDatabaseConnection = new SqlConnection(DatabaseConfig.ConnectionString);
            openDatabaseConnection.Open();

            using (SqlCommand selectPortfolioCommand = new SqlCommand(selectPortfolioQuery, openDatabaseConnection))
            {
                selectPortfolioCommand.Parameters.Add("@UserId", System.Data.SqlDbType.Int).Value = userId;

                using SqlDataReader portfolioReader = selectPortfolioCommand.ExecuteReader();
                if (portfolioReader.Read())
                {
                    portfolio.Id = portfolioReader.GetInt32(0);
                    portfolio.UserId = portfolioReader.GetInt32(1);
                    portfolio.TotalValue = portfolioReader.GetDecimal(2);
                    portfolio.TotalGainLoss = portfolioReader.GetDecimal(3);
                    portfolio.GainLossPercent = portfolioReader.GetDecimal(4);
                }
                else
                {
                    return portfolio;
                }
            }

            using SqlCommand selectHoldingsCommand = new SqlCommand(selectHoldingsQuery, openDatabaseConnection);
            selectHoldingsCommand.Parameters.Add("@PortfolioId", System.Data.SqlDbType.Int).Value = portfolio.Id;

            using SqlDataReader holdingsReader = selectHoldingsCommand.ExecuteReader();
            while (holdingsReader.Read())
            {
                portfolio.Holdings.Add(new InvestmentHolding
                {
                    Id = holdingsReader.GetInt32(0),
                    Ticker = holdingsReader.IsDBNull(1) ? string.Empty : holdingsReader.GetString(1),
                    AssetType = holdingsReader.IsDBNull(2) ? string.Empty : holdingsReader.GetString(2),
                    Quantity = holdingsReader.GetDecimal(3),
                    AvgPurchasePrice = holdingsReader.GetDecimal(4),
                    CurrentPrice = holdingsReader.GetDecimal(5),
                    UnrealizedGainLoss = holdingsReader.GetDecimal(6)
                });
            }

            return portfolio;
        }
    }
}
