using KarmaBanking.App.Models;
using System.Collections.Generic;
using System.Text;

namespace KarmaBanking.App.Utils
{
    public static class CsvExportUtility
    {
        public static string ExportTransactionsToCsv(IEnumerable<InvestmentTransaction> transactions)
        {
            var builder = new StringBuilder();

            // 1. Append the CSV header row
            builder.AppendLine("Transaction ID,Ticker,Action,Quantity,Price Per Unit,Fees,Order Type,Executed At");

            // 2. Append each transaction as a comma-separated row
            if (transactions != null)
            {
                foreach (var tx in transactions)
                {
                    builder.AppendLine($"{tx.Id},{tx.Ticker},{tx.ActionType},{tx.Quantity},{tx.PricePerUnit},{tx.Fees},{tx.OrderType},{tx.ExecutedAt:yyyy-MM-dd HH:mm:ss}");
                }
            }

            return builder.ToString();
        }
    }
}