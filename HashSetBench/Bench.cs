using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Diagnosers;
using SCG = System.Collections.Generic;
using Motvin.Collections;
//using C5;

namespace HashSetBench
{
	//[ClrJob(baseline: true)] //, CoreJob, MonoJob, CoreRtJob
	//[RPlotExporter]
	[MinColumn, MaxColumn] //RankColumn
	[HardwareCounters(HardwareCounter.CacheMisses)]
	[RyuJitX64Job] //LegacyJitX86Job
	//[MemoryDiagnoser]
	public class MinMaxIntRangeFast
	{
		public int[] intArray;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			intArray = new int[N];

			//Random rand = new Random(42);
			//Random rand = new Random(142);
			Random rand = new Random(89);
			for (int i = 0; i < intArray.Length; i++)
			{
				intArray[i] = rand.Next(int.MinValue, int.MaxValue);
				//intArray[i] = rand.Next(0, int.MaxValue);
				//intArray[i] = rand.Next(int.MinValue, -1);
			}
		}

		[Benchmark(Baseline = true)]
		public void Test_Add()
		{
			// use this code to test for different versions of Add to see which one is faster
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		//[Benchmark]
		//public void Test_Add2()
		//{
		//	FastHashSet2<int> set = new FastHashSet2<int>();
		//	for (int i = 0; i < intArray.Length; i++)
		//	{
		//		set.Add(intArray[i]);
		//	}
		//}
	}

	[RyuJitX64Job]
	//[OpenXmlExporter]
	public class MinMaxIntRangeContains
	{
		public int[] a;
		public int[] c;

		public SCG.HashSet<int> h;
		public C5.HashSet<int> c5;
		public FastHashSet<int> f;
		public SCG.List<int> lst;
		public SCG.SortedSet<int> sortedSet;
		

	[Params(1,			2,			3,			4,			5,			6,			7,			8,			9,
			10,			20,			30,			40,			50,			60,			70,			80,			90,
			100,		200,		300,		400,		500,		600,		700,		800,		900,
			1000,		2000,		3000,		4000,		5000,		6000,		7000,		8000,		9000,
			10000,		20000,		30000,		40000,		50000,		60000,		70000,		80000,		90000,
			100000,		200000,		300000,		400000,		500000,		600000,		700000,		800000,		900000,
			1000000,	2000000,	3000000,	4000000,	5000000,	6000000,	7000000,	8000000,	9000000,
			10000000,	20000000,	30_000_000,	40_000_000 )]
			
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new int[N];
			c = new int[N];

			Random rand = new Random(89);
			BenchUtil.PopulateIntArray(a, rand, int.MinValue, int.MaxValue, 1.0);
			BenchUtil.PopulateIntArrayFromUniqueArray(c, rand, a, N / 2);

			h = new SCG.HashSet<int>();
			for (int i = 0; i < N; i++)
			{
				h.Add(a[i]);
			}

			f = new FastHashSet<int>();
			for (int i = 0; i < N; i++)
			{
				f.Add(a[i]);
			}

			c5 = new C5.HashSet<int>();
			for (int i = 0; i < N; i++)
			{
				c5.Add(a[i]);
			}

			lst = new List<int>();
			for (int i = 0; i < N; i++)
			{
				lst.Add(a[i]);
			}
			lst.Sort();

			sortedSet = new SCG.SortedSet<int>();
			for (int i = 0; i < N; i++)
			{
				sortedSet.Add(a[i]);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG_Contains()
		{
			for (int i = 0; i < c.Length; i++)
			{
				h.Contains(c[i]);
			}
		}

		[Benchmark]
		public void C5_Contains()
		{
			for (int i = 0; i < c.Length; i++)
			{
				c5.Contains(c[i]);
			}
		}

		[Benchmark]
		public void List_Binary_Search()
		{
			for (int i = 0; i < c.Length; i++)
			{
				lst.BinarySearch(c[i]);
			}
		}

		[Benchmark]
		public void SortedSet_Contains()
		{
			for (int i = 0; i < c.Length; i++)
			{
				sortedSet.Contains(c[i]);
			}
		}
		
		[Benchmark]
		public void Fast_Contains()
		{
			for (int i = 0; i < c.Length; i++)
			{
				f.Contains(c[i]);
			}
		}
	}

	[RyuJitX64Job]
	//[OpenXmlExporter]
	public class MinMaxIntRangeContains1to100
	{
		public int[] a;
		public int[] c;

		public SCG.HashSet<int> h = new HashSet<int>();
		public FastHashSet<int> f = new FastHashSet<int>();
		public C5.HashSet<int> c5 = new C5.HashSet<int>();

	[Params(1,			2,			3,			4,			5,			6,			7,			8,			9,			10,
			11,			12,			13,			14,			15,			16,			17,			18,			19,			20,
			21,			22,			23,			24,			25,			26,			27,			28,			29,			30,
			31,			32,			33,			34,			35,			36,			37,			38,			39,			40,
			41,			42,			43,			44,			45,			46,			47,			48,			49,			50,
			51,			52,			53,			54,			55,			56,			57,			58,			59,			60,
			61,			62,			63,			64,			65,			66,			67,			68,			69,			70,
			71,			72,			73,			74,			75,			76,			77,			78,			79,			80,
			81,			82,			83,			84,			85,			86,			87,			88,			89,			90,
			91,			92,			93,			94,			95,			96,			97,			98,			99,			maxN
		)]
			
		public int N;
		public const int maxN = 100;

		public MinMaxIntRangeContains1to100()
		{
			BenchUtil.PopulateCollections25_25_50PctUnique(maxN, out a, out c, h, f, c5);
			//BenchUtil.PopulateCollections50PctUnique(maxN, out a, out c, h, f, c5, sortedSet, lst);
		}
		

		[Benchmark(Baseline = true)]
		public void SCG_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				h.Contains(c[i]);
			}
		}
		
		[Benchmark]
		public void Fast_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				f.Contains(c[i]);
			}
		}

		[Benchmark]
		public void C5_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				c5.Contains(c[i]);
			}
		}

		//[Benchmark]
		//public void List_Binary_Search()
		//{
		//	for (int i = 0; i < c.Length; i++)
		//	{
		//		lst.BinarySearch(c[i]);
		//	}
		//}

		//[Benchmark]
		//public void SortedSet_Contains()
		//{
		//	for (int i = 0; i < c.Length; i++)
		//	{
		//		sortedSet.Contains(c[i]);
		//	}
		//}
	}


	[RyuJitX64Job]
	//[OpenXmlExporter]
	//[MemoryDiagnoser]
	//[MinColumn]
	//[MaxColumn]
	public class MinMaxIntRangeContains1000
	{
		public int[] a; // contains unique integers (100% unique)
		public int[] c; // contains 50% unique values

		public SCG.HashSet<int> h = new HashSet<int>();
		public FastHashSet<int> f = new FastHashSet<int>();
		public C5.HashSet<int> c5 = new C5.HashSet<int>();
		//public SCG.List<int> lst = new List<int>();
		//public SCG.SortedSet<int> sortedSet = new SortedSet<int>();

		[Params(1000,
				1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000,
				2100, 2200, 2300, 2400, 2500, 2600, 2700, 2800, 2900, 3000,
				3100, 3200, 3300, 3400, 3500, 3600, 3700, 3800, 3900, 4000,
				4100, 4200, 4300, 4400, 4500, 4600, 4700, 4800, 4900, 5000,
				5100, 5200, 5300, 5400, 5500, 5600, 5700, 5800, 5900, 6000,
				6100, 6200, 6300, 6400, 6500, 6600, 6700, 6800, 6900, 7000,
				7100, 7200, 7300, 7400, 7500, 7600, 7700, 7800, 7900, 8000,
				8100, 8200, 8300, 8400, 8500, 8600, 8700, 8800, 8900, 9000,
				9100, 9200, 9300, 9400, 9500, 9600, 9700, 9800, 9900, maxN
			)]

		//[Params(
		//	maxN,		9900,		9800,		9700,		9600,		9500,		9400,		9300,		9200,		9100,
		//	9000,		8900,		8800,		8700,		8600,		8500,		8400,		8300,		8200,		8100,
		//	8000,		7900,		7800,		7700,		7600,		7500,		7400,		7300,		8200,		7100,
		//	7000,		6900,		6800,		6700,		6600,		6500,		6400,		6300,		6200,		6100,
		//	6000,		5900,		5800,		5700,		5600,		5500,		5400,		5300,		5200,		5100,
		//	5000,		4900,		4800,		4700,		4600,		4500,		4400,		4300,		4200,		4100,
		//	4000,		3900,		3800,		3700,		3600,		3500,		3400,		3300,		3200,		3100,
		//	3000,		2900,		2800,		2700,		2600,		2500,		2400,		2300,		2200,		2100,
		//	2000,		1900,		1800,		1700,		1600,		1500,		1400,		1300,		1200,		1100,
		//	1000
		//	)]

		public int N;
		public const int maxN = 10_000;

		public MinMaxIntRangeContains1000()
		{
			BenchUtil.PopulateCollections25_25_50PctUnique(maxN, out a, out c, h, f, c5);
			//BenchUtil.PopulateCollections50PctUnique(maxN, out a, out c, h, f, c5, sortedSet, lst);
		}
		
		//[IterationSetup]
		//public void IterationSetup()
		//{
		//	BenchUtil.ClearCpuCaches();
		//}

		[Benchmark(Baseline = true)]
		public void SCG_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				h.Contains(c[i]);
			}
		}
		
		[Benchmark]
		public void Fast_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				f.Contains(c[i]);
			}
		}

		[Benchmark]
		public void C5_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				c5.Contains(c[i]);
			}
		}

		//[Benchmark]
		//public void SortedSet_Contains()
		//{
		//	for (int i = 0; i < N; i++)
		//	{
		//		sortedSet.Contains(c[i]);
		//	}
		//}

		//[Benchmark]
		//public void List_Binary_Search()
		//{
		//	for (int i = 0; i < N; i++)
		//	{
		//		lst.BinarySearch(c[i]);
		//	}
		//}
	}

	[RyuJitX64Job]
	//[OpenXmlExporter]
	public class MinMaxIntRangeContains100to1_000
	{
		public int[] a;
		public int[] c;

		public SCG.HashSet<int> h = new HashSet<int>();
		public FastHashSet<int> f = new FastHashSet<int>();
		public C5.HashSet<int> c5 = new C5.HashSet<int>();
		//public SCG.List<int> lst;
		//public SCG.SortedSet<int> sortedSet;
		

	[Params(100,
			110,			120,			130,			140,			150,			160,			170,			180,			190,			200,
			210,			220,			230,			240,			250,			260,			270,			280,			290,			300,
			310,			320,			330,			340,			350,			360,			370,			380,			390,			400,
			410,			420,			430,			440,			450,			460,			470,			480,			490,			500,
			510,			520,			530,			540,			550,			560,			570,			580,			590,			600,
			610,			620,			630,			640,			650,			660,			670,			680,			690,			700,
			710,			720,			730,			740,			750,			760,			770,			780,			790,			800,
			810,			820,			830,			840,			850,			860,			870,			880,			890,			900,
			910,			920,			930,			940,			950,			960,			970,			980,			990,			maxN
		)]
			
		public int N;
		public const int maxN = 1_000;

		public MinMaxIntRangeContains100to1_000()
		{
			BenchUtil.PopulateCollections25_25_50PctUnique(maxN, out a, out c, h, f, c5);
			//BenchUtil.PopulateCollections50PctUnique(maxN, out a, out c, h, f, c5, sortedSet, lst);
		}

		[Benchmark(Baseline = true)]
		public void SCG_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				h.Contains(c[i]);
			}
		}

		[Benchmark]
		public void Fast_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				f.Contains(c[i]);
			}
		}

		[Benchmark]
		public void C5_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				c5.Contains(c[i]);
			}
		}

		//[Benchmark]
		//public void List_Binary_Search()
		//{
		//	for (int i = 0; i < N; i++)
		//	{
		//		lst.BinarySearch(c[i]);
		//	}
		//}

		//[Benchmark]
		//public void SortedSet_Contains()
		//{
		//	for (int i = 0; i < N; i++)
		//	{
		//		sortedSet.Contains(c[i]);
		//	}
		//}
	}

	[RyuJitX64Job]
	//[OpenXmlExporter]
	public class MinMaxIntRangeContains1_000to10_000
	{
		public int[] a;
		public int[] c;

		public SCG.HashSet<int> h = new HashSet<int>();
		public FastHashSet<int> f = new FastHashSet<int>();
		public C5.HashSet<int> c5 = new C5.HashSet<int>();
		

	[Params(1000,
			1100,			1200,			1300,			1400,			1500,			1600,			1700,			1800,			1900,			2000,
			2100,			2200,			2300,			2400,			2500,			2600,			2700,			2800,			2900,			3000,
			3100,			3200,			3300,			3400,			3500,			3600,			3700,			3800,			3900,			4000,
			4100,			4200,			4300,			4400,			4500,			4600,			4700,			4800,			4900,			5000,
			5100,			5200,			5300,			5400,			5500,			5600,			5700,			5800,			5900,			6000,
			6100,			6200,			6300,			6400,			6500,			6600,			6700,			6800,			6900,			7000,
			7100,			7200,			7300,			7400,			7500,			7600,			7700,			7800,			7900,			8000,
			8100,			8200,			8300,			8400,			8500,			8600,			8700,			8800,			8900,			9000,
			9100,			9200,			9300,			9400,			9500,			9600,			9700,			9800,			9900,			maxN
		)]
			
		public int N;
		public const int maxN = 10_000;

		public MinMaxIntRangeContains1_000to10_000()
		{
			BenchUtil.PopulateCollections25_25_50PctUnique(maxN, out a, out c, h, f, c5);
			//BenchUtil.PopulateCollections50PctUnique(maxN, out a, out c, h, f, c5, sortedSet, lst);
		}

		[Benchmark(Baseline = true)]
		public void SCG_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				h.Contains(c[i]);
			}
		}

		[Benchmark]
		public void Fast_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				f.Contains(c[i]);
			}
		}

		[Benchmark]
		public void C5_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				c5.Contains(c[i]);
			}
		}

		//[Benchmark]
		//public void List_Binary_Search()
		//{
		//	for (int i = 0; i < N; i++)
		//	{
		//		lst.BinarySearch(c[i]);
		//	}
		//}

		//[Benchmark]
		//public void SortedSet_Contains()
		//{
		//	for (int i = 0; i < N; i++)
		//	{
		//		sortedSet.Contains(c[i]);
		//	}
		//}
	}

	[RyuJitX64Job]
	[ClrJob, CoreJob]
	//[OpenXmlExporter]
	public class MinMaxIntRangeContains10_000to100_000
	{
		public int[] a;
		public int[] c;

		public SCG.HashSet<int> h = new HashSet<int>();
		public FastHashSet<int> f = new FastHashSet<int>();
		public C5.HashSet<int> c5 = new C5.HashSet<int>();

	[Params(10000,
			11000,			12000,			13000,			14000,			15000,			16000,			17000,			18000,			19000,			20000,
			21000,			22000,			23000,			24000,			25000,			26000,			27000,			28000,			29000,			30000,
			31000,			32000,			33000,			34000,			35000,			36000,			37000,			38000,			39000,			40000,
			41000,			42000,			43000,			44000,			45000,			46000,			47000,			48000,			49000,			50000,
			51000,			52000,			53000,			54000,			55000,			56000,			57000,			58000,			59000,			60000,
			61000,			62000,			63000,			64000,			65000,			66000,			67000,			68000,			69000,			70000,
			71000,			72000,			73000,			74000,			75000,			76000,			77000,			78000,			79000,			80000,
			81000,			82000,			83000,			84000,			85000,			86000,			87000,			88000,			89000,			90000,
			91000,			92000,			93000,			94000,			95000,			96000,			97000,			98000,			99000,			maxN
		)]
			
		public int N;
		public const int maxN = 100_000;

		public MinMaxIntRangeContains10_000to100_000()
		{
			BenchUtil.PopulateCollections25_25_50PctUnique(maxN, out a, out c, h, f, c5);
			//BenchUtil.PopulateCollections50PctUnique(maxN, out a, out c, h, f, c5, sortedSet, lst);
		}

		[Benchmark(Baseline = true)]
		public void SCG_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				h.Contains(c[i]);
			}
		}
		
		[Benchmark]
		public void Fast_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				f.Contains(c[i]);
			}
		}

		[Benchmark]
		public void C5_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				c5.Contains(c[i]);
			}
		}

		//[Benchmark]
		//public void List_Binary_Search()
		//{
		//	for (int i = 0; i < N; i++)
		//	{
		//		lst.BinarySearch(c[i]);
		//	}
		//}

		//[Benchmark]
		//public void SortedSet_Contains()
		//{
		//	for (int i = 0; i < N; i++)
		//	{
		//		sortedSet.Contains(c[i]);
		//	}
		//}
	}

	[RyuJitX64Job]
	//[OpenXmlExporter]
	public class MinMaxIntRangeContains100_000to1_000_000
	{
		public int[] a;
		public int[] c;

		public SCG.HashSet<int> h = new HashSet<int>();
		public FastHashSet<int> f = new FastHashSet<int>();
		public C5.HashSet<int> c5 = new C5.HashSet<int>();
		

	[Params(100000,
			110000,			120000,			130000,			140000,			150000,			160000,			170000,			180000,			190000,			200000,
			210000,			220000,			230000,			240000,			250000,			260000,			270000,			280000,			290000,			300000,
			310000,			320000,			330000,			340000,			350000,			360000,			370000,			380000,			390000,			400000,
			410000,			420000,			430000,			440000,			450000,			460000,			470000,			480000,			490000,			500000,
			510000,			520000,			530000,			540000,			550000,			560000,			570000,			580000,			590000,			600000,
			610000,			620000,			630000,			640000,			650000,			660000,			670000,			680000,			690000,			700000,
			710000,			720000,			730000,			740000,			750000,			760000,			770000,			780000,			790000,			800000,
			810000,			820000,			830000,			840000,			850000,			860000,			870000,			880000,			890000,			900000,
			910000,			920000,			930000,			940000,			950000,			960000,			970000,			980000,			990000,			maxN
		)]
			
		public int N;
		public const int maxN = 1_000_000;

		public MinMaxIntRangeContains100_000to1_000_000()
		{
			BenchUtil.PopulateCollections25_25_50PctUnique(maxN, out a, out c, h, f, c5);
			//BenchUtil.PopulateCollections50PctUnique(maxN, out a, out c, h, f, c5, sortedSet, lst);
		}

		[Benchmark(Baseline = true)]
		public void SCG_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				h.Contains(c[i]);
			}
		}

		[Benchmark]
		public void Fast_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				f.Contains(c[i]);
			}
		}

		[Benchmark]
		public void C5_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				c5.Contains(c[i]);
			}
		}

		//[Benchmark]
		//public void List_Binary_Search()
		//{
		//	for (int i = 0; i < N; i++)
		//	{
		//		lst.BinarySearch(c[i]);
		//	}
		//}

		//[Benchmark]
		//public void SortedSet_Contains()
		//{
		//	for (int i = 0; i < N; i++)
		//	{
		//		sortedSet.Contains(c[i]);
		//	}
		//}
	}

	[RyuJitX64Job]
	//[OpenXmlExporter]
	public class MinMaxIntRangeContains1_000_000to10_000_000
	{
		public int[] a;
		public int[] c;

		public SCG.HashSet<int> h = new HashSet<int>();
		public FastHashSet<int> f = new FastHashSet<int>();
		public C5.HashSet<int> c5 = new C5.HashSet<int>();
		

	[Params(1000000,
			1100000,			1200000,			1300000,			1400000,			1500000,			1600000,			1700000,			1800000,			1900000,			2000000,
			2100000,			2200000,			2300000,			2400000,			2500000,			2600000,			2700000,			2800000,			2900000,			3000000,
			3100000,			3200000,			3300000,			3400000,			3500000,			3600000,			3700000,			3800000,			3900000,			4000000,
			4100000,			4200000,			4300000,			4400000,			4500000,			4600000,			4700000,			4800000,			4900000,			5000000,
			5100000,			5200000,			5300000,			5400000,			5500000,			5600000,			5700000,			5800000,			5900000,			6000000,
			6100000,			6200000,			6300000,			6400000,			6500000,			6600000,			6700000,			6800000,			6900000,			7000000,
			7100000,			7200000,			7300000,			7400000,			7500000,			7600000,			7700000,			7800000,			7900000,			8000000,
			8100000,			8200000,			8300000,			8400000,			8500000,			8600000,			8700000,			8800000,			8900000,			9000000,
			9100000,			9200000,			9300000,			9400000,			9500000,			9600000,			9700000,			9800000,			9900000,			maxN
		)]
			
		public int N;
		public const int maxN = 10_000_000;

		public MinMaxIntRangeContains1_000_000to10_000_000()
		{
			BenchUtil.PopulateCollections25_25_50PctUnique(maxN, out a, out c, h, f, c5);
			//BenchUtil.PopulateCollections50PctUnique(maxN, out a, out c, h, f, c5, sortedSet, lst);
		}

		[Benchmark(Baseline = true)]
		public void SCG_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				h.Contains(c[i]);
			}
		}

		[Benchmark]
		public void Fast_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				f.Contains(c[i]);
			}
		}

		[Benchmark]
		public void C5_Contains()
		{
			for (int i = 0; i < N; i++)
			{
				c5.Contains(c[i]);
			}
		}

		//[Benchmark]
		//public void List_Binary_Search()
		//{
		//	for (int i = 0; i < N; i++)
		//	{
		//		lst.BinarySearch(c[i]);
		//	}
		//}

		//[Benchmark]
		//public void SortedSet_Contains()
		//{
		//	for (int i = 0; i < N; i++)
		//	{
		//		sortedSet.Contains(c[i]);
		//	}
		//}
	}

	[RyuJitX64Job]
	//[OpenXmlExporter]
	public class MinMaxIntRange1to100
	{
		public int[] intArray;

	[Params(1,			2,			3,			4,			5,			6,			7,			8,			9,			10,
			11,			12,			13,			14,			15,			16,			17,			18,			19,			20,
			21,			22,			23,			24,			25,			26,			27,			28,			29,			30,
			31,			32,			33,			34,			35,			36,			37,			38,			39,			40,
			41,			42,			43,			44,			45,			46,			47,			48,			49,			50,
			51,			52,			53,			54,			55,			56,			57,			58,			59,			60,
			61,			62,			63,			64,			65,			66,			67,			68,			69,			70,
			71,			72,			73,			74,			75,			76,			77,			78,			79,			80,
			81,			82,			83,			84,			85,			86,			87,			88,			89,			90,
			91,			92,			93,			94,			95,			96,			97,			98,			99,			100
		)]

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			intArray = new int[N];

			Random rand = new Random(89);
			for (int i = 0; i < intArray.Length; i++)
			{
				intArray[i] = rand.Next(int.MinValue, int.MaxValue);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void C5_HashSet_Add()
		{
			C5.HashSet<int> set = new C5.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void List_Add()
		{
			// if there are duplicate values in the array, then this will add the duplicates, so this isn't really an apples to apples comparison vs. sets, which will only have added a single value
			SCG.List<int> lst = new SCG.List<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				lst.Add(intArray[i]);
			}
			lst.Sort();
		}

		[Benchmark]
		public void SortedSet_Add()
		{
			SCG.SortedSet<int> set = new SCG.SortedSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}
	}

	[RyuJitX64Job]
	//[OpenXmlExporter]
	public class MinMaxIntRange100to1_000
	{
		public int[] intArray;

	[Params(100,
			110,			120,			130,			140,			150,			160,			170,			180,			190,			200,
			210,			220,			230,			240,			250,			260,			270,			280,			290,			300,
			310,			320,			330,			340,			350,			360,			370,			380,			390,			400,
			410,			420,			430,			440,			450,			460,			470,			480,			490,			500,
			510,			520,			530,			540,			550,			560,			570,			580,			590,			600,
			610,			620,			630,			640,			650,			660,			670,			680,			690,			700,
			710,			720,			730,			740,			750,			760,			770,			780,			790,			800,
			810,			820,			830,			840,			850,			860,			870,			880,			890,			900,
			910,			920,			930,			940,			950,			960,			970,			980,			990,			1000
		)]

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			intArray = new int[N];

			Random rand = new Random(89);
			for (int i = 0; i < intArray.Length; i++)
			{
				intArray[i] = rand.Next(int.MinValue, int.MaxValue);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void C5_HashSet_Add()
		{
			C5.HashSet<int> set = new C5.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void List_Add()
		{
			// if there are duplicate values in the array, then this will add the duplicates, so this isn't really an apples to apples comparison vs. sets, which will only have added a single value
			SCG.List<int> lst = new SCG.List<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				lst.Add(intArray[i]);
			}
			lst.Sort();
		}

		[Benchmark]
		public void SortedSet_Add()
		{
			SCG.SortedSet<int> set = new SCG.SortedSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}
	}

	[RyuJitX64Job]
	//[OpenXmlExporter]
	public class MinMaxIntRange1_000to10_000
	{
		public int[] intArray;

	[Params(1000,
			1100,			1200,			1300,			1400,			1500,			1600,			1700,			1800,			1900,			2000,
			2100,			2200,			2300,			2400,			2500,			2600,			2700,			2800,			2900,			3000,
			3100,			3200,			3300,			3400,			3500,			3600,			3700,			3800,			3900,			4000,
			4100,			4200,			4300,			4400,			4500,			4600,			4700,			4800,			4900,			5000,
			5100,			5200,			5300,			5400,			5500,			5600,			5700,			5800,			5900,			6000,
			6100,			6200,			6300,			6400,			6500,			6600,			6700,			6800,			6900,			7000,
			7100,			7200,			7300,			7400,			7500,			7600,			7700,			7800,			7900,			8000,
			8100,			8200,			8300,			8400,			8500,			8600,			8700,			8800,			8900,			9000,
			9100,			9200,			9300,			9400,			9500,			9600,			9700,			9800,			9900,			10000
		)]

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			intArray = new int[N];

			Random rand = new Random(89);
			for (int i = 0; i < intArray.Length; i++)
			{
				intArray[i] = rand.Next(int.MinValue, int.MaxValue);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void C5_HashSet_Add()
		{
			C5.HashSet<int> set = new C5.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void List_Add()
		{
			// if there are duplicate values in the array, then this will add the duplicates, so this isn't really an apples to apples comparison vs. sets, which will only have added a single value
			SCG.List<int> lst = new SCG.List<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				lst.Add(intArray[i]);
			}
			lst.Sort();
		}

		[Benchmark]
		public void SortedSet_Add()
		{
			SCG.SortedSet<int> set = new SCG.SortedSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}
	}

	[RyuJitX64Job]
	//[OpenXmlExporter]
	public class MinMaxIntRange10_000to100_000
	{
		public int[] intArray;

	[Params(10000,
			11000,			12000,			13000,			14000,			15000,			16000,			17000,			18000,			19000,			20000,
			21000,			22000,			23000,			24000,			25000,			26000,			27000,			28000,			29000,			30000,
			31000,			32000,			33000,			34000,			35000,			36000,			37000,			38000,			39000,			40000,
			41000,			42000,			43000,			44000,			45000,			46000,			47000,			48000,			49000,			50000,
			51000,			52000,			53000,			54000,			55000,			56000,			57000,			58000,			59000,			60000,
			61000,			62000,			63000,			64000,			65000,			66000,			67000,			68000,			69000,			70000,
			71000,			72000,			73000,			74000,			75000,			76000,			77000,			78000,			79000,			80000,
			81000,			82000,			83000,			84000,			85000,			86000,			87000,			88000,			89000,			90000,
			91000,			92000,			93000,			94000,			95000,			96000,			97000,			98000,			99000,			100000
		)]

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			intArray = new int[N];

			Random rand = new Random(89);
			for (int i = 0; i < intArray.Length; i++)
			{
				intArray[i] = rand.Next(int.MinValue, int.MaxValue);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void C5_HashSet_Add()
		{
			C5.HashSet<int> set = new C5.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void List_Add()
		{
			// if there are duplicate values in the array, then this will add the duplicates, so this isn't really an apples to apples comparison vs. sets, which will only have added a single value
			SCG.List<int> lst = new SCG.List<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				lst.Add(intArray[i]);
			}
			lst.Sort();
		}

		[Benchmark]
		public void SortedSet_Add()
		{
			SCG.SortedSet<int> set = new SCG.SortedSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}
	}

	[RyuJitX64Job]
	//[OpenXmlExporter]
	public class MinMaxIntRange100_000to1_000_000
	{
		public int[] intArray;

	[Params(100000,
			110000,			120000,			130000,			140000,			150000,			160000,			170000,			180000,			190000,			200000,
			210000,			220000,			230000,			240000,			250000,			260000,			270000,			280000,			290000,			300000,
			310000,			320000,			330000,			340000,			350000,			360000,			370000,			380000,			390000,			400000,
			410000,			420000,			430000,			440000,			450000,			460000,			470000,			480000,			490000,			500000,
			510000,			520000,			530000,			540000,			550000,			560000,			570000,			580000,			590000,			600000,
			610000,			620000,			630000,			640000,			650000,			660000,			670000,			680000,			690000,			700000,
			710000,			720000,			730000,			740000,			750000,			760000,			770000,			780000,			790000,			800000,
			810000,			820000,			830000,			840000,			850000,			860000,			870000,			880000,			890000,			900000,
			910000,			920000,			930000,			940000,			950000,			960000,			970000,			980000,			990000,			1000000
		)]

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			intArray = new int[N];

			Random rand = new Random(89);
			for (int i = 0; i < intArray.Length; i++)
			{
				intArray[i] = rand.Next(int.MinValue, int.MaxValue);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void C5_HashSet_Add()
		{
			C5.HashSet<int> set = new C5.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void List_Add()
		{
			// if there are duplicate values in the array, then this will add the duplicates, so this isn't really an apples to apples comparison vs. sets, which will only have added a single value
			SCG.List<int> lst = new SCG.List<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				lst.Add(intArray[i]);
			}
			lst.Sort();
		}

		[Benchmark]
		public void SortedSet_Add()
		{
			SCG.SortedSet<int> set = new SCG.SortedSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}
	}

	[RyuJitX64Job]
	//[OpenXmlExporter]
	public class MinMaxIntRange1_000_000to10_000_000
	{
		public int[] intArray;

	[Params(1000000,
			1100000,			1200000,			1300000,			1400000,			1500000,			1600000,			1700000,			1800000,			1900000,			2000000,
			2100000,			2200000,			2300000,			2400000,			2500000,			2600000,			2700000,			2800000,			2900000,			3000000,
			3100000,			3200000,			3300000,			3400000,			3500000,			3600000,			3700000,			3800000,			3900000,			4000000,
			4100000,			4200000,			4300000,			4400000,			4500000,			4600000,			4700000,			4800000,			4900000,			5000000,
			5100000,			5200000,			5300000,			5400000,			5500000,			5600000,			5700000,			5800000,			5900000,			6000000,
			6100000,			6200000,			6300000,			6400000,			6500000,			6600000,			6700000,			6800000,			6900000,			7000000,
			7100000,			7200000,			7300000,			7400000,			7500000,			7600000,			7700000,			7800000,			7900000,			8000000,
			8100000,			8200000,			8300000,			8400000,			8500000,			8600000,			8700000,			8800000,			8900000,			9000000,
			9100000,			9200000,			9300000,			9400000,			9500000,			9600000,			9700000,			9800000,			9900000,			10_000_000
		)]

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			intArray = new int[N];

			Random rand = new Random(89);
			for (int i = 0; i < intArray.Length; i++)
			{
				intArray[i] = rand.Next(int.MinValue, int.MaxValue);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void C5_HashSet_Add()
		{
			C5.HashSet<int> set = new C5.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void List_Add()
		{
			// if there are duplicate values in the array, then this will add the duplicates, so this isn't really an apples to apples comparison vs. sets, which will only have added a single value
			SCG.List<int> lst = new SCG.List<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				lst.Add(intArray[i]);
			}
			lst.Sort();
		}

		[Benchmark]
		public void SortedSet_Add()
		{
			SCG.SortedSet<int> set = new SCG.SortedSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}
	}

	[RyuJitX64Job]
	//[OpenXmlExporter]
	public class MinMaxIntRange
	{
		public int[] intArray;

		//[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
	//[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000/*, 90_000_000*/  )]
		//[Params(2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 40, 50, 60, 70, 80, 90, 100, 120, 140, 160, 180, 200, 300, 400, 500, 600, 700, 800, 900, 1000  )]
	[Params(1,			2,			3,			4,			5,			6,			7,			8,			9,
			10,			20,			30,			40,			50,			60,			70,			80,			90,
			100,		200,		300,		400,		500,		600,		700,		800,		900,
			1000,		2000,		3000,		4000,		5000,		6000,		7000,		8000,		9000,
			10000,		20000,		30000,		40000,		50000,		60000,		70000,		80000,		90000,
			100000,		200000,		300000,		400000,		500000,		600000,		700000,		800000,		900000,
			1000000,	2000000,	3000000,	4000000,	5000000,	6000000,	7000000,	8000000,	9000000,
			10000000,	20000000,	30_000_000,	40_000_000 )]
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			intArray = new int[N];

			//Random rand = new Random(42);
			//Random rand = new Random(142);
			Random rand = new Random(89);
			for (int i = 0; i < intArray.Length; i++)
			{
				intArray[i] = rand.Next(int.MinValue, int.MaxValue);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void C5_HashSet_Add()
		{
			C5.HashSet<int> set = new C5.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void List_Add()
		{
			// if there are duplicate values in the array, then this will add the duplicates, so this isn't really an apples to apples comparison vs. sets, which will only have added a single value
			SCG.List<int> lst = new SCG.List<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				lst.Add(intArray[i]);
			}
			lst.Sort();
		}

		[Benchmark]
		public void SortedSet_Add()
		{
			SCG.SortedSet<int> set = new SCG.SortedSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}
	}

	[RyuJitX64Job]
	//[HtmlExporter]
	//[OpenXmlExporter]
	public class SmallSize
	{
		public int[] intArray;

		[Params(1, 2)]
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			intArray = new int[N];

			Random rand = new Random(89);
			for (int i = 0; i < intArray.Length; i++)
			{
				intArray[i] = rand.Next(int.MinValue, int.MaxValue);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}
	}

	[RyuJitX64Job]
	public class MinMaxIntRangeUniqueTestResize
	{
		public int[] intArray;

		SCG.HashSet<int> s;
		FastHashSet<int> f;

		// these params are 1 before a resize and then the N that includes a resize - this way we can see how expensive a resize is
	[Params(
3, 4,
7, 8,
9,
16, 17, 18,
32, 33,
 37, 38,
 64, 65,
 89, 90,
 128, 129,
 197, 198,
 256, 257,
 431, 432,
 512, 513,
 919, 920,
 1_024, 1_025,
 1_931, 1_932,
 2_048, 2_049,
 4_049, 4_050,
 4_096, 4_097,
 8_192, 8_193,
 8_419, 8_420,
 16_384, 16_385,
 17_519, 17_520,
 32_768, 32_769,
 36_353, 36_354,
 65_536, 65_537,
 75_431, 75_432,
 131_072, 131_073,
 156_437, 156_438,
 262_144, 262_145,
 324_449, 324_450,
 524_288, 524_289,
 672_827, 672_828,
 1_048_576, 1_048_577,
 1_395_263, 1_395_264,
 2_097_152, 2_097_153,
 2_893_249, 2_893_250,
 4_194_304, 4_194_305,
 5_999_471, 5_999_472,
 8_388_608, 8_388_609,
 11_998_949, 11_998_950,
 16_777_216, 16_777_217,
 23_997_907, 23_997_908,
 33_554_432, 33_554_433,
 47_995_853, 47_995_854
 )]

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			intArray = new int[N];

			Random rand = new Random(89);
			BenchUtil.PopulateIntArray(intArray, rand, int.MinValue, int.MaxValue, 1.0);
		}

		[IterationSetup]
		public void IterSetup()
		{
			s = new SCG.HashSet<int>();
			for (int i = 0; i < intArray.Length - 1; i++)
			{
				s.Add(intArray[i]);
			}

			f = new FastHashSet<int>();
			for (int i = 0; i < intArray.Length - 1; i++)
			{
				f.Add(intArray[i]);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			s.Add(intArray[N - 1]);
		}

		//[Benchmark]
		//public void C5_HashSet_Add()
		//{
		//	C5.HashSet<int> set = new C5.HashSet<int>();
		//	for (int i = 0; i < intArray.Length; i++)
		//	{
		//		set.Add(intArray[i]);
		//	}
		//}

		[Benchmark]
		public void Fast_Add()
		{
			f.Add(intArray[N - 1]);
		}
	}

	public class PosIntRangeTo100
	{
		public int[] a;

	//[Params(1,			2,			3,			4,			5,			6,			7,			8,			9,			10,
	//		11,			12,			13,			14,			15,			16,			17,			18,			19,			20)]


		[Params(1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
			11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
			21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
			31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
			41, 42, 43, 44, 45, 46, 47, 48, 49, 50,
			51, 52, 53, 54, 55, 56, 57, 58, 59, 60,
			61, 62, 63, 64, 65, 66, 67, 68, 69, 70,
			71, 72, 73, 74, 75, 76, 77, 78, 79, 80,
			81, 82, 83, 84, 85, 86, 87, 88, 89, 90,
			91, 92, 93, 94, 95, 96, 97, 98, 99, 100)]


		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new int[N];

			Random rand = new Random(42);
			BenchUtil.PopulateIntArray(a, rand, int.MinValue, int.MaxValue, 1.0);
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(a[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(a[i]);
			}
		}
	}

	public class PosIntRangeTo1000
	{
		public int[] a;

	[Params(100,		110,		120,		130,		140,		150,		160,		170,		180,		190,
			200,		210,		220,		230,		240,		250,		260,		270,		280,		290,
			300,		310,		320,		330,		340,		350,		360,		370,		380,		390,
			400,		410,		420,		430,		440,		450,		460,		470,		480,		490,
			500,		510,		520,		530,		540,		550,		560,		570,		580,		590,
			600,		610,		620,		630,		640,		650,		660,		670,		680,		690,
			700,		710,		720,		730,		740,		750,		760,		770,		780,		790,
			800,		810,		820,		830,		840,		850,		860,		870,		880,		890,
			900,		910,		920,		930,		940,		950,		960,		970,		980,		990,	1000)]

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new int[N];

			Random rand = new Random(42);
			BenchUtil.PopulateIntArray(a, rand, int.MinValue, int.MaxValue, 1.0);
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(a[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(a[i]);
			}
		}
	}

	public class PosIntRangeTo10_000
	{
		public int[] a;

	[Params(1000,		1100,		1200,		1300,		1400,		1500,		1600,		1700,		1800,		1900,
			2000,		2100,		2200,		2300,		2400,		2500,		2600,		2700,		2800,		2900,
			3000,		3100,		3200,		3300,		3400,		3500,		3600,		3700,		3800,		3900,
			4000,		4100,		4200,		4300,		4400,		4500,		4600,		4700,		4800,		4900,
			5000,		5100,		5200,		5300,		5400,		5500,		5600,		5700,		5800,		5900,
			6000,		6100,		6200,		6300,		6400,		6500,		6600,		6700,		6800,		6900,
			7000,		7100,		7200,		7300,		7400,		7500,		7600,		7700,		7800,		7900,
			8000,		8100,		8200,		8300,		8400,		8500,		8600,		8700,		8800,		8900,
			9000,		9100,		9200,		9300,		9400,		9500,		9600,		9700,		9800,		9900,	10000)]

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new int[N];

			Random rand = new Random(42);
			BenchUtil.PopulateIntArray(a, rand, int.MinValue, int.MaxValue, 1.0);
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(a[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(a[i]);
			}
		}
	}

	public class PosIntRangeTo100_000
	{
		public int[] a;


	[Params(10000,		11000,		12000,		13000,		14000,		15000,		16000,		17000,		18000,		19000,
			20000,		21000,		22000,		23000,		24000,		25000,		26000,		27000,		28000,		29000,
			30000,		31000,		32000,		33000,		34000,		35000,		36000,		37000,		38000,		39000,
			40000,		41000,		42000,		43000,		44000,		45000,		46000,		47000,		48000,		49000,
			50000,		51000,		52000,		53000,		54000,		55000,		56000,		57000,		58000,		59000,
			60000,		61000,		62000,		63000,		64000,		65000,		66000,		67000,		68000,		69000,
			70000,		71000,		72000,		73000,		74000,		75000,		76000,		77000,		78000,		79000,
			80000,		81000,		82000,		83000,		84000,		85000,		86000,		87000,		88000,		89000,
			90000,		91000,		92000,		93000,		94000,		95000,		96000,		97000,		98000,		99000,	100_000)]


		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new int[N];

			Random rand = new Random(42);
			BenchUtil.PopulateIntArray(a, rand, int.MinValue, int.MaxValue, 1.0);
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(a[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(a[i]);
			}
		}
	}

	public class PosIntRangeTo1_000_000
	{
		public int[] a;

	[Params(100000,		110000,		120000,		130000,		140000,		150000,		160000,		170000,		180000,		190000,
			200000,		210000,		220000,		230000,		240000,		250000,		260000,		270000,		280000,		290000,
			300000,		310000,		320000,		330000,		340000,		350000,		360000,		370000,		380000,		390000,
			400000,		410000,		420000,		430000,		440000,		450000,		460000,		470000,		480000,		490000,
			500000,		510000,		520000,		530000,		540000,		550000,		560000,		570000,		580000,		590000,
			600000,		610000,		620000,		630000,		640000,		650000,		660000,		670000,		680000,		690000,
			700000,		710000,		720000,		730000,		740000,		750000,		760000,		770000,		780000,		790000,
			800000,		810000,		820000,		830000,		840000,		850000,		860000,		870000,		880000,		890000,
			900000,		910000,		920000,		930000,		940000,		950000,		960000,		970000,		980000,		990000,	1_000_000)]

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new int[N];

			Random rand = new Random(42);
			BenchUtil.PopulateIntArray(a, rand, int.MinValue, int.MaxValue, 1.0);
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(a[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(a[i]);
			}
		}
	}

	public class PosIntRangeTo10_000_000
	{
		public int[] a;

	[Params(1000000,		1100000,		1200000,		1300000,		1400000,		1500000,		1600000,		1700000,		1800000,		1900000,
			2000000,		2100000,		2200000,		2300000,		2400000,		2500000,		2600000,		2700000,		2800000,		2900000,
			3000000,		3100000,		3200000,		3300000,		3400000,		3500000,		3600000,		3700000,		3800000,		3900000,
			4000000,		4100000,		4200000,		4300000,		4400000,		4500000,		4600000,		4700000,		4800000,		4900000,
			5000000,		5100000,		5200000,		5300000,		5400000,		5500000,		5600000,		5700000,		5800000,		5900000,
			6000000,		6100000,		6200000,		6300000,		6400000,		6500000,		6600000,		6700000,		6800000,		6900000,
			7000000,		7100000,		7200000,		7300000,		7400000,		7500000,		7600000,		7700000,		7800000,		7900000,
			8000000,		8100000,		8200000,		8300000,		8400000,		8500000,		8600000,		8700000,		8800000,		8900000,
			9000000,		9100000,		9200000,		9300000,		9400000,		9500000,		9600000,		9700000,		9800000,		9_900_000,	10_000_000)]

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new int[N];

			Random rand = new Random(42);
			BenchUtil.PopulateIntArray(a, rand, int.MinValue, int.MaxValue, 1.0);
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(a[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(a[i]);
			}
		}
	}

	public class PositiveIntRangeAdd10PctUnique
	{
		public int[] a;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new int[N];

			Random rand = new Random(89);
			BenchUtil.PopulateIntArray(a, rand, 1, int.MaxValue, .1);
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> h = new SCG.HashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				h.Add(a[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> h = new FastHashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				h.Add(a[i]);
			}
		}
	}

	public class PositiveIntRangeAddRemoveAdd
	{
		public int[] a;
		public int[] rem;
		public int[] a2;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new int[N];

			Random rand = new Random(89);
			BenchUtil.PopulateIntArray(a, rand, 1, int.MaxValue, .8); // 80% unique values

			// remove (at least calls to Remove) of 20%
			int remCount = a.Length / 5;
			rem = new int[remCount];
			int maxIdx = a.Length - 1;
			for (int i = 0; i < remCount; i++)
			{
				int idx = rand.Next(0, maxIdx);
				rem[i] = a[idx];
			}

			// then reAdd 20%
			a2 = new int[remCount];
			for (int i = 0; i < remCount; i++)
			{
				a2[i] = rand.Next(0, maxIdx);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG_Add()
		{
			SCG.HashSet<int> h = new SCG.HashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				h.Add(a[i]);
			}
			for (int i = 0; i < rem.Length; i++)
			{
				h.Remove(rem[i]);
			}
			for (int i = 0; i < a2.Length; i++)
			{
				h.Add(a2[i]);
			}
		}

		[Benchmark]
		public void Fast_Add()
		{
			FastHashSet<int> h = new FastHashSet<int>();
			for (int i = 0; i < a.Length; i++)
			{
				h.Add(a[i]);
			}
			for (int i = 0; i < rem.Length; i++)
			{
				h.Remove(rem[i]);
			}
			for (int i = 0; i < a2.Length; i++)
			{
				h.Add(a2[i]);
			}
		}
	}

	[RyuJitX64Job]
	public class MinMaxLongRange
	{
		public long[] longArray;

		//[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
	//[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000/*, 90_000_000*/  )]
		//[Params(2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 40, 50, 60, 70, 80, 90, 100, 120, 140, 160, 180, 200, 300, 400, 500, 600, 700, 800, 900, 1000  )]
	[Params(1,			2,			3,			4,			5,			6,			7,			8,			9,
			10,			20,			30,			40,			50,			60,			70,			80,			90,
			100,		200,		300,		400,		500,		600,		700,		800,		900,
			1000,		2000,		3000,		4000,		5000,		6000,		7000,		8000,		9000,
			10000,		20000,		30000,		40000,		50000,		60000,		70000,		80000,		90000,
			100000,		200000,		300000,		400000,		500000,		600000,		700000,		800000,		900000,
			1000000,	2000000,	3000000,	4000000,	5000000,	6000000,	7000000,	8000000,	9000000,
			10000000,	20000000,	30_000_000,	40_000_000 )]
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			longArray = new long[N];

			//Random rand = new Random(42);
			//Random rand = new Random(142);
			Random rand = new Random(89);
			for (int i = 0; i < longArray.Length; i++)
			{
				long n1 = rand.Next(int.MinValue, int.MaxValue);
				long n2 = rand.Next(int.MinValue, int.MaxValue);
				longArray[i] = (n1<<32) | n2;
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG_HashSet_Add()
		{
			SCG.HashSet<long> set = new SCG.HashSet<long>();
			for (int i = 0; i < longArray.Length; i++)
			{
				set.Add(longArray[i]);
			}
		}

		[Benchmark]
		public void FastHashSet_Add()
		{
			FastHashSet<long> set = new FastHashSet<long>();
			for (int i = 0; i < longArray.Length; i++)
			{
				set.Add(longArray[i]);
			}
		}
	}

	[RyuJitX64Job]
	public class AddMediumStruct
	{
		public MediumStruct[] a;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new MediumStruct[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = MediumStruct.CreateRand(rand);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG()
		{
			SCG.HashSet<MediumStruct> set = new SCG.HashSet<MediumStruct>();
			for (int i = 0; i < a.Length; i++)
			{
				ref MediumStruct r = ref a[i];
				set.Add(new MediumStruct(r.myDate, r.myDouble, r.myInt));
			}
		}

		[Benchmark]
		public void Fast()
		{
			FastHashSet<MediumStruct> set = new FastHashSet<MediumStruct>();
			for (int i = 0; i < a.Length; i++)
			{
				ref MediumStruct r = ref a[i];
				set.Add(new MediumStruct(r.myDate, r.myDouble, r.myInt));
			}
		}
	}

	public class TestArrayCall<T>
	{
		public T[] array;
		public int[] hashArray;
		public int count;
		public System.Collections.Generic.IEqualityComparer<T> comparer;

		public TestArrayCall(int capacity)
		{
			array = new T[capacity];
			hashArray = new int[capacity];
			comparer = System.Collections.Generic.EqualityComparer<T>.Default;
		}

		public void Add(T item)
		{
			array[count] = item;
			hashArray[count] = comparer.GetHashCode(item);
			count++;
		}
	}

	[HardwareCounters(HardwareCounter.CacheMisses)]
	[RyuJitX64Job]
	[MemoryDiagnoser]
	public class CallFuncSmallClassVsStructFast
	{
		public SmallClass[] a;
		public SmallStruct[] ast;

		public TestArrayCall<SmallClass> tClass;
		public TestArrayCall<SmallStruct> tStruct;
		//public System.Collections.Generic.IEqualityComparer<SmallClass> comparerClass;
		//public System.Collections.Generic.IEqualityComparer<SmallStruct> comparerStruct;

		//int[] hashArray;

		[Params(1, 2, 3, 4, 5, 6, 7, 8, 9,
				10, 20, 30, 40, 50, 60, 70, 80, 90,
				100, 200, 300, 400, 500, 600, 700, 800, 900,
				1000)]
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new SmallClass[N];
			ast = new SmallStruct[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = SmallClass.CreateRand(rand);
				SmallClass r = a[i];
				ast[i] = new SmallStruct(r.myInt, r.myInt2);
			}

			tClass = new TestArrayCall<SmallClass>(N);
			tStruct = new TestArrayCall<SmallStruct>(N);

			//comparerClass = System.Collections.Generic.EqualityComparer<SmallClass>.Default;
			//comparerStruct = System.Collections.Generic.EqualityComparer<SmallStruct>.Default;

			//hashArray = new int[N];
		}

		[Benchmark(Baseline = true)]
		public void FastClass()
		{
			for (int i = 0; i < a.Length; i++)
			{
				SmallClass x = a[i];
				tClass.Add(x);
			}
		}

		[Benchmark]
		public void FastStruct()
		{
			for (int i = 0; i < ast.Length; i++)
			{
				SmallStruct x = ast[i];
				tStruct.Add(x);
			}
		}
	}
	
	[HardwareCounters(HardwareCounter.CacheMisses)]
	[RyuJitX64Job]
	[MemoryDiagnoser]
	public class CallFuncSmallClassVsStructFast3
	{
		public SmallClass[] a;
		public SmallStruct[] ast;

		public System.Collections.Generic.IEqualityComparer<SmallClass> comparerClass;
		public System.Collections.Generic.IEqualityComparer<SmallStruct> comparerStruct;

		int[] hashArray;

		[Params(1, 2, 3, 4, 5, 6, 7, 8, 9,
				10, 20, 30, 40, 50, 60, 70, 80, 90,
				100, 200, 300, 400, 500, 600, 700, 800, 900,
				1000)]

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new SmallClass[N];
			ast = new SmallStruct[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = SmallClass.CreateRand(rand);
				SmallClass r = a[i];
				ast[i] = new SmallStruct(r.myInt, r.myInt2);
			}

			comparerClass = System.Collections.Generic.EqualityComparer<SmallClass>.Default;
			comparerStruct = System.Collections.Generic.EqualityComparer<SmallStruct>.Default;

			hashArray = new int[N];
		}

		[Benchmark(Baseline = true)]
		public void FastClass()
		{
			for (int i = 0; i < a.Length; i++)
			{
				int hash = comparerClass.GetHashCode(a[i]);
				hashArray[i] = hash;
			}
		}

		[Benchmark]
		public void FastStruct()
		{
			for (int i = 0; i < ast.Length; i++)
			{
				int hash = comparerStruct.GetHashCode(ast[i]);
				hashArray[i] = hash;
			}
		}
	}
	
	[RyuJitX64Job]
	public class CallFuncSmallClassVsStructFast2
	{
		public SmallClass[] a;

		public SmallClass sc1;
		public SmallClass sc2;
		public SmallClass sc3;
		public SmallStruct st1;
		public SmallStruct st2;
		public SmallStruct st3;
		public System.Collections.Generic.IEqualityComparer<SmallClass> comparerClass;
		public System.Collections.Generic.IEqualityComparer<SmallStruct> comparerStruct;

		int h1;
		int h2;
		int h3;

		[Params(3)]

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new SmallClass[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = SmallClass.CreateRand(rand);
			}

			sc1 = new SmallClass(a[0].myInt, a[0].myInt2);
			sc1 = new SmallClass(a[1].myInt, a[1].myInt2);
			sc1 = new SmallClass(a[2].myInt, a[2].myInt2);

			st1 = new SmallStruct(a[0].myInt, a[0].myInt2);
			st1 = new SmallStruct(a[1].myInt, a[1].myInt2);
			st1 = new SmallStruct(a[2].myInt, a[2].myInt2);
			comparerClass = System.Collections.Generic.EqualityComparer<SmallClass>.Default;
			comparerStruct = System.Collections.Generic.EqualityComparer<SmallStruct>.Default;
		}

		[Benchmark(Baseline = true)]
		public void FastClass()
		{
			h1 = comparerClass.GetHashCode(sc1);
			h2 = comparerClass.GetHashCode(sc2);
			h3 = comparerClass.GetHashCode(sc3);
		}

		[Benchmark]
		public void FastStruct()
		{
			h1 = comparerStruct.GetHashCode(st1);
			h2 = comparerStruct.GetHashCode(st2);
			h3 = comparerStruct.GetHashCode(st3);
		}
	}
	
	[HardwareCounters(HardwareCounter.CacheMisses)]
	[RyuJitX64Job]
	[MemoryDiagnoser]
	public class AddSmallClassVsStructFast
	{
		public SmallClass[] a;
		public SmallStruct[] ast;

		//[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		//[Params(1, 2, 3, 4, 5, 6, 7, 8, 9,
		//		10, 20, 30, 40, 50, 60, 70, 80, 90,
		//		100, 200, 300, 400, 500, 600, 700, 800, 900,
		//		1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000,
		//		10000, 20000, 30000, 40000, 50000, 60000, 70000, 80000, 90000,
		//		100000, 200000, 300000, 400000, 500000, 600000, 700000, 800000, 900000,
		//		1000000, 2000000, 3000000, 4000000, 5000000, 6000000, 7000000, 8000000, 9000000,
		//		10000000, 20000000, 30_000_000, 40_000_000)]

		[Params(1, 2, 3, 4, 5, 6, 7, 8, 9,
				10, 20, 30, 40, 50, 60, 70, 80, 90,
				100, 200, 300, 400, 500, 600, 700, 800, 900,
				1000, 10_000, 100_000, 1_000_000)]

		//[Params(1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
		//	11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
		//	21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
		//	31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
		//	41, 42, 43, 44, 45, 46, 47, 48, 49, 50,
		//	51, 52, 53, 54, 55, 56, 57, 58, 59, 60,
		//	61, 62, 63, 64, 65, 66, 67, 68, 69, 70,
		//	71, 72, 73, 74, 75, 76, 77, 78, 79, 80,
		//	81, 82, 83, 84, 85, 86, 87, 88, 89, 90,
		//	91, 92, 93, 94, 95, 96, 97, 98, 99, 100)]
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new SmallClass[N];
			ast = new SmallStruct[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = SmallClass.CreateRand(rand);
				SmallClass r = a[i];
				ast[i] = new SmallStruct(r.myInt, r.myInt2);
			}
		}

		[Benchmark(Baseline = true)]
		public void FastClass()
		{
			FastHashSet<SmallClass> set = new FastHashSet<SmallClass>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(new SmallClass(a[i].myInt, a[i].myInt2));
			}
		}

		[Benchmark]
		public void FastStruct()
		{
			FastHashSet<SmallStruct> set = new FastHashSet<SmallStruct>();
			for (int i = 0; i < ast.Length; i++)
			{
				set.Add(new SmallStruct(ast[i].myInt, ast[i].myInt2));
			}
		}
	}
	
	[HardwareCounters(HardwareCounter.CacheMisses)]
	[RyuJitX64Job]
	public class AddSmallClassVsStructFast2
	{
		public SmallStruct[] a;

		//[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		//[Params(1,			2,			3,			4,			5,			6,			7,			8,			9,
		//		10,			20,			30,			40,			50,			60,			70,			80,			90,
		//		100,		200,		300,		400,		500,		600,		700,		800,		900,
		//		1000,		2000,		3000,		4000,		5000,		6000,		7000,		8000,		9000,
		//		10000,		20000,		30000,		40000,		50000,		60000,		70000,		80000,		90000,
		//		100000,		200000,		300000,		400000,		500000,		600000,		700000,		800000,		900000,
		//		1000000,	2000000,	3000000,	4000000,	5000000,	6000000,	7000000,	8000000,	9000000,
		//		10000000,	20000000,	30_000_000,	40_000_000 )]

		//[Params(1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
		//11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
		//21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
		//31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
		//41, 42, 43, 44, 45, 46, 47, 48, 49, 50,
		//51, 52, 53, 54, 55, 56, 57, 58, 59, 60,
		//61, 62, 63, 64, 65, 66, 67, 68, 69, 70,
		//71, 72, 73, 74, 75, 76, 77, 78, 79, 80,
		//81, 82, 83, 84, 85, 86, 87, 88, 89, 90,
		//91, 92, 93, 94, 95, 96, 97, 98, 99, 100,

		//			110,		120,		130,		140,		150,		160,		170,		180,		190,
		//200,		210,		220,		230,		240,		250,		260,		270,		280,		290,
		//300,		310,		320,		330,		340,		350,		360,		370,		380,		390,
		//400,		410,		420,		430,		440,		450,		460,		470,		480,		490,
		//500,		510,		520,		530,		540,		550,		560,		570,		580,		590,
		//600,		610,		620,		630,		640,		650,		660,		670,		680,		690,
		//700,		710,		720,		730,		740,		750,		760,		770,		780,		790,
		//800,		810,		820,		830,		840,		850,		860,		870,		880,		890,
		//900,		910,		920,		930,		940,		950,		960,		970,		980,		990,	1000,

		//			1100,		1200,		1300,		1400,		1500,		1600,		1700,		1800,		1900,
		//2000,		2100,		2200,		2300,		2400,		2500,		2600,		2700,		2800,		2900,
		//3000,		3100,		3200,		3300,		3400,		3500,		3600,		3700,		3800,		3900,
		//4000,		4100,		4200,		4300,		4400,		4500,		4600,		4700,		4800,		4900,
		//5000,		5100,		5200,		5300,		5400,		5500,		5600,		5700,		5800,		5900,
		//6000,		6100,		6200,		6300,		6400,		6500,		6600,		6700,		6800,		6900,
		//7000,		7100,		7200,		7300,		7400,		7500,		7600,		7700,		7800,		7900,
		//8000,		8100,		8200,		8300,		8400,		8500,		8600,		8700,		8800,		8900,
		//9000,		9100,		9200,		9300,		9400,		9500,		9600,		9700,		9800,		9900,	10000,
		//			11000,		12000,		13000,		14000,		15000,		16000,		17000,		18000,		19000,
		//20000,		21000,		22000,		23000,		24000,		25000,		26000,		27000,		28000,		29000,
		//30000,		31000,		32000,		33000,		34000,		35000,		36000,		37000,		38000,		39000,
		//40000,		41000,		42000,		43000,		44000,		45000,		46000,		47000,		48000,		49000,
		//50000,		51000,		52000,		53000,		54000,		55000,		56000,		57000,		58000,		59000,
		//60000,		61000,		62000,		63000,		64000,		65000,		66000,		67000,		68000,		69000,
		//70000,		71000,		72000,		73000,		74000,		75000,		76000,		77000,		78000,		79000,
		//80000,		81000,		82000,		83000,		84000,		85000,		86000,		87000,		88000,		89000,
		//90000,		91000,		92000,		93000,		94000,		95000,		96000,		97000,		98000,		99000,	100_000,
		//			110000,		120000,		130000,		140000,		150000,		160000,		170000,		180000,		190000,
		//200000,		210000,		220000,		230000,		240000,		250000,		260000,		270000,		280000,		290000,
		//300000,		310000,		320000,		330000,		340000,		350000,		360000,		370000,		380000,		390000,
		//400000,		410000,		420000,		430000,		440000,		450000,		460000,		470000,		480000,		490000,
		//500000,		510000,		520000,		530000,		540000,		550000,		560000,		570000,		580000,		590000,
		//600000,		610000,		620000,		630000,		640000,		650000,		660000,		670000,		680000,		690000,
		//700000,		710000,		720000,		730000,		740000,		750000,		760000,		770000,		780000,		790000,
		//800000,		810000,		820000,		830000,		840000,		850000,		860000,		870000,		880000,		890000,
		//900000,		910000,		920000,		930000,		940000,		950000,		960000,		970000,		980000,		990000,	1_000_000,
		//			1100000,		1200000,		1300000,		1400000,		1500000,		1600000,		1700000,		1800000,		1900000,
		//2000000,		2100000,		2200000,		2300000,		2400000,		2500000,		2600000,		2700000,		2800000,		2900000,
		//3000000,		3100000,		3200000,		3300000,		3400000,		3500000,		3600000,		3700000,		3800000,		3900000,
		//4000000,		4100000,		4200000,		4300000,		4400000,		4500000,		4600000,		4700000,		4800000,		4900000,
		//5000000,		5100000,		5200000,		5300000,		5400000,		5500000,		5600000,		5700000,		5800000,		5900000,
		//6000000,		6100000,		6200000,		6300000,		6400000,		6500000,		6600000,		6700000,		6800000,		6900000,
		//7000000,		7100000,		7200000,		7300000,		7400000,		7500000,		7600000,		7700000,		7800000,		7900000,
		//8000000,		8100000,		8200000,		8300000,		8400000,		8500000,		8600000,		8700000,		8800000,		8900000,
		//9000000,		9100000,		9200000,		9300000,		9400000,		9500000,		9600000,		9700000,		9800000,		9_900_000,	10_000_000)]	
		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000)]
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new SmallStruct[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = SmallStruct.CreateRand(rand);
			}
		}

		[Benchmark(Baseline = true)]
		public void FastClass()
		{
			FastHashSet<SmallClass> set = new FastHashSet<SmallClass>();
			for (int i = 0; i < a.Length; i++)
			{
				SmallStruct r = a[i];
				set.Add(new SmallClass(r.myInt, r.myInt2));
			}
		}

		[Benchmark]
		public void FastStruct()
		{
			FastHashSet<SmallStruct> set = new FastHashSet<SmallStruct>();
			for (int i = 0; i < a.Length; i++)
			{
				SmallStruct r = a[i];
				set.Add(new SmallStruct(r.myInt, r.myInt2));
			}
		}
	}

	// Compare a reference counting implementation using SCG HashSet<SmallClassIntVal> vs. FastHashSet<SmallStructIntVal>
	[RyuJitX64Job]
	public class RefCountHashSetVFastHashSet
	{
		public int[] a;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000)]
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new int[N];

			Random rand = new Random(89);
			int nDiv2 = N / 2;
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = rand.Next(1, nDiv2); // make sure there are some duplicates
			}
		}

		[Benchmark(Baseline = true)]
		public void SCGRefCnt()
		{
			HashSet<SmallClassIntVal> set = new HashSet<SmallClassIntVal>();
			for (int i = 0; i < a.Length; i++)
			{
				SmallClassIntVal v = new SmallClassIntVal(a[i], 1);
				if (set.TryGetValue(v, out SmallClassIntVal v2))
				{
					v2.refCountOrSum++;
				}
				else
				{
					set.Add(v);
				}
			}
			for (int i = a.Length - 1; i >= 0; i--)
			{
				SmallClassIntVal v = new SmallClassIntVal(a[i], 1);
				if (set.TryGetValue(v, out SmallClassIntVal v2))
				{
					if (v2.refCountOrSum == 1)
					{
						set.Remove(v);
					}
					else
					{
						v2.refCountOrSum--;
					}
				}
			}
		}

		[Benchmark]
		public void FastRefCnt()
		{

			FastHashSet<SmallStructIntVal> set = new FastHashSet<SmallStructIntVal>();
			for (int i = 0; i < a.Length; i++)
			{
				SmallStructIntVal v = new SmallStructIntVal(a[i], 1);
				ref SmallStructIntVal v2 = ref set.FindOrAdd(in v, out bool isFound);
				if (isFound)
				{
					v2.refCountOrSum++;
				}
			}

			for (int i = a.Length - 1; i >= 0; i--)
			{
				SmallStructIntVal v = new SmallStructIntVal(a[i], 0);
				ref SmallStructIntVal v2 = ref set.FindAndRemoveIf(in v, b => b.refCountOrSum == 1, out bool isFound, out bool isRemoved);
				if (!isRemoved && isFound)
				{
					v2.refCountOrSum--;
				}
			}
		}
	}

	[RyuJitX64Job]
	public class AddSmallClassVsStructFast3
	{
		public int[] a;
		public int[] a2;

		[Params(1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
		11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
		21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
		31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
		41, 42, 43, 44, 45, 46, 47, 48, 49, 50,
		51, 52, 53, 54, 55, 56, 57, 58, 59, 60,
		61, 62, 63, 64, 65, 66, 67, 68, 69, 70,
		71, 72, 73, 74, 75, 76, 77, 78, 79, 80,
		81, 82, 83, 84, 85, 86, 87, 88, 89, 90,
		91, 92, 93, 94, 95, 96, 97, 98, 99, 100,

					110, 120, 130, 140, 150, 160, 170, 180, 190,
		200, 210, 220, 230, 240, 250, 260, 270, 280, 290,
		300, 310, 320, 330, 340, 350, 360, 370, 380, 390,
		400, 410, 420, 430, 440, 450, 460, 470, 480, 490,
		500, 510, 520, 530, 540, 550, 560, 570, 580, 590,
		600, 610, 620, 630, 640, 650, 660, 670, 680, 690,
		700, 710, 720, 730, 740, 750, 760, 770, 780, 790,
		800, 810, 820, 830, 840, 850, 860, 870, 880, 890,
		900, 910, 920, 930, 940, 950, 960, 970, 980, 990, 1000,

					1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900,
		2000, 2100, 2200, 2300, 2400, 2500, 2600, 2700, 2800, 2900,
		3000, 3100, 3200, 3300, 3400, 3500, 3600, 3700, 3800, 3900,
		4000, 4100, 4200, 4300, 4400, 4500, 4600, 4700, 4800, 4900,
		5000, 5100, 5200, 5300, 5400, 5500, 5600, 5700, 5800, 5900,
		6000, 6100, 6200, 6300, 6400, 6500, 6600, 6700, 6800, 6900,
		7000, 7100, 7200, 7300, 7400, 7500, 7600, 7700, 7800, 7900,
		8000, 8100, 8200, 8300, 8400, 8500, 8600, 8700, 8800, 8900,
		9000, 9100, 9200, 9300, 9400, 9500, 9600, 9700, 9800, 9900, 10000,
					11000, 12000, 13000, 14000, 15000, 16000, 17000, 18000, 19000,
		20000, 21000, 22000, 23000, 24000, 25000, 26000, 27000, 28000, 29000,
		30000, 31000, 32000, 33000, 34000, 35000, 36000, 37000, 38000, 39000,
		40000, 41000, 42000, 43000, 44000, 45000, 46000, 47000, 48000, 49000,
		50000, 51000, 52000, 53000, 54000, 55000, 56000, 57000, 58000, 59000,
		60000, 61000, 62000, 63000, 64000, 65000, 66000, 67000, 68000, 69000,
		70000, 71000, 72000, 73000, 74000, 75000, 76000, 77000, 78000, 79000,
		80000, 81000, 82000, 83000, 84000, 85000, 86000, 87000, 88000, 89000,
		90000, 91000, 92000, 93000, 94000, 95000, 96000, 97000, 98000, 99000, 100_000,
					110000, 120000, 130000, 140000, 150000, 160000, 170000, 180000, 190000,
		200000, 210000, 220000, 230000, 240000, 250000, 260000, 270000, 280000, 290000,
		300000, 310000, 320000, 330000, 340000, 350000, 360000, 370000, 380000, 390000,
		400000, 410000, 420000, 430000, 440000, 450000, 460000, 470000, 480000, 490000,
		500000, 510000, 520000, 530000, 540000, 550000, 560000, 570000, 580000, 590000,
		600000, 610000, 620000, 630000, 640000, 650000, 660000, 670000, 680000, 690000,
		700000, 710000, 720000, 730000, 740000, 750000, 760000, 770000, 780000, 790000,
		800000, 810000, 820000, 830000, 840000, 850000, 860000, 870000, 880000, 890000,
		900000, 910000, 920000, 930000, 940000, 950000, 960000, 970000, 980000, 990000, 1_000_000,
					1100000, 1200000, 1300000, 1400000, 1500000, 1600000, 1700000, 1800000, 1900000,
		2000000, 2100000, 2200000, 2300000, 2400000, 2500000, 2600000, 2700000, 2800000, 2900000,
		3000000, 3100000, 3200000, 3300000, 3400000, 3500000, 3600000, 3700000, 3800000, 3900000,
		4000000, 4100000, 4200000, 4300000, 4400000, 4500000, 4600000, 4700000, 4800000, 4900000,
		5000000, 5100000, 5200000, 5300000, 5400000, 5500000, 5600000, 5700000, 5800000, 5900000,
		6000000, 6100000, 6200000, 6300000, 6400000, 6500000, 6600000, 6700000, 6800000, 6900000,
		7000000, 7100000, 7200000, 7300000, 7400000, 7500000, 7600000, 7700000, 7800000, 7900000,
		8000000, 8100000, 8200000, 8300000, 8400000, 8500000, 8600000, 8700000, 8800000, 8900000,
		9000000, 9100000, 9200000, 9300000, 9400000, 9500000, 9600000, 9700000, 9800000, 9_900_000, 10_000_000)]
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new int[N];
			a2 = new int[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = rand.Next();
				a2[i] = rand.Next();
			}
		}

		[Benchmark(Baseline = true)]
		public void FastClass()
		{
			FastHashSet<SmallClass> set = new FastHashSet<SmallClass>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(new SmallClass(a[i], a2[i]));
			}
		}

		[Benchmark]
		public void FastStruct()
		{
			FastHashSet<SmallStruct> set = new FastHashSet<SmallStruct>();
			for (int i = 0; i < a.Length; i++)
			{
				SmallStruct s = new SmallStruct(a[i], a2[i]);
				set.Add(in s);
			}
		}
	}

	[RyuJitX64Job]
	public class AddSmallClassVsStructList
	{
		public int[] a;
		public int[] a2;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000)]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new int[N];
			a2 = new int[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = rand.Next();
				a2[i] = rand.Next();
			}
		}

		[Benchmark(Baseline = true)]
		public void FastClass()
		{
			List<SmallClassBasic> lst = new List<SmallClassBasic>();
			for (int i = 0; i < a.Length; i++)
			{
				lst.Add(new SmallClassBasic(a[i], a2[i]));
			}
		}

		[Benchmark]
		public void FastStruct()
		{
			List<SmallStructBasic> lst = new List<SmallStructBasic>();
			for (int i = 0; i < a.Length; i++)
			{
				lst.Add(new SmallStructBasic(a[i], a2[i]));
			}
		}
	}

	[RyuJitX64Job]
	public class SumSmallClassVsStructList
	{
		public int[] a;
		public int[] a2;


		public List<SmallClassBasic> classList;
		public List<SmallStructBasic> structList;

		public int sumClass;
		public int sumStruct;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000)]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new int[N];
			a2 = new int[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = rand.Next();
				a2[i] = rand.Next();
			}

			classList = new List<SmallClassBasic>();
			for (int i = 0; i < a.Length; i++)
			{
				classList.Add(new SmallClassBasic(a[i], a2[i]));
			}

			structList = new List<SmallStructBasic>();
			for (int i = 0; i < a.Length; i++)
			{
				structList.Add(new SmallStructBasic(a[i], a2[i]));
			}

			BenchUtil.ClearCpuCaches();
		}

		[Benchmark(Baseline = true)]
		public void FastClass()
		{
			for (int i = 0; i < classList.Count; i++)
			{
				sumClass += classList[i].myInt;
			}
		}

		[Benchmark]
		public void FastStruct()
		{
			for (int i = 0; i < structList.Count; i++)
			{
				sumStruct += structList[i].myInt;
			}
		}
	}

	[RyuJitX64Job]
	public class AddMediumClassVsStructFast
	{
		public MediumClass[] a;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new MediumClass[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = MediumClass.CreateRand(rand);
			}
		}

		[Benchmark(Baseline = true)]
		public void FastClass()
		{
			FastHashSet<MediumClass> set = new FastHashSet<MediumClass>();
			for (int i = 0; i < a.Length; i++)
			{
				MediumClass r = a[i];
				set.Add(new MediumClass(r.myDate, r.myDouble, r.myInt));
			}
		}

		[Benchmark]
		public void FastStruct()
		{
			FastHashSet<MediumStruct> set = new FastHashSet<MediumStruct>();
			for (int i = 0; i < a.Length; i++)
			{
				MediumClass r = a[i];
				set.Add(new MediumStruct(r.myDate, r.myDouble, r.myInt));
			}
		}
	}
	
	[RyuJitX64Job]
	public class AddMediumClassVsStructFastWithIn
	{
		public MediumClass[] a;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new MediumClass[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = MediumClass.CreateRand(rand);
			}
		}

		[Benchmark(Baseline = true)]
		public void FastClass()
		{
			//FastHashSet<MediumClass> set = new FastHashSet<MediumClass>();
			//for (int i = 0; i < a.Length; i++)
			//{
			//	MediumClass s = new MediumClass(a[i].myDate, a[i].myDouble, a[i].myInt);
			//	set.Add(a[i]);
			//}
			FastHashSet<MediumClass> set = new FastHashSet<MediumClass>();
			for (int i = 0; i < a.Length; i++)
			{
				MediumClass r = a[i];
				set.Add(new MediumClass(r.myDate, r.myDouble, r.myInt));
			}
		}

		[Benchmark]
		public void FastStruct()
		{
			FastHashSet<MediumStruct> set = new FastHashSet<MediumStruct>();
			for (int i = 0; i < a.Length; i++)
			{
				MediumStruct s = new MediumStruct(a[i].myDate, a[i].myDouble, a[i].myInt);
				set.Add(in s);
			}
		}
	}
	
	[RyuJitX64Job]
	public class AddMediumStructInVsNot
	{
		public MediumStruct[] a;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new MediumStruct[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = MediumStruct.CreateRand(rand);
			}
		}

		[Benchmark(Baseline = true)]
		public void FastStruct()
		{
			FastHashSet<MediumStruct> set = new FastHashSet<MediumStruct>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(a[i]);
			}
		}

		[Benchmark]
		public void FastStructIn()
		{
			FastHashSet<MediumStruct> set = new FastHashSet<MediumStruct>();
			for (int i = 0; i < a.Length; i++)
			{
				set.Add(in a[i]);
			}
		}
	}
	
	[RyuJitX64Job]
	public class AddLargeClassVsStructFast
	{
		public LargeClass[] a;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new LargeClass[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = LargeClass.CreateRand(rand);
			}
		}

		[Benchmark(Baseline = true)]
		public void FastClass()
		{
			FastHashSet<LargeClass> set = new FastHashSet<LargeClass>();
			for (int i = 0; i < a.Length; i++)
			{
				LargeClass r = a[i];
				set.Add(new LargeClass(r.myDate, r.myDouble, r.myDouble2, r.myInt, r.myInt2, r.myInt3, r.myString));
			}
		}

		[Benchmark]
		public void FastStruct()
		{
			FastHashSet<LargeStruct> set = new FastHashSet<LargeStruct>();
			for (int i = 0; i < a.Length; i++)
			{
				LargeClass r = a[i];
				set.Add(new LargeStruct(r.myDate, r.myDouble, r.myDouble2, r.myInt, r.myInt2, r.myInt3, r.myString));
			}
		}
	}
	
	[RyuJitX64Job]
	public class AddMediumClass
	{
		public MediumClass[] a;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new MediumClass[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = MediumClass.CreateRand(rand);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG()
		{
			SCG.HashSet<MediumClass> set = new SCG.HashSet<MediumClass>();
			for (int i = 0; i < a.Length; i++)
			{
				MediumClass r = a[i];
				set.Add(new MediumClass(r.myDate, r.myDouble, r.myInt));
			}
		}

		[Benchmark]
		public void Fast()
		{
			FastHashSet<MediumClass> set = new FastHashSet<MediumClass>();
			for (int i = 0; i < a.Length; i++)
			{
				MediumClass r = a[i];
				set.Add(new MediumClass(r.myDate, r.myDouble, r.myInt));
			}
		}
	}
	
	[RyuJitX64Job]
	public class AddLargeStruct
	{
		public LargeStruct[] a;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new LargeStruct[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = LargeStruct.CreateRand(rand);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG()
		{
			SCG.HashSet<LargeStruct> set = new SCG.HashSet<LargeStruct>();
			for (int i = 0; i < a.Length; i++)
			{
				ref LargeStruct r = ref a[i];
				set.Add(new LargeStruct(r.myDate, r.myDouble, r.myDouble2, r.myInt, r.myInt2, r.myInt3, r.myString));
			}
		}

		[Benchmark]
		public void Fast()
		{
			FastHashSet<LargeStruct> set = new FastHashSet<LargeStruct>();
			for (int i = 0; i < a.Length; i++)
			{
				ref LargeStruct r = ref a[i];
				set.Add(new LargeStruct(r.myDate, r.myDouble, r.myDouble2, r.myInt, r.myInt2, r.myInt3, r.myString));
			}
		}
	}
	
	[RyuJitX64Job]
	public class AddLargeClass
	{
		public LargeClass[] a;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new LargeClass[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = LargeClass.CreateRand(rand);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG()
		{
			SCG.HashSet<LargeClass> set = new SCG.HashSet<LargeClass>();
			for (int i = 0; i < a.Length; i++)
			{
				LargeClass r = a[i];
				set.Add(new LargeClass(r.myDate, r.myDouble, r.myDouble2, r.myInt, r.myInt2, r.myInt3, r.myString));
			}
		}

		[Benchmark]
		public void Fast()
		{
			FastHashSet<LargeClass> set = new FastHashSet<LargeClass>();
			for (int i = 0; i < a.Length; i++)
			{
				LargeClass r = a[i];
				set.Add(new LargeClass(r.myDate, r.myDouble, r.myDouble2, r.myInt, r.myInt2, r.myInt3, r.myString));
			}
		}
	}
	
	[RyuJitX64Job]
	public class AddMediumStructInCon
	{
		public MediumStruct[] a;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new MediumStruct[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = MediumStruct.CreateRand(rand);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG()
		{
			SCG.HashSet<MediumStruct> set = new SCG.HashSet<MediumStruct>(a);
		}

		[Benchmark]
		public void Fast()
		{
			FastHashSet<MediumStruct> set = new FastHashSet<MediumStruct>(a);
		}
	}
	

	[RyuJitX64Job]
	public class AddMediumClassInCon
	{
		public MediumClass[] a;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new MediumClass[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = MediumClass.CreateRand(rand);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG()
		{
			SCG.HashSet<MediumClass> set = new SCG.HashSet<MediumClass>(a);
		}

		[Benchmark]
		public void Fast()
		{
			FastHashSet<MediumClass> set = new FastHashSet<MediumClass>(a);
		}
	}
	
	[RyuJitX64Job]
	public class AddLargeStructInCon
	{
		public LargeStruct[] a;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new LargeStruct[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = LargeStruct.CreateRand(rand);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG()
		{
			SCG.HashSet<LargeStruct> set = new SCG.HashSet<LargeStruct>(a);
		}

		[Benchmark]
		public void Fast()
		{
			FastHashSet<LargeStruct> set = new FastHashSet<LargeStruct>(a);
		}
	}
	
	[RyuJitX64Job]
	public class AddLargeClassInCon
	{
		public LargeClass[] a;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			a = new LargeClass[N];

			Random rand = new Random(89);
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = LargeClass.CreateRand(rand);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG()
		{
			SCG.HashSet<LargeClass> set = new SCG.HashSet<LargeClass>(a);
		}

		[Benchmark]
		public void Fast()
		{
			FastHashSet<LargeClass> set = new FastHashSet<LargeClass>(a);
		}
	}
	
	//[MinColumn, MaxColumn] //RankColumn
	//[HardwareCounters(HardwareCounter.CacheMisses)]
	[RyuJitX64Job] //LegacyJitX86Job
	//[MemoryDiagnoser]
	public class MinMaxIntRangeAddInConstructor
	{
		public int[] intArray;

		//[Params(1_000_000 )]// 100_000_000
	//[Params(1,			2,			3,			4,			5,			6,			7,			8,			9,
	//		10,			20,			30,			40,			50,			60,			70,			80,			90,
	//		100,		200,		300,		400,		500,		600,		700,		800,		900,
	//		1000,		2000,		3000,		4000,		5000,		6000,		7000,		8000,		9000,
	//		10000,		20000,		30000,		40000,		50000,		60000,		70000,		80000,		90000,
	//		100000,		200000,		300000,		400000,		500000,		600000,		700000,		800000,		900000,
	//		1000000,	2000000,	3000000,	4000000,	5000000,	6000000,	7000000,	8000000,	9000000,
	//		10000000,	20000000,	30_000_000,	40_000_000 )]
		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		//[Params(5, 10, 20, 30, 40, 50, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000/*, 90_000_000*/  )]
		//[Params(2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 40, 50, 60, 70, 80, 90, 100, 120, 140, 160, 180, 200, 300, 400, 500, 600, 700, 800, 900, 1000  )]
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			intArray = new int[N];

			//Random rand = new Random(42);
			//Random rand = new Random(142);
			Random rand = new Random(89);
			for (int i = 0; i < intArray.Length; i++)
			{
				intArray[i] = rand.Next(int.MinValue, int.MaxValue);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>(intArray);
		}

		[Benchmark]
		public void Fast()
		{
			FastHashSet<int> set = new FastHashSet<int>(intArray);
		}
	}

	[RyuJitX64Job]
	public class SmallStringUpperCase
	{
		public string[] stringArray;
		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			stringArray = new string[N];

			Random rand = new Random(89);
			string[] strFreq = new string[] { StringRandUtil.uppercaseChars };
			for (int i = 0; i < stringArray.Length; i++)
			{
				stringArray[i] = StringRandUtil.CreateRandomString(rand, 6, 12, strFreq);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG()
		{
			SCG.HashSet<string> set = new SCG.HashSet<string>();
			for (int i = 0; i < stringArray.Length; i++)
			{
				set.Add(stringArray[i]);
			}
		}

		[Benchmark]
		public void Fast()
		{
			FastHashSet<string> set = new FastHashSet<string>();
			for (int i = 0; i < stringArray.Length; i++)
			{
				set.Add(stringArray[i]);
			}
		}
	}

	[RyuJitX64Job]
	public class MediumStringUpperCase
	{
		public string[] stringArray;
		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			stringArray = new string[N];

			Random rand = new Random(89);
			string[] strFreq = new string[] { StringRandUtil.uppercaseChars };
			for (int i = 0; i < stringArray.Length; i++)
			{
				stringArray[i] = StringRandUtil.CreateRandomString(rand, 12, 30, strFreq);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG()
		{
			SCG.HashSet<string> set = new SCG.HashSet<string>();
			for (int i = 0; i < stringArray.Length; i++)
			{
				set.Add(stringArray[i]);
			}
		}

		[Benchmark]
		public void Fast()
		{
			FastHashSet<string> set = new FastHashSet<string>();
			for (int i = 0; i < stringArray.Length; i++)
			{
				set.Add(stringArray[i]);
			}
		}
	}

	[RyuJitX64Job]
	public class LargeStringUpperCase
	{
		public string[] stringArray;
		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			stringArray = new string[N];

			Random rand = new Random(89);
			string[] strFreq = new string[] { StringRandUtil.uppercaseChars };
			for (int i = 0; i < stringArray.Length; i++)
			{
				stringArray[i] = StringRandUtil.CreateRandomString(rand, 30, 80, strFreq);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG()
		{
			SCG.HashSet<string> set = new SCG.HashSet<string>();
			for (int i = 0; i < stringArray.Length; i++)
			{
				set.Add(stringArray[i]);
			}
		}

		[Benchmark]
		public void Fast()
		{
			FastHashSet<string> set = new FastHashSet<string>();
			for (int i = 0; i < stringArray.Length; i++)
			{
				set.Add(stringArray[i]);
			}
		}
	}

	[RyuJitX64Job]
	public class LargeStringMixed
	{
		public string[] stringArray;
		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000

		public int N;

		[GlobalSetup]
		public void Setup()
		{
			stringArray = new string[N];

			Random rand = new Random(89);
			string[] strFreq = new string[] { 
				StringRandUtil.uppercaseChars,
				StringRandUtil.uppercaseChars,
				StringRandUtil.lowercaseChars,
				StringRandUtil.lowercaseChars,
				StringRandUtil.digits,
				StringRandUtil.space,
				StringRandUtil.symbols,
				};
			for (int i = 0; i < stringArray.Length; i++)
			{
				stringArray[i] = StringRandUtil.CreateRandomString(rand, 30, 80, strFreq);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG()
		{
			SCG.HashSet<string> set = new SCG.HashSet<string>();
			for (int i = 0; i < stringArray.Length; i++)
			{
				set.Add(stringArray[i]);
			}
		}

		[Benchmark]
		public void Fast()
		{
			FastHashSet<string> set = new FastHashSet<string>();
			for (int i = 0; i < stringArray.Length; i++)
			{
				set.Add(stringArray[i]);
			}
		}
	}

	[MinColumn, MaxColumn] //RankColumn
	[HardwareCounters(HardwareCounter.CacheMisses)]
	[RyuJitX64Job] //LegacyJitX86Job
	//[MemoryDiagnoser]
	public class MinMaxIntRangeFastVsFast2
	{
		public int[] intArray;

		//[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000, 90_000_000  )]
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			intArray = new int[N];

			//Random rand = new Random(42);
			//Random rand = new Random(142);
			Random rand = new Random(89);
			for (int i = 0; i < intArray.Length; i++)
			{
				intArray[i] = rand.Next(int.MinValue, int.MaxValue);
			}
		}

		[Benchmark(Baseline = true)]
		public void Test_Add2()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void Test_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}
	}

	//[ClrJob(baseline: true)] //, CoreJob, MonoJob, CoreRtJob
	//[RPlotExporter]
	[MinColumn, MaxColumn] //RankColumn
	[HardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.BranchMispredictions)] //, HardwareCounter.LlcMisses
	[RyuJitX64Job, LegacyJitX86Job]
	public class SmallerIntRange
	{
		public int[] intArray;
		//public int[] extraIntArray;

		//public HashSet<int> hset = new HashSet<int>();
		//public FastHashSet<int> fset = new FastHashSet<int>();

		//[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000, 100_000_000)]
		//[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000 )]// 100_000_000
		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000  )]
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			intArray = new int[N];

			//Random rand = new Random(42);
			//Random rand = new Random(142);
			Random rand = new Random(89);
			for (int i = 0; i < intArray.Length; i++)
			{
				intArray[i] = rand.Next(1, N);
			}
		}

		[Benchmark(Baseline = true)]
		public void SCG_HashSet_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void C5_HashSet_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}

		[Benchmark]
		public void FastHashSet_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}
	}

	//[ClrJob(baseline: true)] //, CoreJob, MonoJob, CoreRtJob
	//[RPlotExporter]
	[MinColumn, MaxColumn] //RankColumn
	[HardwareCounters(HardwareCounter.CacheMisses)]
	[RyuJitX64Job, LegacyJitX86Job]
	public class ExtraInts_SmallerRange
	{
		public int[] intArray;
		public int[] extraIntArray;

		public SCG.HashSet<int> hset = new SCG.HashSet<int>();
		public FastHashSet<int> fset = new FastHashSet<int>();

		[Params(3000)]
		public int N;

		[GlobalSetup]
		public void SetupWithSmallerRange()
		{
			intArray = new int[N];

			Random rand = new Random(89);
			for (int i = 0; i < intArray.Length; i++)
			{
				intArray[i] = rand.Next(1, N);
			}

			for (int i = 0; i < intArray.Length; i++)
			{
				hset.Add(intArray[i]);
				fset.Add(intArray[i]);
			}

			extraIntArray = new int[1000];
			for (int i = 0; i < extraIntArray.Length; i++)
			{
				extraIntArray[i] = rand.Next(1, N);
			}
		}

		[Benchmark(Baseline = true)]
		public void HashSet_AddExtra()
		{
			for (int i = 0; i < extraIntArray.Length; i++)
			{
				hset.Add(extraIntArray[i]);
			}
		}

		[Benchmark]
		public void FastHashSet_AddExtra()
		{
			for (int i = 0; i < extraIntArray.Length; i++)
			{
				fset.Add(extraIntArray[i]);
			}
		}
	}
}
