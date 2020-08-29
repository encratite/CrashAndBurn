using System;

namespace CrashAndBurn.Strategy
{
    abstract class BaseStrategy
    {
        public string Name { get; private set; }
        public decimal Cash { get; private set; }

        protected bool FirstPurchase { get; set; }

        private int _Shares;
        private decimal _OrderFees;

        public BaseStrategy(string name)
        {
            Name = name;
        }

        public void Initialize(decimal initialCash, decimal orderFees)
        {
            Cash = initialCash;
            FirstPurchase = true;
            _Shares = 0;
            _OrderFees = orderFees;
        }

        public virtual void Buy(StockData stockData)
        {
            decimal price = stockData.Open;
            if (Cash >= 2 * _OrderFees + price)
            {
                Cash -= _OrderFees;
                _Shares = (int)Math.Floor((Cash - _OrderFees) / price);
                Cash -= _Shares * price;
                FirstPurchase = false;
            }
        }

        public virtual void Sell(StockData stockData, bool low = false)
        {
            if (_Shares > 0 && Cash >= _OrderFees)
            {
                decimal price = low ? stockData.Low : stockData.Open;
                Cash += _Shares * price - _OrderFees;
                _Shares = 0;
            }
        }

        public abstract void ProcessStockData(StockData stockData);
    }
}
