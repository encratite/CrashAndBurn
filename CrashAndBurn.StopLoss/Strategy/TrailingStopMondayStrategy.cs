using CrashAndBurn.Common;
using System;

namespace CrashAndBurn.StopLoss.Strategy
{
    class TrailingStopMondayStrategy : TrailingStopStrategy
    {
        private const string _StrategyName = "Trailing stop, Monday anomaly";

        public override string StrategyName => _StrategyName;

        public TrailingStopMondayStrategy(decimal trailingStopPercentage, int recoveryDays)
            : base($"{_StrategyName} ({trailingStopPercentage:P1} pullback, {recoveryDays} recovery days)", trailingStopPercentage, recoveryDays)
        {
        }

        public override void Buy(StockData stockData)
        {
            if (IsMonday(stockData))
            {
                base.Buy(stockData);
            }
        }

        public override void ProcessStockData(StockData stockData)
        {
            if (FirstPurchase)
            {
                if (IsMonday(stockData))
                {
                    Buy(stockData);
                }
            }
            else
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
                else if (RecoveryDate.HasValue && stockData.Date >= RecoveryDate.Value && IsMonday(stockData))
                {
                    Buy(stockData);
                }
            }
        }

        private bool IsMonday(StockData stockData)
        {
            return stockData.Date.DayOfWeek == DayOfWeek.Monday;
        }
    }
}
