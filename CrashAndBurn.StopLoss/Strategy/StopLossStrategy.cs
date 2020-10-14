using CrashAndBurn.Common;
using System;

namespace CrashAndBurn.StopLoss.Strategy
{
    class StopLossStrategy : BaseStrategy
    {
        private const string BaseStrategyName = "Stop-loss";

        public override string StrategyName => BaseStrategyName;

        private decimal _stopLossPercentage;
        private int _recoveryDays;

        private decimal? _stopLoss;
        private DateTime? _recoveryDate;

        public StopLossStrategy(decimal stopLossPercentage, int recoveryDays)
            : base($"{BaseStrategyName} ({stopLossPercentage:P1} pullback, {recoveryDays} recovery days)")
        {
            _stopLossPercentage = stopLossPercentage;
            _recoveryDays = recoveryDays;
        }

        public override void Buy(StockData stockData)
        {
            base.Buy(stockData);
            decimal price = stockData.Open;
            _stopLoss = (1.0m - _stopLossPercentage) * price;
            _recoveryDate = null;
        }

        public override void Sell(StockData stockData, bool low)
        {
            base.Sell(stockData, low);
            _stopLoss = null;
            _recoveryDate = stockData.Date.AddDays(_recoveryDays);
        }

        public override void ProcessStockData(StockData stockData)
        {
            if (_stopLoss.HasValue && stockData.Low <= _stopLoss.Value)
            {
                Sell(stockData, true);
            }
            else if (_recoveryDate.HasValue && stockData.Date >= _recoveryDate.Value)
            {
                Buy(stockData);
            }
        }
    }
}
