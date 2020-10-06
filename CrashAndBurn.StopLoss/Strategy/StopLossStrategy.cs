using CrashAndBurn.Common;
using System;

namespace CrashAndBurn.StopLoss.Strategy
{
    class StopLossStrategy : BaseStrategy
    {
        private const string BaseStrategyName = "Stop-loss";

        public override string StrategyName => BaseStrategyName;

        private decimal stopLossPercentage;
        private int recoveryDays;

        private decimal? stopLoss;
        private DateTime? recoveryDate;

        public StopLossStrategy(decimal stopLossPercentage, int recoveryDays)
            : base($"{BaseStrategyName} ({stopLossPercentage:P1} pullback, {recoveryDays} recovery days)")
        {
            this.stopLossPercentage = stopLossPercentage;
            this.recoveryDays = recoveryDays;
        }

        public override void Buy(StockData stockData)
        {
            base.Buy(stockData);
            decimal price = stockData.Open;
            stopLoss = (1.0m - stopLossPercentage) * price;
            recoveryDate = null;
        }

        public override void Sell(StockData stockData, bool low)
        {
            base.Sell(stockData, low);
            stopLoss = null;
            recoveryDate = stockData.Date.AddDays(recoveryDays);
        }

        public override void ProcessStockData(StockData stockData)
        {
            if (stopLoss.HasValue && stockData.Low <= stopLoss.Value)
            {
                Sell(stockData, true);
            }
            else if (recoveryDate.HasValue && stockData.Date >= recoveryDate.Value)
            {
                Buy(stockData);
            }
        }
    }
}
