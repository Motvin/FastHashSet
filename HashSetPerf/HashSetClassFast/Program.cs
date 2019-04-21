using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Perf;
using Motvin.Collections;


namespace HashSetClassFast
{
	class Program
	{
		static void Main(string[] args)
		{
			// cmd line params variables
			string dbConnStr = null;
			int runID = 0;
			int benchmarkMethodID = 0;
			int n;
			int maxN;

			try
			{
				DateTime startTime = DateTime.Now;
				//Console.WriteLine($"Args Count:" + args.Length.ToString());
				//foreach (string s in args)
				//{
				//	Console.WriteLine(s);
				//}
				//Console.ReadKey();

				string errMsg = PerfUtil.GetCmdLineParams_DbNAndMaxN(args, out dbConnStr, out runID, out benchmarkMethodID, out n, out maxN);
				//if (errMsg != null)
				//{
				//	Console.WriteLine(errMsg);
				//}
				//Console.WriteLine($"Args: {dbConnStr}; {runID.ToString()}; {benchmarkMethodID.ToString()}; {n.ToString()}; {maxN.ToString()}");
				//Console.ReadKey();

				int[] a = new int[n];
				int[] a2 = new int[n];

				Random rand = new Random(89);
				for (int i = 0; i < a.Length; i++)
				{
					a[i] = rand.Next();
					a2[i] = rand.Next();
				}

				FastHashSet<SmallClass> setWarmup = new FastHashSet<SmallClass>();
				setWarmup.Add(new SmallClass(1, 2));

				FastHashSet<SmallClass> set = new FastHashSet<SmallClass>();

				double overheadNanoSecs = PerfUtil.GetTimestampOverheadInNanoSeconds();

				PerfUtil.DoGCCollect();

				int iterations = 1;
				long startTicks;
				long endTicks;
				double ticks;

				// this is enough to jit things and not put everything in the cache
				//bool isContained = set.Contains(0);

				iterations = 1;

				//SmallClass sc = new SmallClass(a[0], a2[0]);
				startTicks = Stopwatch.GetTimestamp();
				for (int i = 0; i < a.Length; i++)
				{
					set.Add(new SmallClass(a[i], a2[i]));
				}
				//	set.Add(sc);

				endTicks = Stopwatch.GetTimestamp();

				ticks = (double)(endTicks - startTicks);

				double nanoSecs = PerfUtil.GetNanoSecondsFromTicks(ticks, Stopwatch.Frequency) - overheadNanoSecs;

				PerfDb.InsertMeasurement(dbConnStr, runID, benchmarkMethodID, n, iterations, nanoSecs, startTime, DateTime.Now);

			}
			catch (Exception ex)
			{
				Console.Write(ex.ToString());
				if (!string.IsNullOrEmpty(dbConnStr))
				{
					// write error to db
					PerfDb.InsertRunError(dbConnStr, runID, benchmarkMethodID, ex);
				}
				else
				{
					// log error to file
				}
			}
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

			return Equals(obj as SmallClass);
		}

		public bool Equals(SmallClass other)
		{
			if (other == null)
			{
				return false;
			}

			return (myInt == other.myInt && myInt2 == other.myInt2);
		}
	}


}
