using CrashAndBurn.Common;
using System;

namespace CrashAndBurn.StopLoss.Strategy
{
    class TrailingStopStrategy : BaseStrategy
    {
        private const string BaseStrategyName = "Trailing stop";

        public override string StrategyName => BaseStrategyName;

        private decimal trailingStopPercentage;
        private int? recoveryDays;

        protected decimal? MaximumPrice { get; set; }
        protected decimal? TrailingStop { get; set; }
        protected DateTime? RecoveryDate { get; set; }

        public TrailingStopStrategy(decimal trailingStopPercentage, int recoveryDays)
            : this($"{BaseStrategyName} ({trailingStopPercentage:P1} pullback, {recoveryDays} recovery days)", trailingStopPercentage, recoveryDays)
        {
        }

        public TrailingStopStrategy(string name, decimal trailingStopPercentage, int? recoveryDays)
            : base(name)
        {
            this.trailingStopPercentage = trailingStopPercentage;
            this.recoveryDays = recoveryDays;
        }

        public override void Buy(StockData stockData)
        {
            base.Buy(stockData);
            decimal price = stockData.Open;
            MaximumPrice = price;
            SetTrailingStop(price);
            RecoveryDate = null;
        }

        public override void Sell(StockData stockData, bool low)
        {
            base.Sell(stockData, low);
            MaximumPrice = null;
            TrailingStop = null;
            if (recoveryDays.HasValue)
            {
                RecoveryDate = stockData.Date.AddDays(recoveryDays.Value);
            }
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
            else if (RecoveryDate.HasValue && stockData.Date >= RecoveryDate.Value)
            {
                Buy(stockData);
            }
        }

        protected void SetTrailingStop(decimal price)
        {
            TrailingStop = (1.0m - trailingStopPercentage) * price;
        }
    }
}
