using System;

namespace CrashAndBurn.Strategy
{
    class TrailingStopStrategy : BaseStrategy
    {
        private decimal _TrailingStopPercentage;
        private int _RecoveryDays;

        protected decimal? MaximumPrice { get; set; }
        protected decimal? TrailingStop { get; set; }
        protected DateTime? RecoveryDate { get; set; }

        public TrailingStopStrategy(decimal trailingStopPercentage, int recoveryDays)
            : this($"Trailing stop ({trailingStopPercentage:P1}, {recoveryDays} recovery days)", trailingStopPercentage, recoveryDays)
        {
        }

        public TrailingStopStrategy(string name, decimal trailingStopPercentage, int recoveryDays)
            : base(name)
        {
            _TrailingStopPercentage = trailingStopPercentage;
            _RecoveryDays = recoveryDays;
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
            RecoveryDate = stockData.Date.AddDays(_RecoveryDays);
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
            TrailingStop = (1.0m - _TrailingStopPercentage) * price;
        }
    }
}
