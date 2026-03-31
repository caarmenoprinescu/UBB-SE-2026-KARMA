using System.Collections.Generic;
using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace KarmaBanking.App.Repositories
{
    public class InvestmentRepository : IInvestmentRepository
    {
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
