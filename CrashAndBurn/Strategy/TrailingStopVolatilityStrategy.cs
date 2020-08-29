using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;

namespace CrashAndBurn.Strategy
{
    class TrailingStopVolatilityStrategy : BaseStrategy
    {
        private decimal _TrailingStopPercentage;
        private decimal _VolatilityPercentage;
        private decimal? _MaximumPrice;
        private decimal? _TrailingStop;

        private const int _StockDataBufferLimit = 5;
        private List<StockData> _StockDataBuffer = new List<StockData>();

        public TrailingStopVolatilityStrategy(decimal trailingStopPercentage, decimal volatilityPercentage)
            : base($"Trailing stop, volatility window ({trailingStopPercentage:P1} pullback, {volatilityPercentage:P0} volatility)")
        {
            _TrailingStopPercentage = trailingStopPercentage;
            _VolatilityPercentage = volatilityPercentage;
        }

        public override void Buy(StockData stockData)
        {
            base.Buy(stockData);
            decimal price = stockData.Open;
            _MaximumPrice = price;
            SetTrailingStop(price);
            _StockDataBuffer.Clear();
        }

        public override void Sell(StockData stockData, bool low)
        {
            base.Sell(stockData, low);
            _MaximumPrice = null;
            _TrailingStop = null;
            _StockDataBuffer.Clear();
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
            else if (!_TrailingStop.HasValue && stockData.Date.DayOfWeek == DayOfWeek.Monday && IsLowVolatility())
            {
                Buy(stockData);
            }
            else if (!_TrailingStop.HasValue)
            {
                _StockDataBuffer.Add(stockData);
                if (_StockDataBuffer.Count > _StockDataBufferLimit)
                {
                    _StockDataBuffer.Remove(_StockDataBuffer.First());
                }
            }
        }

        private void SetTrailingStop(decimal price)
        {
            _TrailingStop = (1.0m - _TrailingStopPercentage) * price;
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
