using System;

namespace CrashAndBurn.Strategy
{
    class StopLossStrategy : BaseStrategy
    {
        private decimal _StopLossPercentage;
        private int _RecoveryDays;

        private decimal? _StopLoss;
        private DateTime? _RecoveryDate;

        public StopLossStrategy(decimal stopLossPercentage, int recoveryDays)
            : base($"Stop-loss ({stopLossPercentage:P1} pullback, {recoveryDays} recovery days)")
        {
            _StopLossPercentage = stopLossPercentage;
            _RecoveryDays = recoveryDays;
        }

        public override void Buy(StockData stockData)
        {
            base.Buy(stockData);
            decimal price = stockData.Open;
            _StopLoss = (1.0m - _StopLossPercentage) * price;
            _RecoveryDate = null;
        }

        public override void Sell(StockData stockData, bool low)
        {
            base.Sell(stockData, low);
            _StopLoss = null;
            _RecoveryDate = stockData.Date.AddDays(_RecoveryDays);
        }

        public override void ProcessStockData(StockData stockData)
        {
            if (_StopLoss.HasValue && stockData.Low <= _StopLoss.Value)
            {
                Sell(stockData, true);
            }
            else if (_RecoveryDate.HasValue && stockData.Date >= _RecoveryDate.Value)
            {
                Buy(stockData);
            }
        }
    }
}
