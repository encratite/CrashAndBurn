namespace CrashAndBurn.Momentum
{
	class ParameterPair
	{
		public string Parameter { get; private set; }
		public decimal Cash { get; private set; }

		public ParameterPair(string parameter, decimal cash)
		{
			Parameter = parameter;
			Cash = cash;
		}
	}
}
