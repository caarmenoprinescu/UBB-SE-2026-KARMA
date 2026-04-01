using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KarmaBanking.App.Services.Interfaces
{
    internal interface IInvestmentService
    {
        /// <summary>
        /// Executes a crypto trade, calculates fees, and records it in the database.
        /// </summary>
        Task<bool> ExecuteCryptoTradeAsync(int portfolioId, string ticker, string actionType, decimal quantity, decimal pricePerUnit);
    }
}
