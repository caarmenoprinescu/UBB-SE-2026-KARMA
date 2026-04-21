namespace KarmaBanking.App.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;
    using KarmaBanking.App.Data;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Repositories.Interfaces;
    using Microsoft.Data.SqlClient;

    public class InvestmentRepository : IInvestmentRepository
    {
        private const string AssetTypeCrypto = "Crypto";
        private const string OrderTypeMarket = "Market";

        /// <summary>
        /// Records a crypto trade and updates the holding with final values calculated by the Service.
        /// </summary>
        public async Task RecordCryptoTradeAsync(
            int portfolioIdentificationNumber,
            string ticker,
            string actionType,
            decimal quantity,
            decimal pricePerUnit,
            decimal fees,
            decimal finalQuantity,
            decimal finalAveragePrice)
        {
            using var sqlConnection = DatabaseConfig.GetDatabaseConnection();
            await sqlConnection.OpenAsync();

            using var sqlTransaction = (SqlTransaction)await sqlConnection.BeginTransactionAsync();

            try
            {
                int? holdingIdentificationNumber = null;

                // 1. Check if the holding already exists
                var checkHoldingSqlQuery = "SELECT id FROM InvestmentHolding WHERE portfolioId = @PortfolioId AND ticker = @Ticker";
                using (var checkCommand = new SqlCommand(checkHoldingSqlQuery, sqlConnection, sqlTransaction))
                {
                    checkCommand.Parameters.AddWithValue("@PortfolioId", portfolioIdentificationNumber);
                    checkCommand.Parameters.AddWithValue("@Ticker", ticker);

                    var result = await checkCommand.ExecuteScalarAsync();
                    if (result != null)
                    {
                        holdingIdentificationNumber = (int)result;
                    }
                }

                // 2. Update existing holding or Insert new holding using FINAL values provided by the Service
                if (holdingIdentificationNumber.HasValue)
                {
                    var updateHoldingSqlQuery = "UPDATE InvestmentHolding SET quantity = @FinalQuantity, avgPurchasePrice = @FinalAveragePrice WHERE id = @HoldingId";
                    using var updateCommand = new SqlCommand(updateHoldingSqlQuery, sqlConnection, sqlTransaction);
                    updateCommand.Parameters.AddWithValue("@FinalQuantity", finalQuantity);
                    updateCommand.Parameters.AddWithValue("@FinalAveragePrice", finalAveragePrice);
                    updateCommand.Parameters.AddWithValue("@HoldingId", holdingIdentificationNumber.Value);

                    await updateCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    var insertHoldingSqlQuery = @"
                            INSERT INTO InvestmentHolding (portfolioId, ticker, assetType, quantity, avgPurchasePrice, currentPrice, unrealizedGainLoss)
                            OUTPUT INSERTED.id
                            VALUES (@PortfolioId, @Ticker, @AssetType, @Quantity, @AveragePrice, @AveragePrice, 0)";

                    using var insertCommand = new SqlCommand(insertHoldingSqlQuery, sqlConnection, sqlTransaction);
                    insertCommand.Parameters.AddWithValue("@PortfolioId", portfolioIdentificationNumber);
                    insertCommand.Parameters.AddWithValue("@Ticker", ticker);
                    insertCommand.Parameters.AddWithValue("@AssetType", AssetTypeCrypto);
                    insertCommand.Parameters.AddWithValue("@Quantity", finalQuantity);
                    insertCommand.Parameters.AddWithValue("@AveragePrice", finalAveragePrice);

                    holdingIdentificationNumber = (int)await insertCommand.ExecuteScalarAsync();
                }

                // 3. Log the transaction details
                var insertTransactionSqlQuery = @"
                        INSERT INTO InvestmentTransaction (holdingId, ticker, actionType, quantity, pricePerUnit, fees, orderType, executedAt)
                        VALUES (@HoldingId, @Ticker, @ActionType, @Quantity, @PricePerUnit, @Fees, @OrderType, @ExecutedAt)";

                using var transactionLogCommand = new SqlCommand(insertTransactionSqlQuery, sqlConnection, sqlTransaction);
                transactionLogCommand.Parameters.AddWithValue("@HoldingId", holdingIdentificationNumber);
                transactionLogCommand.Parameters.AddWithValue("@Ticker", ticker);
                transactionLogCommand.Parameters.AddWithValue("@ActionType", actionType.ToUpper());
                transactionLogCommand.Parameters.AddWithValue("@Quantity", quantity);
                transactionLogCommand.Parameters.AddWithValue("@PricePerUnit", pricePerUnit);
                transactionLogCommand.Parameters.AddWithValue("@Fees", fees);
                transactionLogCommand.Parameters.AddWithValue("@OrderType", OrderTypeMarket);
                transactionLogCommand.Parameters.AddWithValue("@ExecutedAt", DateTime.Now);

                await transactionLogCommand.ExecuteNonQueryAsync();

                await sqlTransaction.CommitAsync();
            }
            catch (Exception)
            {
                await sqlTransaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Retrieves a user's portfolio and associated holdings.
        /// </summary>
        public Portfolio GetPortfolio(int userIdentificationNumber)
        {
            const string selectPortfolioSqlQuery = "SELECT id, userId, totalValue, totalGainLoss, gainLossPercent FROM Portfolio WHERE userId = @UserId";
            const string selectHoldingsSqlQuery = "SELECT id, ticker, assetType, quantity, avgPurchasePrice, currentPrice, unrealizedGainLoss FROM InvestmentHolding WHERE portfolioId = @PortfolioId ORDER BY id";

            var userPortfolio = new Portfolio { UserIdentificationNumber = userIdentificationNumber };

            using var sqlConnection = new SqlConnection(DatabaseConfig.DatabaseConnectionString);
            sqlConnection.Open();

            using (var selectPortfolioCommand = new SqlCommand(selectPortfolioSqlQuery, sqlConnection))
            {
                selectPortfolioCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userIdentificationNumber;
                using var portfolioDataReader = selectPortfolioCommand.ExecuteReader();
                if (portfolioDataReader.Read())
                {
                    userPortfolio.IdentificationNumber = portfolioDataReader.GetInt32(0);
                    userPortfolio.TotalValue = portfolioDataReader.GetDecimal(2);
                    userPortfolio.TotalGainLoss = portfolioDataReader.GetDecimal(3);
                    userPortfolio.GainLossPercent = portfolioDataReader.GetDecimal(4);
                }
                else
                {
                    return userPortfolio;
                }
            }

            using (var selectHoldingsCommand = new SqlCommand(selectHoldingsSqlQuery, sqlConnection))
            {
                selectHoldingsCommand.Parameters.Add("@PortfolioId", SqlDbType.Int).Value = userPortfolio.IdentificationNumber;
                using var holdingsDataReader = selectHoldingsCommand.ExecuteReader();
                while (holdingsDataReader.Read())
                {
                    userPortfolio.Holdings.Add(new InvestmentHolding
                    {
                        IdentificationNumber = holdingsDataReader.GetInt32(0),
                        Ticker = holdingsDataReader.IsDBNull(1) ? string.Empty : holdingsDataReader.GetString(1),
                        AssetType = holdingsDataReader.IsDBNull(2) ? string.Empty : holdingsDataReader.GetString(2),
                        Quantity = holdingsDataReader.GetDecimal(3),
                        AveragePurchasePrice = holdingsDataReader.GetDecimal(4),
                        CurrentPrice = holdingsDataReader.GetDecimal(5),
                        UnrealizedGainLoss = holdingsDataReader.GetDecimal(6)
                    });
                }
            }

            return userPortfolio;
        }

        /// <summary>
        /// Retrieves transaction logs with optional filtering.
        /// </summary>
        public async Task<List<InvestmentTransaction>> GetInvestmentLogsAsync(
            int portfolioIdentificationNumber,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? ticker = null)
        {
            var investmentLogs = new List<InvestmentTransaction>();

            using var sqlConnection = DatabaseConfig.GetDatabaseConnection();
            await sqlConnection.OpenAsync();

            var filterLogsSqlQuery = @"
                    SELECT t.id, t.holdingId, t.ticker, t.actionType, t.quantity, t.pricePerUnit, t.fees, t.orderType, t.executedAt 
                    FROM InvestmentTransaction t
                    INNER JOIN InvestmentHolding h ON t.holdingId = h.id
                    WHERE h.portfolioId = @PortfolioId";

            if (startDate.HasValue) filterLogsSqlQuery += " AND t.executedAt >= @StartDate";
            if (endDate.HasValue) filterLogsSqlQuery += " AND t.executedAt <= @EndDate";
            if (!string.IsNullOrWhiteSpace(ticker)) filterLogsSqlQuery += " AND t.ticker = @Ticker";

            filterLogsSqlQuery += " ORDER BY t.executedAt DESC";

            using var filterCommand = new SqlCommand(filterLogsSqlQuery, sqlConnection);
            filterCommand.Parameters.AddWithValue("@PortfolioId", portfolioIdentificationNumber);
            if (startDate.HasValue) filterCommand.Parameters.AddWithValue("@StartDate", startDate.Value);
            if (endDate.HasValue) filterCommand.Parameters.AddWithValue("@EndDate", endDate.Value);
            if (!string.IsNullOrWhiteSpace(ticker)) filterCommand.Parameters.AddWithValue("@Ticker", ticker);

            using var transactionLogDataReader = await filterCommand.ExecuteReaderAsync();
            while (await transactionLogDataReader.ReadAsync())
            {
                investmentLogs.Add(new InvestmentTransaction
                {
                    IdentificationNumber = transactionLogDataReader.GetInt32(0),
                    HoldingIdentificationNumber = transactionLogDataReader.GetInt32(1),
                    Ticker = transactionLogDataReader.GetString(2),
                    ActionType = transactionLogDataReader.GetString(3),
                    Quantity = transactionLogDataReader.GetDecimal(4),
                    PricePerUnit = transactionLogDataReader.GetDecimal(5),
                    Fees = transactionLogDataReader.GetDecimal(6),
                    OrderType = transactionLogDataReader.GetString(7),
                    ExecutedAt = transactionLogDataReader.GetDateTime(8)
                });
            }

            return investmentLogs;
        }
    }
}