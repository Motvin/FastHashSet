//#define NotBench
using System;
using System.Text;
using SCG = System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Diagnosers;
using FastHashSet;
using FastHashSet2;
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

		[Benchmark]
		public void Test_Add2()
		{
			FastHashSet2<int> set = new FastHashSet2<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
		}
	}

	//[ClrJob(baseline: true)] //, CoreJob, MonoJob, CoreRtJob
	//[CsvMeasurementsExporter]
	//[RPlotExporter]
	[MinColumn, MaxColumn] //RankColumn
	//[HardwareCounters(HardwareCounter.CacheMisses)]
	[RyuJitX64Job] //LegacyJitX86Job
	//[MemoryDiagnoser]
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
		public void SCG_HashSet_Add()
		{
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
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
		public void FastHashSet_Add()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set.Add(intArray[i]);
			}
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

	public static class BenchUtil
	{
		// make sure the minInt/maxInt range is large enough for the length of the array so that the random #'s aren't mostly already taken
		public static void PopulateIntArray(int[] a, Random rand, int minInt, int maxInt, double uniqueValuePercent = 0)
		{
			if (uniqueValuePercent > 0)
			{
				int uniqueValuesCount = (int)(uniqueValuePercent * a.Length);
				if (uniqueValuesCount <= 0)
				{
					uniqueValuesCount = 1; // there must be at least one of these unique values
				}

				// first get all unique values in the uniqueValuesArray
				SCG.HashSet<int> h = new SCG.HashSet<int>();

				int cnt = 0;
				if (a.Length == uniqueValuesCount)
				{
					while (cnt < uniqueValuesCount)
					{
						int val = rand.Next(minInt, maxInt);
						if (h.Add(val))
						{
							a[cnt] = val;
							cnt++;
						}
					}
				}
				else
				{
					int[] uniqueValuesArray = new int[uniqueValuesCount];
					while (cnt < uniqueValuesCount)
					{
						int val = rand.Next(minInt, maxInt);
						if (h.Add(val))
						{
							uniqueValuesArray[cnt] = val;
							cnt++;
						}
					}

					PopulateIntArrayFromUniqueArray(a, rand, uniqueValuesArray, uniqueValuesCount);
				}
			}
			else
			{
				for (int i = 0; i < a.Length; i++)
				{
					a[i] = rand.Next(minInt, maxInt);
				}
			}
		}

		public static void PopulateIntArrayFromUniqueArray(int[] a, Random rand, int[] uniqueValuesArray, int uniqueValuesCount)
		{
			// randomly pick an index for each value and indicate that this index is used
			bool[] isUsed = new bool[a.Length];

			int maxIdx = a.Length - 1;
			for (int i = 0; i < uniqueValuesCount; i++)
			{
				int idx = rand.Next(0, maxIdx);
				a[idx] = uniqueValuesArray[i];
				isUsed[idx] = true;
			}

			// now loop through a and randomly pick a value from uniqueValuesArray
			maxIdx = uniqueValuesCount - 1;
			for (int i = 0; i < a.Length; i++)
			{
				if (isUsed[i] == false)
				{
					int idx = rand.Next(0, maxIdx);
					a[i] = uniqueValuesArray[idx];
				}
			}
		}
	}

	public class MinMaxIntRangeContains
	{
		public int[] a;
		public int[] c;

		public SCG.HashSet<int> h;
		public FastHashSet<int> f;

		public int addLen;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
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
		}

		[Benchmark(Baseline = true)]
		public void SCG_Contains()
		{
			for (int i = 0; i < c.Length; i++)
			{
				h.Contains(c[i]);
			}
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
		public void Fast_Contains()
		{
			for (int i = 0; i < c.Length; i++)
			{
				f.Contains(c[i]);
			}
		}

   //     [GlobalCleanup]
   //     public void GlobalCleanup()
   //     {
			//// assert that 
   //         // Disposing logic
   //     }
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

	[RyuJitX64Job]
	public class AddSmallClassVsStructFast
	{
		public SmallClass[] a;

		[Params(10, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10_000, 20_000, 30_000, 40_000, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 1_000_000, 10_000_000 )]// 100_000_000
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
		}

		[Benchmark(Baseline = true)]
		public void FastClass()
		{
			FastHashSet<SmallClass> set = new FastHashSet<SmallClass>();
			for (int i = 0; i < a.Length; i++)
			{
				SmallClass r = a[i];
				set.Add(new SmallClass(r.myInt, r.myInt2));
			}
		}

		[Benchmark]
		public void FastStruct()
		{
			FastHashSet<SmallStruct> set = new FastHashSet<SmallStruct>();
			for (int i = 0; i < a.Length; i++)
			{
				SmallClass r = a[i];
				set.Add(new SmallStruct(r.myInt, r.myInt2));
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

	public static class Str
	{
		public const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		public const string space = " ";
		public const string digits = "1234567890";
		public const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
		public const string symbols = "!@#$%^&*()_+-=[]{};':\",./<>?\\";

		public static string CreateRandomString(Random rand, int minLen, int maxLen, string[] freqArray)
		{
			int len = rand.Next(minLen, maxLen);

			StringBuilder sb = new StringBuilder(new string(' ', len));

			int maxFreq = freqArray.Length - 1;
			string s;
			for (int i = 0; i < len; i++)
			{
				if (maxFreq == 0)
				{
					s = freqArray[0];
				}
				else
				{
					int freq = rand.Next(0, maxFreq);
					s = freqArray[freq];
				}
				int n = rand.Next(0, s.Length - 1);

				sb[i] = s[n];
			}

			return sb.ToString();
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
			string[] strFreq = new string[] { Str.uppercaseChars };
			for (int i = 0; i < stringArray.Length; i++)
			{
				stringArray[i] = Str.CreateRandomString(rand, 6, 12, strFreq);
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
			string[] strFreq = new string[] { Str.uppercaseChars };
			for (int i = 0; i < stringArray.Length; i++)
			{
				stringArray[i] = Str.CreateRandomString(rand, 12, 30, strFreq);
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
			string[] strFreq = new string[] { Str.uppercaseChars };
			for (int i = 0; i < stringArray.Length; i++)
			{
				stringArray[i] = Str.CreateRandomString(rand, 30, 80, strFreq);
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
				Str.uppercaseChars,
				Str.uppercaseChars,
				Str.lowercaseChars,
				Str.lowercaseChars,
				Str.digits,
				Str.space,
				Str.symbols,
				};
			for (int i = 0; i < stringArray.Length; i++)
			{
				stringArray[i] = Str.CreateRandomString(rand, 30, 80, strFreq);
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


	class Program
	{
		static void Main(string[] args)
		{
#if NotBench
#if Test_Max_HashSetSize
			HashSet<int> set = null;
		
			try
			{
				HashSetBench b = new HashSetBench();
				b.N = 100_000_000;
				b.Setup();
				set = new HashSet<int>();
				for (int i = 0; i < b.intArray.Length; i++)
				{
					set.Add(b.intArray[i]);
				}

				//b.XXTestHashSet_Add();
				Console.WriteLine("Done---");
			}
			catch (Exception ex)
			{
				Console.WriteLine("SetCount = " + set.Count.ToString("N0"));
				Console.WriteLine(ex.ToString());
			}			
#endif
			FastHashSet<int> set = null;
			try
			{
				HashSetBench b = new HashSetBench();
				b.N = 3_000;
				b.Setup();
				set = new FastHashSet<int>();
				for (int i = 0; i < b.intArray.Length; i++)
				{
					set.Add(b.intArray[i]);
				}

				List<LevelAndCount> lst = set.GetChainLevelsCounts();

				foreach(LevelAndCount n in lst)
				{
					Console.WriteLine("Level: " + n.Level.ToString("N0") + ", Count: " + n.Count.ToString("N0"));
				}
				//b.XXTestHashSet_Add();
				Console.WriteLine("Count=" + set.Count.ToString("N0"));
				Console.WriteLine("Done---");
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
			Console.ReadKey();

#else
#if false
		ExtraInts_LargerRange t = new ExtraInts_LargerRange();
		t.N = 2000;
		t.SetupWithLargerRange();
		t.step = 1;
		t.TestFastHashSet_AddExtra();
		t.step = 2;
		t.TestFastHashSet_AddExtra();
		t.step = 3;
		t.TestFastHashSet_AddExtra();
#endif
#if false
			//int[] p2 = new int[] { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 65536 * 2, 65536 * 4, 65536 * 8, 65536 * 16, 65536 * 32,
			//65536 * 64, 65536 * 128, 65536 * 256, 65536 * 512, 65536 * 1024, 65536 * 2048, 65536 * 4096, 65536 * 8192, 65536 * 16384 };

			int[] p2 = new int[] { 19831, 40241, 84460 };


			int[] primes = new int[p2.Length];

			primes[0] = 1;
			ref int intRef = ref p2[0];

			intRef = ref primes[0];
			double loadFactor = 1.0;
			//double loadFactor = .75;
			int i = 0;
			foreach (int n in p2)
			{
				int thresh = n;
				int indexArrayLen = (int)(thresh / loadFactor);
				int usedItemsLoadFactorThreshold = (int)(loadFactor * (double)indexArrayLen);
				if (usedItemsLoadFactorThreshold < thresh)
				{
					indexArrayLen++;
				}
				primes[i++] = FastHashSet.FastHashSet<int>.GetEqualOrClosestHigherPrime(indexArrayLen);
			}
#endif

			//MinMaxIntRange r = new MinMaxIntRange();
			//r.N = 4_000;
			////r.N = 10_000_000;
			//r.Setup();
			//r.TestFastHashSet_Add();

			//FastHashSet<int> set = null;
			//try
			//{
			//	MinMaxIntRange b = new MinMaxIntRange();
			//	b.N = 3_000;
			//	b.Setup();
			//	set = new FastHashSet<int>();
			//	for (int i = 0; i < b.intArray.Length; i++)
			//	{
			//		set.Add(b.intArray[i]);
			//	}

			//	List<LevelAndCount> lst = set.GetChainLevelsCounts(out double avgNodeVisitPerChain);

			//	foreach (LevelAndCount n in lst)
			//	{
			//		Console.WriteLine("Level: " + n.Level.ToString("N0") + ", Count: " + n.Count.ToString("N0"));
			//	}
			//	//b.XXTestHashSet_Add();
			//	Console.WriteLine("Count=" + set.Count.ToString("N0"));
			//	Console.WriteLine("avgNodeVisitPerChain=" + avgNodeVisitPerChain.ToString("N4"));
			//	Console.WriteLine("Done---");
			//}
			//catch (Exception ex)
			//{
			//	Console.WriteLine(ex.ToString());
			//}
			//Console.ReadKey();

			//return;

			//Random rand = new Random(89);

			//const int HighBitNotSet = unchecked((int)0b0111_1111_1111_1111_1111_1111_1111_1111); //??? use this instead of the negate negative logic when getting hashindex

			//int rem;
			//int rem2;
			//int rem3;
			//int rem4;
			//int v;
			//const int modVal = 17;
			//for (int i = 0; i < 100; i++)
			//{
			//	int val = rand.Next(int.MinValue, -1);
			//	val = -(i + 1);

			//	rem = val % modVal;
			//	if (rem < 0)
			//	{
			//		rem = -rem;
			//	}

			//	rem2 = (val & HighBitNotSet) % modVal;

			//	v = (val & HighBitNotSet);

			//	rem3 = (-val) % modVal;

			//	rem4 = val % modVal;
			//	if (rem4 < 0)
			//	{
			//		rem4 += modVal;
			//	}
			//	int x = 23;
			//}

			//MinMaxIntRangeAddInConstructor t = new MinMaxIntRangeAddInConstructor();
			//t.N = 20000;
			//t.Setup();
			//t.FastHashSet_AddCon();			

			//MinMaxIntRange t = new MinMaxIntRange();
			//t.N = 10_000_000;
			//t.Setup();
			//t.FastHashSet_Add

#if false
			int N = 40_000_000;
			int[] intArray = new int[N];

			Random rand = new Random(89);

			// make sure all ints in the intArray are unique
			SCG.HashSet<int> h = new SCG.HashSet<int>();
			int x = 0;
			while (true)
			{
				int nextInt = rand.Next(int.MinValue, int.MaxValue);
				if (h.Add(nextInt))
				{
					intArray[x++] = nextInt;
					if (x == N)
					{
						break;
					}
				}
			}

			//1_395_263
			SCG.HashSet<int> set = new SCG.HashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				if (set.Count == 23997907)
				{
					int asd = 1;
				}
				set.Add(intArray[i]);
			}

			FastHashSet<int> set2 = new FastHashSet<int>();
			for (int i = 0; i < intArray.Length; i++)
			{
				set2.Add(intArray[i]);
			}
#endif

			//int N = 1_000_000;
			//int[] intArray = new int[N];

			//Random rand = new Random(89);

			//// make sure all ints in the intArray are unique
			//SCG.HashSet<int> h = new SCG.HashSet<int>();
			//int x = 0;
			//while (true)
			//{
			//	int nextInt = rand.Next(int.MinValue, int.MaxValue);
			//	if (h.Add(nextInt))
			//	{
			//		intArray[x++] = nextInt;
			//		if (x == N)
			//		{
			//			break;
			//		}
			//	}
			//}
			//SCG.HashSet<int> set = new SCG.HashSet<int>(intArray);
			//FastHashSet<int> set2 = new FastHashSet<int>(intArray);

			//SmallStringUpperCase s = new SmallStringUpperCase();
			//s.N = 10;
			//s.Setup();
			//BenchmarkDotNet.Reports.Summary summary;
			//var summary = BenchmarkRunner.Run<LargeStringMixed>();
			//var summary = BenchmarkRunner.Run<AddMediumStruct>();
			//var summary = BenchmarkRunner.Run<AddLargeStruct>();
			//var summary = BenchmarkRunner.Run<MinMaxIntRangeAddInConstructor>();
			//var summary = BenchmarkRunner.Run<MinMaxIntRange>();
			//var summary = BenchmarkRunner.Run<SmallerIntRange>();
			//var summary2 = BenchmarkRunner.Run<SmallerIntRange>();
			//var summary3 = BenchmarkRunner.Run<ExtraInts_LargerRange>();
			//var summary4 = BenchmarkRunner.Run<ExtraInts_SmallerRange>();

			//var summary = BenchmarkRunner.Run<AddMediumStruct>();

			//AddMediumClassVsStructFastWithIn s = new AddMediumClassVsStructFastWithIn();
			//s.N = 100;
			//s.Setup();
			//s.FastStruct();
			//var summary2 = BenchmarkRunner.Run<AddMediumClass>();

			//var summary = BenchmarkRunner.Run<AddLargeStruct>();
			//var summary2 = BenchmarkRunner.Run<AddLargeClass>();

			//var summary = BenchmarkRunner.Run<MinMaxIntRangeContains>();
			//var summary = BenchmarkRunner.Run<PositiveIntRangeAdd10PctUnique>();
			//var summary = BenchmarkRunner.Run<PositiveIntRangeAddRemoveAdd>();

			//int sss = (int)(23 * .75);
			//int tst = (int)(16 / .75);
			//int prim = FastHashSet<int>.GetEqualOrClosestHigherPrime(tst);

			var summary = BenchmarkRunner.Run<PosIntRangeTo100>();
			var summary2 = BenchmarkRunner.Run<PosIntRangeTo1000>();
			var summary3 = BenchmarkRunner.Run<PosIntRangeTo10_000>();
			var summary4 = BenchmarkRunner.Run<PosIntRangeTo100_000>();
			var summary5 = BenchmarkRunner.Run<PosIntRangeTo1_000_000>();
			var summary6 = BenchmarkRunner.Run<PosIntRangeTo10_000_000>();

			//summary = BenchmarkRunner.Run<AddMediumClassInCon>();
			//summary = BenchmarkRunner.Run<AddLargeClassInCon>();
#endif
		}
	}

	// this should be 8 bytes in size
	public struct SmallStruct : IEquatable<SmallStruct>
	{
		public int myInt;
		public int myInt2;

		public SmallStruct(int i, int i2)
		{
			myInt = i;
			myInt2 = i2;
		}

		public static SmallStruct CreateRand(Random rand)
		{
			int i = rand.Next(); // make this non-negative
			int i2 = rand.Next(); // make this non-negative

			return new SmallStruct(i, i2);
		}

		public override int GetHashCode()
		{
			int hash = 13;

			unchecked // the code below may overflow the hash int and that will cause an exception if compiler is checking for arithmetic overflow - unchecked prevents this
			{
				hash = (hash * 7) + myInt;
				hash = (hash * 7) + myInt2;
			}

			return hash;
		}

		public override bool Equals(object obj)
		{
			SmallStruct c = (SmallStruct)obj;
			return Equals(c);
		}

		public bool Equals(SmallStruct other)
		{
			return (myInt == other.myInt && myInt2 == other.myInt2);
		}

        public static bool operator ==(SmallStruct c1, SmallStruct c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(SmallStruct c1, SmallStruct c2)
        {
            return !(c1 == c2);
        }
	}

	public sealed class SmallClass : IEquatable<SmallClass>
	{
		public int myInt;
		public int myInt2;

		public SmallClass(int i, int i2)
		{
			myInt = i;
			myInt2 = i2;
		}

		public static SmallClass CreateRand(Random rand)
		{
			int i = rand.Next(); // make this non-negative
			int i2 = rand.Next(); // make this non-negative

			return new SmallClass(i, i2);
		}

		public override int GetHashCode()
		{
			int hash = 13;

			unchecked // the code below may overflow the hash int and that will cause an exception if compiler is checking for arithmetic overflow - unchecked prevents this
			{
				hash = (hash * 7) + myInt;
				hash = (hash * 7) + myInt2;
			}

			return hash;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			SmallClass c = (SmallClass)obj;
			return Equals(c);
		}

		public bool Equals(SmallClass other)
		{
			if (other == null)
			{
				return false;
			}

			return (myInt == other.myInt && myInt2 == other.myInt2);
		}

        public static bool operator ==(SmallClass c1, SmallClass c2)
        {
 			if (c1 is null && c2 is null)
			{
				return true;
			}
			else if (c1 is null)
			{
				return false;
			}

           return c1.Equals(c2);
        }

        public static bool operator !=(SmallClass c1, SmallClass c2)
        {
            return !(c1 == c2);
        }
	}

	// this should be about 20 bytes in size (assuming DateTime is 8 bytes)
	public struct MediumStruct : IEquatable<MediumStruct>
	{
		public DateTime myDate;
		public double myDouble;
		public int myInt;

		public MediumStruct(DateTime dt, double d, int i)
		{
			myDate = dt;
			myDouble = d;
			myInt = i;
		}

		public static MediumStruct CreateRand(Random rand)
		{
			double d = rand.NextDouble();
			int i = rand.Next(); // make this non-negative
			int year = rand.Next(1990, 2019);
			int month = rand.Next(1, 12);
			int day = rand.Next(1, 28);

			return new MediumStruct(new DateTime(year, month, day), d, i);
		}

		public override int GetHashCode()
		{
			int hash = 13;

			unchecked // the code below may overflow the hash int and that will cause an exception if compiler is checking for arithmetic overflow - unchecked prevents this
			{
				hash = (hash * 7) + myInt;
				hash = (hash * 7) + myDouble.GetHashCode();
				hash = (hash * 7) + myDate.GetHashCode();
			}

			return hash;
		}

		public override bool Equals(object obj)
		{
			MediumStruct c = (MediumStruct)obj;
			return Equals(c);
		}

		public bool Equals(MediumStruct other)
		{
			return (myInt == other.myInt && myDouble == other.myDouble && myDate == other.myDate);

		}

        public static bool operator ==(MediumStruct c1, MediumStruct c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(MediumStruct c1, MediumStruct c2)
        {
            return !(c1 == c2);
        }
	}

	public sealed class MediumClass : IEquatable<MediumClass>
	{
		public DateTime myDate;
		public double myDouble;
		public int myInt;

		public MediumClass(DateTime dt, double d, int i)
		{
			myDate = dt;
			myDouble = d;
			myInt = i;
		}

		public static MediumClass CreateRand(Random rand)
		{
			double d = rand.NextDouble();
			int i = rand.Next(); // make this non-negative
			int year = rand.Next(1990, 2019);
			int month = rand.Next(1, 12);
			int day = rand.Next(1, 28);

			return new MediumClass(new DateTime(year, month, day), d, i);
		}

		public override int GetHashCode()
		{
			int hash = 13;

			unchecked // the code below may overflow the hash int and that will cause an exception if compiler is checking for arithmetic overflow - unchecked prevents this
			{
				hash = (hash * 7) + myInt;
				hash = (hash * 7) + myDouble.GetHashCode();
				hash = (hash * 7) + myDate.GetHashCode();
			}

			return hash;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			MediumClass c = (MediumClass)obj;
			return Equals(c);
		}

		public bool Equals(MediumClass other)
		{
			if (other == null)
			{
				return false;
			}

			return (myInt == other.myInt && myDouble == other.myDouble && myDate == other.myDate);
		}

        public static bool operator ==(MediumClass c1, MediumClass c2)
        {
 			if (c1 is null && c2 is null)
			{
				return true;
			}
			else if (c1 is null)
			{
				return false;
			}

           return c1.Equals(c2);
        }

        public static bool operator !=(MediumClass c1, MediumClass c2)
        {
            return !(c1 == c2);
        }
	}

	// this should be about 40 bytes, not including the space for the actual string bytes
	public struct LargeStruct : IEquatable<LargeStruct>
	{
		public DateTime myDate;
		public double myDouble;
		public double myDouble2;
		public int myInt;
		public int myInt2;
		public int myInt3;
		public string myString;

		public LargeStruct(DateTime dt, double d, double d2, int i, int i2, int i3, string s)
		{
			myDate = dt;
			myDouble = d;
			myDouble2 = d2;
			myInt = i;
			myInt2 = i2;
			myInt3 = i3;
			myString = s;
		}

		public static LargeStruct CreateRand(Random rand)
		{
			double d = rand.NextDouble();
			double d2 = rand.NextDouble();
			int i = rand.Next(); // make this non-negative
			int i2 = rand.Next(); // make this non-negative
			int i3 = rand.Next(); // make this non-negative
			int year = rand.Next(1990, 2019);
			int month = rand.Next(1, 12);
			int day = rand.Next(1, 28);

			string[] strFreq = new string[] { 
				Str.uppercaseChars,
				Str.uppercaseChars,
				Str.lowercaseChars,
				Str.lowercaseChars,
				Str.digits,
				Str.space,
				Str.symbols,
				};
			string s = Str.CreateRandomString(rand, 10, 12, strFreq);

			return new LargeStruct(new DateTime(year, month, day), d, d2, i, i2, i3, s);
		}

		public override int GetHashCode()
		{
			int hash = 13;

			unchecked // the code below may overflow the hash int and that will cause an exception if compiler is checking for arithmetic overflow - unchecked prevents this
			{
				hash = (hash * 7) + myInt;
				hash = (hash * 7) + myInt2;
				hash = (hash * 7) + myInt3;
				hash = (hash * 7) + myDouble.GetHashCode();
				hash = (hash * 7) + myDouble2.GetHashCode();
				hash = (hash * 7) + myDate.GetHashCode();
				hash = (hash * 7) + myString.GetHashCode();
			}

			return hash;
		}

		public override bool Equals(object obj)
		{
			LargeStruct c = (LargeStruct)obj;
			return Equals(c);
		}

		public bool Equals(LargeStruct other)
		{
			return (myInt == other.myInt && myInt2 == other.myInt2 && myInt3 == other.myInt3 && myDouble == other.myDouble && myDouble2 == other.myDouble2 && myDate == other.myDate && myString == other.myString);
		}

        public static bool operator ==(LargeStruct c1, LargeStruct c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(LargeStruct c1, LargeStruct c2)
        {
            return !(c1 == c2);
        }
	}

	// this should be about 40 bytes, not including the space for the actual string bytes
	public sealed class LargeClass : IEquatable<LargeClass>
	{
		public DateTime myDate;
		public double myDouble;
		public double myDouble2;
		public int myInt;
		public int myInt2;
		public int myInt3;
		public string myString;

		public LargeClass(DateTime dt, double d, double d2, int i, int i2, int i3, string s)
		{
			myDate = dt;
			myDouble = d;
			myDouble2 = d2;
			myInt = i;
			myInt2 = i2;
			myInt3 = i3;
			myString = s;
		}

		public static LargeClass CreateRand(Random rand)
		{
			double d = rand.NextDouble();
			double d2 = rand.NextDouble();
			int i = rand.Next(); // make this non-negative
			int i2 = rand.Next(); // make this non-negative
			int i3 = rand.Next(); // make this non-negative
			int year = rand.Next(1990, 2019);
			int month = rand.Next(1, 12);
			int day = rand.Next(1, 28);

			string[] strFreq = new string[] { 
				Str.uppercaseChars,
				Str.uppercaseChars,
				Str.lowercaseChars,
				Str.lowercaseChars,
				Str.digits,
				Str.space,
				Str.symbols,
				};
			string s = Str.CreateRandomString(rand, 10, 12, strFreq);

			return new LargeClass(new DateTime(year, month, day), d, d2, i, i2, i3, s);
		}

		public override int GetHashCode()
		{
			int hash = 13;

			unchecked // the code below may overflow the hash int and that will cause an exception if compiler is checking for arithmetic overflow - unchecked prevents this
			{
				hash = (hash * 7) + myInt;
				hash = (hash * 7) + myInt2;
				hash = (hash * 7) + myInt3;
				hash = (hash * 7) + myDouble.GetHashCode();
				hash = (hash * 7) + myDouble2.GetHashCode();
				hash = (hash * 7) + myDate.GetHashCode();
				hash = (hash * 7) + myString.GetHashCode();
			}

			return hash;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			LargeStruct c = (LargeStruct)obj;
			return Equals(c);
		}

		public bool Equals(LargeClass other)
		{
			if (other == null)
			{
				return false;
			}

			return (myInt == other.myInt && myInt2 == other.myInt2 && myInt3 == other.myInt3 && myDouble == other.myDouble && myDouble2 == other.myDouble2 && myDate == other.myDate && myString == other.myString);
		}

        public static bool operator ==(LargeClass c1, LargeClass c2)
        {
 			if (c1 is null && c2 is null)
			{
				return true;
			}
			else if (c1 is null)
			{
				return false;
			}

            return c1.Equals(c2);
        }

        public static bool operator !=(LargeClass c1, LargeClass c2)
        {
            return !(c1 == c2);
        }
	}
}
