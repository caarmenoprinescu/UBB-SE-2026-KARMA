namespace KarmaBanking.App.Services.Interfaces
{
    using System;
    using System.Collections.Generic;

    public interface IMarketDataService
    {
        void StartPolling(List<string> tickerSymbols);
        void StopPolling();
        decimal GetPrice(string tickerSymbol);
        void RegisterPriceUpdateHandler(Action updateHandler);
    }
}