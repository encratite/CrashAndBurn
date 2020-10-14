using CrashAndBurn.Common;
using System.Collections.Generic;
using System.Linq;

namespace CrashAndBurn.StopLoss.Strategy
{
    class TrailingStopRallyStrategy : TrailingStopStrategy
    {
        private const string BaseStrategyName = "Trailing stop, rally";

        public override string StrategyName => BaseStrategyName;

        private decimal _rallyPercentage;

        private const int StockDataBufferLimit = 20;
        private List<StockData> _stockDataBuffer = new List<StockData>();

        public TrailingStopRallyStrategy(decimal trailingStopPercentage, decimal volatilityPercentage)
            : base($"{BaseStrategyName} ({trailingStopPercentage:P1} pullback, {volatilityPercentage:P0} rally)", trailingStopPercentage, null)
        {
            _rallyPercentage = volatilityPercentage;
        }

        public override void Buy(StockData stockData)
        {
            base.Buy(stockData);
            _stockDataBuffer.Clear();
        }

        public override void Sell(StockData stockData, bool low)
        {
            base.Sell(stockData, low);
            _stockDataBuffer.Clear();
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
                _stockDataBuffer.Add(stockData);
                if (_stockDataBuffer.Count > StockDataBufferLimit)
                {
                    _stockDataBuffer.Remove(_stockDataBuffer.First());
                }
            }
        }

        private bool IsRally(StockData stockData)
        {
            if (_stockDataBuffer.Count < StockDataBufferLimit)
            {
                return false;
            }
            decimal performance = stockData.Open / _stockDataBuffer.First().Open - 1.0m;
            bool isRally = performance > _rallyPercentage;
            return isRally;
        }
    }
}
