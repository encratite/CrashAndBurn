using CrashAndBurn.Common;
using System.Collections.Generic;
using System.Linq;

namespace CrashAndBurn.StopLoss.Strategy
{
    class TrailingStopRallyStrategy : TrailingStopStrategy
    {
        private const string BaseStrategyName = "Trailing stop, rally";

        public override string StrategyName => BaseStrategyName;

        private decimal rallyPercentage;

        private const int stockDataBufferLimit = 20;
        private List<StockData> stockDataBuffer = new List<StockData>();

        public TrailingStopRallyStrategy(decimal trailingStopPercentage, decimal volatilityPercentage)
            : base($"{BaseStrategyName} ({trailingStopPercentage:P1} pullback, {volatilityPercentage:P0} rally)", trailingStopPercentage, null)
        {
            rallyPercentage = volatilityPercentage;
        }

        public override void Buy(StockData stockData)
        {
            base.Buy(stockData);
            stockDataBuffer.Clear();
        }

        public override void Sell(StockData stockData, bool low)
        {
            base.Sell(stockData, low);
            stockDataBuffer.Clear();
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
            else if (!TrailingStop.HasValue && IsRally(stockData))
            {
                Buy(stockData);
            }
            else if (!TrailingStop.HasValue)
            {
                stockDataBuffer.Add(stockData);
                if (stockDataBuffer.Count > stockDataBufferLimit)
                {
                    stockDataBuffer.Remove(stockDataBuffer.First());
                }
            }
        }

        private bool IsRally(StockData stockData)
        {
            if (stockDataBuffer.Count < stockDataBufferLimit)
            {
                return false;
            }
            decimal performance = stockData.Open / stockDataBuffer.First().Open - 1.0m;
            bool isRally = performance > rallyPercentage;
            return isRally;
        }
    }
}
