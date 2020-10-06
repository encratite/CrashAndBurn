using CrashAndBurn.Common;
using System;

namespace CrashAndBurn.StopLoss.Strategy
{
    class TrailingStopJanuaryStrategy : TrailingStopStrategy
    {
        private const string BaseStrategyName = "Trailing stop, January anomaly";

        public override string StrategyName => BaseStrategyName;

        private int offsetDays;

        public TrailingStopJanuaryStrategy(decimal trailingStopPercentage, int recoveryDays, int offsetDays)
            : base($"{BaseStrategyName} ({trailingStopPercentage:P1} pullback, {recoveryDays} recovery days, {offsetDays} offset days)", trailingStopPercentage, recoveryDays)
        {
            this.offsetDays = offsetDays;
        }

        public override void ProcessStockData(StockData stockData)
        {
            if (MaximumPrice.HasValue && stockData.High > MaximumPrice)
            {
                MaximumPrice = stockData.High;
                SetTrailingStop(stockData.High);
            }
            int targetYear = stockData.Date.Month > 6 ? stockData.Date.Year + 1 : stockData.Date.Year;
            var januaryDate = new DateTime(targetYear, 1, 1);
            var targetDate = januaryDate.AddDays(offsetDays);
            if (TrailingStop.HasValue && stockData.Low <= TrailingStop.Value)
            {
                Sell(stockData, true);
            }
            else if (TrailingStop.HasValue && stockData.Date >= targetDate)
            {
                Sell(stockData, false);
            }
            else if (RecoveryDate.HasValue && stockData.Date >= RecoveryDate.Value)
            {
                Buy(stockData);
            }
        }
    }
}
