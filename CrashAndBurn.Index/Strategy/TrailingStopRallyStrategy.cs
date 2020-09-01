using CrashAndBurn.Common;
using System.Collections.Generic;
using System.Linq;

namespace CrashAndBurn.Strategy
{
    class TrailingStopRallyStrategy : TrailingStopStrategy
    {
        private const string _StrategyName = "Trailing stop, rally";

        public override string StrategyName => _StrategyName;

        private decimal _RallyPercentage;

        private const int _StockDataBufferLimit = 20;
        private List<StockData> _StockDataBuffer = new List<StockData>();

        public TrailingStopRallyStrategy(decimal trailingStopPercentage, decimal volatilityPercentage)
            : base($"{_StrategyName} ({trailingStopPercentage:P1} pullback, {volatilityPercentage:P0} rally)", trailingStopPercentage, null)
        {
            _RallyPercentage = volatilityPercentage;
        }

        public override void Buy(StockData stockData)
        {
            base.Buy(stockData);
            _StockDataBuffer.Clear();
        }

        public override void Sell(StockData stockData, bool low)
        {
            base.Sell(stockData, low);
            _StockDataBuffer.Clear();
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
                _StockDataBuffer.Add(stockData);
                if (_StockDataBuffer.Count > _StockDataBufferLimit)
                {
                    _StockDataBuffer.Remove(_StockDataBuffer.First());
                }
            }
        }

        private bool IsRally(StockData stockData)
        {
            if (_StockDataBuffer.Count < _StockDataBufferLimit)
            {
                return false;
            }
            decimal performance = stockData.Open / _StockDataBuffer.First().Open - 1.0m;
            bool isRally = performance > _RallyPercentage;
            return isRally;
        }
    }
}
