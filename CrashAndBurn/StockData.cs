using System;

namespace CrashAndBurn
{
    class StockData
    {
        public DateTime Date { get; private set; }
        public decimal Open { get; private set; }
        public decimal High { get; private set; }
        public decimal Low { get; private set; }
        public decimal Close { get; private set; }
        public decimal AdjustedClose { get; private set; }
        public long Volume{ get; private set; }

        public StockData(DateTime date, decimal open, decimal high, decimal low, decimal close, decimal adjustedClose, long volume)
        {
            Date = date;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            AdjustedClose = adjustedClose;
            Volume = volume;
        }
    }
}
