using System;

namespace CrashAndBurn.Strategy
{
    class TrailingStopMondayStrategy : TrailingStopStrategy
    {
        public TrailingStopMondayStrategy(decimal trailingStopPercentage, int recoveryDays)
            : base($"Trailing stop, buy on Monday ({trailingStopPercentage:P1}, {recoveryDays} recovery days)", trailingStopPercentage, recoveryDays)
        {
        }

        public override void ProcessStockData(StockData stockData)
        {
            if (MaximumPrice.HasValue && stockData.High > MaximumPrice)
            {
                MaximumPrice = stockData.High;
                SetTrailingStop(stockData.High);
            }
            if (TrailingStop.HasValue && stockData.Low <= TrailingStop.Value)
            {
                Sell(stockData, true);
            }
            else if (RecoveryDate.HasValue && stockData.Date >= RecoveryDate.Value && stockData.Date.DayOfWeek == DayOfWeek.Monday)
            {
                Buy(stockData);
            }
        }
    }
}
