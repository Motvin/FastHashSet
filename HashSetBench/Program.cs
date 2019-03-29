using System;
using System.Text;
using BenchmarkDotNet.Running;

namespace HashSetBench
{
	class Program
	{
		static void Main(string[] args)
		{
			var summary = BenchmarkRunner.Run<MinMaxIntRangeContains10_000to100_000>();
		}
	}
}
