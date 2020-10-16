using CrashAndBurn.Momentum.Strategy;
using System.Collections;
using System.Collections.Generic;

namespace CrashAndBurn.Momentum
{
	class StrategyClass : IEnumerable<ParameterPair>
	{
		private Dictionary<string, List<BaseStrategy>> _strategies = new Dictionary<string, List<BaseStrategy>>();

		public string Name { get; private set; }

		public StrategyClass(string name)
		{
			Name = name;
		}

		public void Add(string parameter, BaseStrategy strategy)
		{
			List<BaseStrategy> strategies;
			if (!_strategies.TryGetValue(parameter, out strategies))
			{
				strategies = new List<BaseStrategy>();
				_strategies[parameter] = strategies;
			}
			strategies.Add(strategy);
		}

		public IEnumerator<ParameterPair> GetEnumerator()
		{
			var parameterPairs = new List<ParameterPair>();
			foreach (var pair in _strategies)
			{
				string parameter = pair.Key;
				var strategies = pair.Value;
				decimal cashSum = 0.0m;
				foreach (var strategy in strategies)
					cashSum += strategy.Cash.Value;
				decimal cash = cashSum / strategies.Count;
				var parameterPair = new ParameterPair(parameter, cash);
				parameterPairs.Add(parameterPair);
			}
			parameterPairs.Sort((x, y) => -x.Cash.CompareTo(y.Cash));
			return parameterPairs.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
