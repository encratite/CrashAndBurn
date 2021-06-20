using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace CrashAndBurn.Common
{
	public class DividendData
	{
		public DateTime Date { get; set; }
		public decimal Amount { get; set; }

		public DividendData(DateTime date, decimal amount)
		{
			Date = date;
			Amount = amount;
		}
	}
}
