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
    public class InvestmentRepository : IInvestmentRepository
    internal class InvestmentRepository : IInvestmentRepository
    {
        public async Task RecordCryptoTradeAsync(int portfolioId, string ticker, string actionType, decimal quantity, decimal pricePerUnit, decimal fees)
        {
            // Establish a connection to the database using the shared configuration
            using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();
            await connection.OpenAsync();

            // Begin a database transaction to ensure that both the holding update 
            // and the transaction record are committed atomically (all or nothing).
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
    }
}
