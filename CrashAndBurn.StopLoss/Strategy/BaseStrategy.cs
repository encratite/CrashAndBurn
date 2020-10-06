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

        private int shares;
        private decimal orderFees;
        private decimal capitalGainsTax;

        private decimal? originalPrice;

        public BaseStrategy(string name)
        {
            Name = name;
        }

        public void Initialize(decimal initialCash, decimal orderFees, decimal capitalGainsTax)
        {
            Cash = initialCash;
            FirstPurchase = true;
            shares = 0;
            this.orderFees = orderFees;
            this.capitalGainsTax = capitalGainsTax;
        }

        public virtual void Buy(StockData stockData)
        {
            decimal price = stockData.Open;
            if (Cash >= 2 * orderFees + price)
            {
                Cash -= orderFees;
                shares = (int)Math.Floor((Cash - orderFees) / price);
                originalPrice = shares * price;
                Cash -= originalPrice.Value;
                FirstPurchase = false;
            }
        }

        public virtual void Sell(StockData stockData, bool low = false)
        {
            if (shares > 0 && Cash >= orderFees)
            {
                decimal price = low ? stockData.Low : stockData.Open;
                decimal value = shares * price;
                Cash += value - orderFees;
                if (value > originalPrice.Value)
                {
                    Cash -= capitalGainsTax * (originalPrice.Value - value);
                }
                shares = 0;
                originalPrice = null;
            }
        }

        public abstract void ProcessStockData(StockData stockData);
    }
}
