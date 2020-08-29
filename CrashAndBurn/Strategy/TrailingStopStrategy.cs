using System;

namespace CrashAndBurn.Strategy
{
    class TrailingStopStrategy : BaseStrategy
    {
        private decimal _TrailingStopPercentage;
        private int _RecoveryDays;

        private decimal? _MaximumPrice;
        private decimal? _TrailingStop;
        private DateTime? _RecoveryDate;

        public TrailingStopStrategy(decimal trailingStopPercentage, int recoveryDays)
            : base($"Trailing stop ({trailingStopPercentage:P1}, {recoveryDays} recovery days)")
        {
            _TrailingStopPercentage = trailingStopPercentage;
            _RecoveryDays = recoveryDays;
        }

        public override void Buy(StockData stockData)
        {
            base.Buy(stockData);
            decimal price = stockData.Open;
            _MaximumPrice = price;
            SetTrailingStop(price);
            _RecoveryDate = null;
        }

        public override void Sell(StockData stockData, bool low)
        {
            base.Sell(stockData, low);
            _MaximumPrice = null;
            _TrailingStop = null;
            _RecoveryDate = stockData.Date.AddDays(_RecoveryDays);
        }

        public override void ProcessStockData(StockData stockData)
        {
            if (_MaximumPrice.HasValue && stockData.High > _MaximumPrice)
            {
                _MaximumPrice = stockData.High;
                SetTrailingStop(stockData.High);
            }
            if (_TrailingStop.HasValue && stockData.Low <= _TrailingStop.Value)
            {
                Sell(stockData, true);
            }
            else if (_RecoveryDate.HasValue && stockData.Date >= _RecoveryDate.Value)
            {
                Buy(stockData);
            }
        }

        private void SetTrailingStop(decimal price)
        {
            _TrailingStop = (1.0m - _TrailingStopPercentage) * price;
        }
    }
}
