using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace CrashAndBurn.Common
{
	public class JsonData
	{
		public List<DividendData> Dividends { get; private set; }
		public DateTime DateFirstAdded { get; private set; }

		public static JsonData Read(string path)
		{
			string json = File.ReadAllText(path);
			var jsonData = JsonConvert.DeserializeObject<JsonData>(json);
			return jsonData;
		}

		public static void Write(string path, JsonData jsonData)
		{
			string json = JsonConvert.SerializeObject(jsonData);
			File.WriteAllText(path, json);
		}

		public JsonData(List<DividendData> dividends, DateTime dateFirstAdded)
		{
			Dividends = dividends;
			DateFirstAdded = dateFirstAdded;
		}
	}
}
