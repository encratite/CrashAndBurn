using CrashAndBurn.Common;

namespace CrashAndBurn.StopLoss.Strategy
{
    class BuyAndHoldStrategy : BaseStrategy
    {
        private const string BaseStrategyName = "Buy and hold";

        public override string StrategyName => BaseStrategyName;

        public BuyAndHoldStrategy()
            : base(BaseStrategyName)
        {
        }

        public override void ProcessStockData(StockData stockData)
        {
        }
    }
}
