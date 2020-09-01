using System.Collections.Generic;

namespace CrashAndBurn.Common
{
    public class StockMarket
    {
        private HashSet<Stock> _Stocks = new HashSet<Stock>();

        public StockMarket(IEnumerable<Stock> stocks)
        {
            foreach (var stock in stocks)
            {
                _Stocks.Add(stock);
            }
        }
    }
}
