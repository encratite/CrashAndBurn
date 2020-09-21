using CrashAndBurn.Common;

namespace CrashAndBurn.StopLoss.Strategy
{
    class BuyAndHoldStrategy : BaseStrategy
    {
        private const string _StrategyName = "Buy and hold";

        public override string StrategyName => _StrategyName;

        public BuyAndHoldStrategy()
            : base(_StrategyName)
        {
        }

        public override void ProcessStockData(StockData stockData)
        {
        }
    }
}
