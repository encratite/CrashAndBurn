using CrashAndBurn.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CrashAndBurn.StopLoss.Strategy
{
    class TrailingStopVolatilityStrategy : TrailingStopStrategy
    {
        private const string BaseStrategyName = "Trailing stop, volatility window";
        private const int StockDataBufferLimit = 20;

        public override string StrategyName => BaseStrategyName;

        private decimal _volatilityPercentage;

        private List<StockData> _stockDataBuffer = new List<StockData>();

        public TrailingStopVolatilityStrategy(decimal trailingStopPercentage, decimal volatilityPercentage)
            : base($"{BaseStrategyName} ({trailingStopPercentage:P1} pullback, {volatilityPercentage:P0} volatility)", trailingStopPercentage, null)
        {
            _volatilityPercentage = volatilityPercentage;
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
            else if (!TrailingStop.HasValue && IsLowVolatility())
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

        private bool IsLowVolatility()
        {
            if (_stockDataBuffer.Count < StockDataBufferLimit)
            {
                return false;
            }
            decimal? low = null;
            decimal? high = null;
            foreach (var stockData in _stockDataBuffer)
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
            bool isLowVolatility = volatility < _volatilityPercentage;
            return isLowVolatility;
        }
    }
}
