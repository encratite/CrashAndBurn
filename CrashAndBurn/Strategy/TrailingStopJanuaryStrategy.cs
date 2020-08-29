using System;

namespace CrashAndBurn.Strategy
{
    class TrailingStopJanuaryStrategy : TrailingStopStrategy
    {
        private const string _StrategyName = "Trailing stop, January anomaly";

        public override string StrategyName => _StrategyName;

        private int _OffsetDays;

        public TrailingStopJanuaryStrategy(decimal trailingStopPercentage, int recoveryDays, int offsetDays)
            : base($"{_StrategyName} ({trailingStopPercentage:P1} pullback, {recoveryDays} recovery days, {offsetDays} offset days)", trailingStopPercentage, recoveryDays)
        {
            _OffsetDays = offsetDays;
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
            var targetDate = januaryDate.AddDays(_OffsetDays);
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
