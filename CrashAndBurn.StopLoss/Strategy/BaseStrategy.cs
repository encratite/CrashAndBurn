using CrashAndBurn.Common;
using System;

namespace CrashAndBurn.StopLoss.Strategy
{
    abstract class BaseStrategy
    {
        public abstract string StrategyName { get; }

        public string Name { get; private set; }
        public decimal Cash { get; private set; }

        protected bool FirstPurchase { get; set; }

        private int _shares;
        private decimal _orderFees;
        private decimal _capitalGainsTax;

        private decimal? _originalPrice;

        public BaseStrategy(string name)
        {
            Name = name;
        }

        public void Initialize(decimal initialCash, decimal orderFees, decimal capitalGainsTax)
        {
            Cash = initialCash;
            FirstPurchase = true;
            _shares = 0;
            _orderFees = orderFees;
            _capitalGainsTax = capitalGainsTax;
        }

        public virtual void Buy(StockData stockData)
        {
            decimal price = stockData.Open;
            if (Cash >= 2 * _orderFees + price)
            {
                Cash -= _orderFees;
                _shares = (int)Math.Floor((Cash - _orderFees) / price);
                _originalPrice = _shares * price;
                Cash -= _originalPrice.Value;
                FirstPurchase = false;
            }
        }

        public virtual void Sell(StockData stockData, bool low = false)
        {
            if (_shares > 0 && Cash >= _orderFees)
            {
                decimal price = low ? stockData.Low : stockData.Open;
                decimal value = _shares * price;
                Cash += value - _orderFees;
                if (value > _originalPrice.Value)
                {
                    Cash -= _capitalGainsTax * (_originalPrice.Value - value);
                }
                _shares = 0;
                _originalPrice = null;
            }
        }

        public abstract void ProcessStockData(StockData stockData);
    }
}
