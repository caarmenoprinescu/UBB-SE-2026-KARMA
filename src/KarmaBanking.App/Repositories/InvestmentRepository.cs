using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace KarmaBanking.App.Repositories
{
    public class InvestmentRepository : IInvestmentRepository
    {
        public async Task RecordCryptoTradeAsync(int portfolioId, string ticker, string actionType, decimal quantity, decimal pricePerUnit, decimal fees) 
        {
            // Establish a connection to the database using the shared configuration
            using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();
            await connection.OpenAsync();

            using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                int? holdingId = null;
                decimal currentQuantity = 0;
                decimal currentAvgPrice = 0;

                // 1. Check if the user already has an existing holding for this specific crypto asset in their portfolio
                string checkHoldingQuery = "SELECT id, quantity, avgPurchasePrice FROM InvestmentHolding WHERE portfolioId = @PortfolioId AND ticker = @Ticker";
                using (SqlCommand checkCmd = new SqlCommand(checkHoldingQuery, connection, transaction))
                {
                    checkCmd.Parameters.AddWithValue("@PortfolioId", portfolioId);
                    checkCmd.Parameters.AddWithValue("@Ticker", ticker);

                    using SqlDataReader reader = await checkCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
            {
                        // Store the current holding details if found
                        holdingId = reader.GetInt32(0);
                        currentQuantity = reader.GetDecimal(1);
                        currentAvgPrice = reader.GetDecimal(2);
                    }
                }

                // 2. Process the trade based on the specified action type (BUY or SELL)
                if (actionType.Equals("BUY", StringComparison.OrdinalIgnoreCase))
                {
                    if (holdingId.HasValue)
                    {
                        // 2a. The user already owns this asset. Calculate the new weighted average price and total quantity.
                        decimal totalCost = (currentQuantity * currentAvgPrice) + (quantity * pricePerUnit);
                        decimal newQuantity = currentQuantity + quantity;
                        decimal newAvgPrice = totalCost / newQuantity;

                        // Update the existing holding record
                        string updateHoldingQuery = "UPDATE InvestmentHolding SET quantity = @NewQuantity, avgPurchasePrice = @NewAvgPrice WHERE id = @HoldingId";
                        using SqlCommand updateCmd = new SqlCommand(updateHoldingQuery, connection, transaction);
                        updateCmd.Parameters.AddWithValue("@NewQuantity", newQuantity);
                        updateCmd.Parameters.AddWithValue("@NewAvgPrice", newAvgPrice);
                        updateCmd.Parameters.AddWithValue("@HoldingId", holdingId.Value);

                        await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                        // 2b. The user does not own this asset yet. Insert a new holding record.
                        string insertHoldingQuery = @"
                            INSERT INTO InvestmentHolding (portfolioId, ticker, assetType, quantity, avgPurchasePrice, currentPrice, unrealizedGainLoss)
                            OUTPUT INSERTED.id
                            VALUES (@PortfolioId, @Ticker, 'Crypto', @Quantity, @AvgPrice, @AvgPrice, 0)";

                        using SqlCommand insertCmd = new SqlCommand(insertHoldingQuery, connection, transaction);
                        insertCmd.Parameters.AddWithValue("@PortfolioId", portfolioId);
                        insertCmd.Parameters.AddWithValue("@Ticker", ticker);
                        insertCmd.Parameters.AddWithValue("@Quantity", quantity);
                        insertCmd.Parameters.AddWithValue("@AvgPrice", pricePerUnit);

                        // Execute the insert and retrieve the newly generated holding ID
                        holdingId = (int)await insertCmd.ExecuteScalarAsync();
                    }
                }
                else if (actionType.Equals("SELL", StringComparison.OrdinalIgnoreCase))
                {
                    // 2c. Validate that the user actually owns enough of the asset to sell
                    if (!holdingId.HasValue || currentQuantity < quantity)
                    {
                        throw new InvalidOperationException("Insufficient wallet balance to execute this sell order.");
            }

                    // Deduct the sold quantity from the current holding
                    decimal newQuantity = currentQuantity - quantity;
                    string updateHoldingQuery = "UPDATE InvestmentHolding SET quantity = @NewQuantity WHERE id = @HoldingId";

                    using SqlCommand updateCmd = new SqlCommand(updateHoldingQuery, connection, transaction);
                    updateCmd.Parameters.AddWithValue("@NewQuantity", newQuantity);
                    updateCmd.Parameters.AddWithValue("@HoldingId", holdingId.Value);

                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
            {
                    // Reject unsupported action types to prevent data corruption
                    throw new ArgumentException("ActionType must be either 'BUY' or 'SELL'.");
                }

                // 3. Record the transaction details in the history table
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

                // 4. Commit the transaction if all operations (holding update + transaction log) succeed
                await transaction.CommitAsync();
            }
            catch
            {
                // Revert all database changes if any exception occurs during the process
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

        public async Task<List<InvestmentTransaction>> GetInvestmentLogsAsync(int portfolioId, DateTime? startDate = null, DateTime? endDate = null, string? ticker = null)
        {
            var logs = new List<InvestmentTransaction>();

            using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();
            await connection.OpenAsync();

            // Base query with INNER JOIN to filter by the user's portfolio
            string query = @"
                SELECT t.id, t.holdingId, t.ticker, t.actionType, t.quantity, 
                       t.pricePerUnit, t.fees, t.orderType, t.executedAt 
                FROM InvestmentTransaction t
                INNER JOIN InvestmentHolding h ON t.holdingId = h.id
                WHERE h.portfolioId = @PortfolioId";

            // Dynamically append filters
            if (startDate.HasValue)
            {
                query += " AND t.executedAt >= @StartDate";
            }
            if (endDate.HasValue)
            {
                query += " AND t.executedAt <= @EndDate";
            }
            if (!string.IsNullOrWhiteSpace(ticker))
            {
                query += " AND t.ticker = @Ticker";
            }

            // Order by most recent transactions first
            query += " ORDER BY t.executedAt DESC";

            using SqlCommand cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@PortfolioId", portfolioId);

            // Add parameters conditionally to match the dynamic query string
            if (startDate.HasValue)
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate.Value);
            }
            if (endDate.HasValue)
            {
                cmd.Parameters.AddWithValue("@EndDate", endDate.Value);
            }
            if (!string.IsNullOrWhiteSpace(ticker))
            {
                cmd.Parameters.AddWithValue("@Ticker", ticker);
            }

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new InvestmentTransaction
                {
                    Id = reader.GetInt32(0),
                    HoldingId = reader.GetInt32(1),
                    Ticker = reader.GetString(2),
                    ActionType = reader.GetString(3),
                    Quantity = reader.GetDecimal(4),
                    PricePerUnit = reader.GetDecimal(5),
                    Fees = reader.GetDecimal(6),
                    OrderType = reader.GetString(7),
                    ExecutedAt = reader.GetDateTime(8)
                });
            }

            return logs;
        }
    }
}
