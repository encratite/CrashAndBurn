using CrashAndBurn.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CrashAndBurn.Strategy
{
    class TrailingStopVolatilityStrategy : TrailingStopStrategy
    {
        private const string _StrategyName = "Trailing stop, volatility window";

        public override string StrategyName => _StrategyName;

        private decimal _VolatilityPercentage;

        private const int _StockDataBufferLimit = 20;
        private List<StockData> _StockDataBuffer = new List<StockData>();

        public TrailingStopVolatilityStrategy(decimal trailingStopPercentage, decimal volatilityPercentage)
            : base($"{_StrategyName} ({trailingStopPercentage:P1} pullback, {volatilityPercentage:P0} volatility)", trailingStopPercentage, null)
        {
            _VolatilityPercentage = volatilityPercentage;
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
            else if (!TrailingStop.HasValue && IsLowVolatility())
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

        private bool IsLowVolatility()
        {
            if (_StockDataBuffer.Count < _StockDataBufferLimit)
            {
                return false;
            }
            decimal? low = null;
            decimal? high = null;
            foreach (var stockData in _StockDataBuffer)
            {
                if (low.HasValue)
                {
                    low = Math.Min(low.Value, stockData.Low);
                }
                else
                {
                    low = stockData.Low;
                }
                if (high.HasValue)
                {
                    high = Math.Max(high.Value, stockData.High);
                }
                else
                {
                    high = stockData.High;
                }
            }
            decimal volatility = high.Value / low.Value - 1.0m;
            bool isLowVolatility = volatility < _VolatilityPercentage;
            return isLowVolatility;
        }
    }
}
