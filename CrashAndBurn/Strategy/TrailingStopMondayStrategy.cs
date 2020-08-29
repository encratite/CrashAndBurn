﻿using System;

namespace CrashAndBurn.Strategy
{
    class TrailingStopMondayStrategy : TrailingStopStrategy
    {
        public TrailingStopMondayStrategy(decimal trailingStopPercentage, int recoveryDays)
            : base($"Trailing stop, buy on Monday ({trailingStopPercentage:P1}, {recoveryDays} recovery days)", trailingStopPercentage, recoveryDays)
        {
        }

        public override void Buy(StockData stockData)
        {
            if (IsMonday(stockData))
            {
                base.Buy(stockData);
            }
        }

        public override void ProcessStockData(StockData stockData)
        {
            if (FirstPurchase)
            {
                if (IsMonday(stockData))
                {
                    Buy(stockData);
                }
            }
            else
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
                else if (RecoveryDate.HasValue && stockData.Date >= RecoveryDate.Value && IsMonday(stockData))
                {
                    Buy(stockData);
                }
            }
        }

        private bool IsMonday(StockData stockData)
        {
            return stockData.Date.DayOfWeek == DayOfWeek.Monday;
        }
    }
}
