using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using SCG = System.Collections.Generic;
using Perf;

namespace HashSetContains
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

				int[] a;
				int[] c;

				HashSet<int> set = new HashSet<int>();

				BenchUtil.PopulateArrays25_25_50PctUnique(maxN, out a, out c);

				// in a real world scenario, we will have probably have recently added the items into the set, so no need to try to clear the cache or anything
				for (int i = 0; i < maxN; i++)
				{
					set.Add(a[i]);
				}

				double overheadNanoSecs = PerfUtil.GetTimestampOverheadInNanoSeconds();

				PerfUtil.DoGCCollect();

				int iterations = 1;
				long startTicks;
				long endTicks;
				double ticks;

				// this is enough to jit things and not put everything in the cache
				bool isContained = set.Contains(0);

				if (maxN <= 1000)
				{
					iterations = 1;

					// there amount of time taken for these is too small to measure just one iteration - so we measure multiple iterations in a loop and get the time for these
					// the mean time is this total time / iterations

					// for really small operations, like a single contains on a hashet that has 8 items you would probably want to call at least a feww hundred Contains
					// and then average that - the maxN wouldn't work too well if that was just 8

					startTicks = Stopwatch.GetTimestamp();

					for (int i = 0; i < maxN; i++)
					{
						set.Contains(c[i]);
					}

					endTicks = Stopwatch.GetTimestamp();

					ticks = ((endTicks - startTicks) * n) / (double)maxN;
				}
				else
				{
					iterations = 1;

					startTicks = Stopwatch.GetTimestamp();
					for (int i = 0; i < n; i++) // loop overhead is ok because we assume there will be some loop in real-world scenario
					{
						set.Contains(c[i]);
					}
					endTicks = Stopwatch.GetTimestamp();

					ticks = (double)(endTicks - startTicks);
				}

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
}
