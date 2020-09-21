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

        private int _Shares;
        private decimal _OrderFees;
        private decimal _CapitalGainsTax;

        private decimal? _OriginalPrice;

        public BaseStrategy(string name)
        {
            Name = name;
        }

        public void Initialize(decimal initialCash, decimal orderFees, decimal capitalGainsTax)
        {
            Cash = initialCash;
            FirstPurchase = true;
            _Shares = 0;
            _OrderFees = orderFees;
            _CapitalGainsTax = capitalGainsTax;
        }

        public virtual void Buy(StockData stockData)
        {
            decimal price = stockData.Open;
            if (Cash >= 2 * _OrderFees + price)
            {
                Cash -= _OrderFees;
                _Shares = (int)Math.Floor((Cash - _OrderFees) / price);
                _OriginalPrice = _Shares * price;
                Cash -= _OriginalPrice.Value;
                FirstPurchase = false;
            }
        }

        public virtual void Sell(StockData stockData, bool low = false)
        {
            if (_Shares > 0 && Cash >= _OrderFees)
            {
                decimal price = low ? stockData.Low : stockData.Open;
                decimal value = _Shares * price;
                Cash += value - _OrderFees;
                if (value > _OriginalPrice.Value)
                {
                    Cash -= _CapitalGainsTax * (_OriginalPrice.Value - value);
                }
                _Shares = 0;
                _OriginalPrice = null;
            }
        }

        public abstract void ProcessStockData(StockData stockData);
    }
}
