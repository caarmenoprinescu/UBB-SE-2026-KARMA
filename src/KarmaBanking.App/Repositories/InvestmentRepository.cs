using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KarmaBanking.App.Repositories.Interfaces;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.UI.Windowing;

namespace KarmaBanking.App.Repositories
{
    internal class InvestmentRepository : IInvestmentRepository
    {
        public async Task RecordCryptoTradeAsync(int portfolioId, string ticker, string actionType, decimal quantity, decimal pricePerUnit, decimal fees)
        public Portfolio GetPortfolio(int userId)
        {
            using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();
            await connection.OpenAsync();
            const string selectPortfolioQuery = @"
                SELECT id, userId, totalValue, totalGainLoss, gainLossPercent
                FROM Portfolio
                WHERE userId = @UserId";

            using SqlTransaction transaction = (SqlTransaction) await connection.BeginTransactionAsync();
            const string selectHoldingsQuery = @"
                SELECT id, ticker, assetType, quantity, avgPurchasePrice, currentPrice, unrealizedGainLoss
                FROM InvestmentHolding
                WHERE portfolioId = @PortfolioId
                ORDER BY id";

            try
            Portfolio portfolio = new Portfolio
            {
                int? holdingId = null;
                decimal currentQuantity = 0;
                decimal currentAvgPrice = 0;
                UserId = userId
            };

                string checkHoldingQuery = "SELECT id, quantity, avgPurchasePrice FROM InvestmentHolding WHERE portfolioId = @PortfolioId AND ticker = @Ticker";
                using (SqlCommand checkCmd = new SqlCommand(checkHoldingQuery, connection, transaction))
                {
                    checkCmd.Parameters.AddWithValue("@PortfolioId", portfolioId);
                    checkCmd.Parameters.AddWithValue("@Ticker", ticker);
            using SqlConnection openDatabaseConnection = new SqlConnection(DatabaseConfig.ConnectionString);
            openDatabaseConnection.Open();

                    using SqlDataReader reader = await checkCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
            using (SqlCommand selectPortfolioCommand = new SqlCommand(selectPortfolioQuery, openDatabaseConnection))
            {
                        holdingId = reader.GetInt32(0);
                        currentQuantity = reader.GetDecimal(1);
                        currentAvgPrice = reader.GetDecimal(2);
                    }
                }
                selectPortfolioCommand.Parameters.Add("@UserId", System.Data.SqlDbType.Int).Value = userId;

                if (actionType.Equals("BUY", StringComparison.OrdinalIgnoreCase))
                using SqlDataReader portfolioReader = selectPortfolioCommand.ExecuteReader();
                if (portfolioReader.Read())
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
                    portfolio.Id = portfolioReader.GetInt32(0);
                    portfolio.UserId = portfolioReader.GetInt32(1);
                    portfolio.TotalValue = portfolioReader.GetDecimal(2);
                    portfolio.TotalGainLoss = portfolioReader.GetDecimal(3);
                    portfolio.GainLossPercent = portfolioReader.GetDecimal(4);
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
                    return portfolio;
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
            using SqlCommand selectHoldingsCommand = new SqlCommand(selectHoldingsQuery, openDatabaseConnection);
            selectHoldingsCommand.Parameters.Add("@PortfolioId", System.Data.SqlDbType.Int).Value = portfolio.Id;

                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
            using SqlDataReader holdingsReader = selectHoldingsCommand.ExecuteReader();
            while (holdingsReader.Read())
            {
                    throw new ArgumentException("ActionType must be either 'BUY' or 'SELL'.");
                }

                string insertTxQuery = @"
                    INSERT INTO InvestmentTransaction (holdingId, ticker, actionType, quantity, pricePerUnit, fees, orderType, executedAt)
                    VALUES (@HoldingId, @Ticker, @ActionType, @Quantity, @PricePerUnit, @Fees, 'Market', @ExecutedAt)";
                using (SqlCommand txCmd = new SqlCommand(insertTxQuery, connection, transaction))
                portfolio.Holdings.Add(new InvestmentHolding
                {
                    txCmd.Parameters.AddWithValue("@HoldingId", holdingId.Value);
                    txCmd.Parameters.AddWithValue("@Ticker", ticker);
                    txCmd.Parameters.AddWithValue("@ActionType", actionType.ToUpper());
                    txCmd.Parameters.AddWithValue("@Quantity", quantity);
                    txCmd.Parameters.AddWithValue("@PricePerUnit", pricePerUnit);
                    txCmd.Parameters.AddWithValue("@Fees", fees);
                    txCmd.Parameters.AddWithValue("@ExecutedAt", DateTime.Now);

                    await txCmd.ExecuteNonQueryAsync();
                    Id = holdingsReader.GetInt32(0),
                    Ticker = holdingsReader.IsDBNull(1) ? string.Empty : holdingsReader.GetString(1),
                    AssetType = holdingsReader.IsDBNull(2) ? string.Empty : holdingsReader.GetString(2),
                    Quantity = holdingsReader.GetDecimal(3),
                    AvgPurchasePrice = holdingsReader.GetDecimal(4),
                    CurrentPrice = holdingsReader.GetDecimal(5),
                    UnrealizedGainLoss = holdingsReader.GetDecimal(6)
                });
            }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            return portfolio;
        }
    }
}
