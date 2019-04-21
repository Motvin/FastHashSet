using System;
using System.Text;
using BenchmarkDotNet.Running;

namespace HashSetBench
{
	class Program
	{
		static void Main(string[] args)
		{
			
			var summary = BenchmarkRunner.Run<RefCountHashSetVFastHashSet>();
			//var summary = BenchmarkRunner.Run<SumSmallClassVsStructList>();
			//var summary2 = BenchmarkRunner.Run<AddSmallClassVsStructList>();
			//var summary = BenchmarkRunner.Run<AddSmallClassVsStructFast3>();
			//var summary = BenchmarkRunner.Run<AddSmallClassVsStructFast>();
			//var summary2 = BenchmarkRunner.Run<AddSmallClassVsStructFast2>();
			//var summary = BenchmarkRunner.Run<MinMaxIntRangeContains1to100>();
			//var summary2 = BenchmarkRunner.Run<MinMaxIntRangeContains100to1_000>();
			//var summary3 = BenchmarkRunner.Run<MinMaxIntRangeContains1_000to10_000>();
			//var summar4 = BenchmarkRunner.Run<MinMaxIntRangeContains10_000to100_000>();
			//var summar5 = BenchmarkRunner.Run<MinMaxIntRangeContains100_000to1_000_000>();

		}
	}
}
