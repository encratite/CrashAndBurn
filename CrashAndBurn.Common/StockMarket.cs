using System;
using System.Collections.Generic;
using System.Linq;

namespace CrashAndBurn.Common
{
    public class StockMarket
    {
        private HashSet<Stock> _Stocks = new HashSet<Stock>();
        private List<Position> _Positions = new List<Position>();

        private decimal _Cash;
        private decimal _OrderFees;
        private decimal _CapitalGainsTax;

        private bool _IsMarginAccount;
        private decimal _InitialMargin;
        private decimal _MaintenanceMargin;
        private decimal _InitialMarginReserved;

        private bool _MarginCallSellAllPositions = true;
        private int _MarginCallCount;

        private decimal _Spread = 0.01m;

        private decimal _Gains = 0;
        private decimal _Losses = 0;


        public IReadOnlyCollection<Stock> Stocks
        {
            get => _Stocks;
        }

        public IReadOnlyCollection<Position> Positions
        {
            get => _Positions;
        }

        public DateTime Date { get; private set; }

        public StockMarket(IEnumerable<Stock> stocks)
        {
            foreach (var stock in stocks)
            {
                _Stocks.Add(stock);
            }
        }

        public void InitializeCashAccount(decimal cash, decimal orderFees, decimal capitalGainsTax, DateTime date)
        {
            _Cash = cash;
            _OrderFees = orderFees;
            _CapitalGainsTax = capitalGainsTax;

            _IsMarginAccount = false;
            _InitialMargin = 0.0m;
            _MaintenanceMargin = 0.0m;
            _InitialMarginReserved = 0.0m;
            _MarginCallCount = 0;

            Date = date;
        }

        public void InitializeMarginAccount(decimal cash, decimal orderFees, decimal capitalGainsTax, decimal initialMargin, decimal maintenanceMargin, DateTime date)
        {
            InitializeCashAccount(cash, orderFees, capitalGainsTax, date);

            _IsMarginAccount = true;
            _InitialMargin = initialMargin;
            _MaintenanceMargin = maintenanceMargin;
        }

        public void NextDay()
        {
            int lastMonth = Date.Month;
            while (Date.DayOfWeek == DayOfWeek.Saturday || Date.DayOfWeek == DayOfWeek.Sunday)
            {
                Date = Date.AddDays(1);
            }
            if (_IsMarginAccount && BelowMaintenanceMargin())
            {
                MarginCall();
            }
            if (Date.Month != lastMonth)
            {
                decimal minimum = Math.Min(_Gains, _Losses);
                decimal taxReturn = _CapitalGainsTax * minimum;
                _Cash += taxReturn;
                _Gains -= minimum;
                _Losses -= minimum;
            }
        }

        public Position Buy(Stock stock, int count)
        {
            decimal pricePerShare = GetPricePerShare(stock);
            if (_IsMarginAccount)
            {
                ReserveInitialMargin(count, pricePerShare);
            }
            else
            {
                decimal price = count * pricePerShare + _OrderFees;
                if (price > _Cash)
                {
                    throw new ApplicationException("Unable to buy shares, not enough funds.");
                }
                _Cash -= price;
            }
            var position = new Position(stock, count, pricePerShare, false);
            _Positions.Add(position);
            return position;
        }

        public Position Short(Stock stock, int count)
        {
            if (!_IsMarginAccount)
            {
                throw new ApplicationException("Shorting requires a margin account.");
            }
            decimal pricePerShare = GetPricePerShare(stock);
            ReserveInitialMargin(count, pricePerShare);
            var position = new Position(stock, count, pricePerShare, true);
            _Positions.Add(position);
            return position;
        }

        public void Sell(Position position)
        {
            if (_IsMarginAccount)
            {
                throw new NotImplementedException();
            }
            decimal currentPrice = position.Stock.GetPrice(Date);
            decimal priceDelta = currentPrice - position.OriginalPrice;
            decimal capitalGains = position.Count * priceDelta;
            if (position.IsShort)
            {
                capitalGains = -capitalGains;
                _Cash += capitalGains;
                BookCapitalGains(capitalGains);
            }
            else
            {
                _Cash += position.Count * currentPrice;
                BookCapitalGains(capitalGains);
            }
            decimal initialMargin = GetInitialMargin(position.Count, position.OriginalPrice);
            _InitialMargin -= initialMargin;
            _Positions.Remove(position);
        }

        private decimal GetInitialMargin(int count, decimal pricePerShare)
        {
            decimal initialMargin = _InitialMargin * count * pricePerShare;
            return initialMargin;
        }

        private void ReserveInitialMargin(int count, decimal pricePerShare)
        {
            decimal initialMargin = GetInitialMargin(count, pricePerShare);
            decimal cashRequired = _InitialMarginReserved + initialMargin + _OrderFees;
            if (cashRequired > _Cash)
            {
                throw new ApplicationException("Unable to perform transaction, required margin exceeds funds.");
            }
            _Cash -= _OrderFees;
            _InitialMarginReserved += initialMargin;
        }

        private void BookCapitalGains(decimal capitalGains)
        {
            if (capitalGains > 0)
            {
                _Cash -= _CapitalGainsTax * capitalGains;
                _Gains += capitalGains;
            }
            else
            {
                _Losses -= capitalGains;
            }
        }

        private decimal GetPricePerShare(Stock stock)
        {
            decimal pricePerStock = stock.GetPrice(Date) + _Spread;
            return pricePerStock;
        }

        private bool BelowMaintenanceMargin()
        {
            decimal equity = 0.0m;
            foreach (var position in _Positions)
            {
                decimal currentPrice = position.Stock.GetPrice(Date);
                decimal worth = position.Count * currentPrice;
            }
            throw new NotImplementedException();
        }

        private void MarginCall()
        {
            if (_MarginCallSellAllPositions)
            {
                foreach (var position in _Positions)
                {
                    Sell(position);
                }
            }
            else
            {
                while (BelowMaintenanceMargin() && _Positions.Any())
                {
                    var position = _Positions.First();
                    Sell(position);
                }
            }
            _MarginCallCount++;
        }
    }
}
