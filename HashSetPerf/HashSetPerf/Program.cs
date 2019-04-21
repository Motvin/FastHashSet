using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.CompilerServices;

using SCG = System.Collections.Generic;
using Motvin.Collections;
using Perf;

namespace HashSetPerf
{
	class Program
	{
		static void Main(string[] args)
		{
			//string outputFileName = @"e:\\proj\\summary.tsv";
			//int minN = 100_000;
			//int maxN = 1_000_000;

			//int incrementNBy = 10_000;
			string errMsg = PerfUtil.GetCmdLineParams_OutputFileAndMinMaxIncN(args, out int minN, out int maxN, out int incrementNBy, out string outputFileName);

			int nCount = ((maxN - minN) / incrementNBy) + 1;
			int[] nArray = new int[nCount];

			int idx = 0;
			for (int n = minN; n <= maxN; n += incrementNBy, idx++)
			{
				nArray[idx] = n;
			}

			const int LoopUnrollCount = 1;
			const int IterartionCount = 512;
			const int IterartionWarmupCount = 16;

			long [] ticksH = new long[nArray.Length * IterartionCount * LoopUnrollCount];
			int ticksIdxForH = 0;

			long [] ticksF = new long[nArray.Length * IterartionCount * LoopUnrollCount];
			int ticksIdxForF = 0;

			long [] ticksC = new long[nArray.Length * IterartionCount * LoopUnrollCount];
			int ticksIdxForC = 0;

			long startTicks;

			double overheadTicks = PerfUtil.GetTimestampOverheadInNanoSeconds();

			int[] a;
			int[] c;

			SCG.HashSet<int> h = new HashSet<int>();
			FastHashSet<int> f = new FastHashSet<int>();
			C5.HashSet<int> c5 = new C5.HashSet<int>();

			HashSetBench.BenchUtil.PopulateCollections25_25_50PctUnique(maxN, out a, out c, h, f, c5);

			// not sure if we should run bechmark 1 and then benchmark 2 separately so that the presence of the one doesn't effect the other???
			// in practice they will probably not be run together one after the other

			PerfUtil.DoGCCollect();

			int N;
			for (int j = 0; j < nArray.Length; j++)
			{
				N = nArray[j];

				// not really sure what running the warmup really does - it can put things in the cache that maybe shouldn't be there because they won't be in a real application???
				// still, if we execute the same code with the same data in a loop alot of times, this will populate the cache unrealistically
				// also if we do a warmup, the jit times will be removed, but does this represent reality - jit times do happen in real running code???

				for (int iterationIdx = 0; iterationIdx < IterartionWarmupCount; iterationIdx++)
				{
					// SCG_Contains
					for (int i = 0; i < N; i++)
					{
						h.Contains(c[i]);
					}
		
					// Fast_Contains
					for (int i = 0; i < N; i++)
					{
						f.Contains(c[i]);
					}

					for (int i = 0; i < N; i++)
					{
						c5.Contains(c[i]);
					}
				}

				for (int iterationIdx = 0; iterationIdx < IterartionCount; iterationIdx++)
				{
					// to minimize the effects of the loop code on the count, unroll each bechmark 2 times
					// also alternate randomly between the order of these to minimize any effects of order
					// not sure what effects loop unrolling has since that part isn't contained in the stopwatch time
					// still there might be some residual effects on CPU registers? - not really sure

					// 1

					// there is some overhead that should be removed - it is returning from GetTimestamp and setting startTicks and afterwards calling GetTimestamp until the point where the return value is obtained
					// we should determine this overhead by calling 
					startTicks = Stopwatch.GetTimestamp();
					for (int i = 0; i < N; i++)
					{
						h.Contains(c[i]);
					}
					ticksH[ticksIdxForH++] = Stopwatch.GetTimestamp() - startTicks;
		
					startTicks = Stopwatch.GetTimestamp();
					for (int i = 0; i < N; i++)
					{
						f.Contains(c[i]);
					}
					ticksF[ticksIdxForF++] = Stopwatch.GetTimestamp() - startTicks;
		
					startTicks = Stopwatch.GetTimestamp();
					for (int i = 0; i < N; i++)
					{
						c5.Contains(c[i]);
					}
					ticksC[ticksIdxForC++] = Stopwatch.GetTimestamp() - startTicks;
				}
			}

			// summarize and output the data

			BenchmarkSummaries summaries = new BenchmarkSummaries();

			summaries.AddNSummaryList(NSummary.CreateNSummaryListForBenchmark(overheadTicks, nArray, IterartionCount * LoopUnrollCount, ticksH), "SCG_Contains");

			summaries.AddNSummaryList(NSummary.CreateNSummaryListForBenchmark(overheadTicks, nArray, IterartionCount * LoopUnrollCount, ticksF), "Fast_Contains");

			summaries.AddNSummaryList(NSummary.CreateNSummaryListForBenchmark(overheadTicks, nArray, IterartionCount * LoopUnrollCount, ticksC), "C5_Contains");

			summaries.OutputSummariesToFile(outputFileName, "SCG_Contains");
		}
	}

	public static class Perf
	{
	}


  //  public class Timings
  //  {
		//private long[] timingsArray;
		//private int timingsIdx;

		//[MethodImpl(MethodImplOptions.AggressiveInlining)] //??? not sure this should be used
		//public Timings(int timingsCount)
		//{
		//	timingsArray = new long[timingsCount];
		//}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public void Start(Stopwatch sw)
		//{
		//	sw.Restart();
		//}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public void Stop(Stopwatch sw, bool getElapsedTime = true)
		//{
		//	sw.Stop();
		//	if (getElapsedTime)
		//	{
		//		timingsArray[timingsIdx++] = sw.ElapsedTicks;
		//	}
		//}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public void StopAndRestart(Stopwatch sw, bool getElapsedTime = true)
		//{
		//	if (getElapsedTime)
		//	{
		//		sw.Stop();
		//		timingsArray[timingsIdx++] = sw.ElapsedTicks;
		//	}
		//	sw.Restart();
		//}

		//public void Clear()
		//{
		//	Array
		//}

		//public static void WriteSummaryToFile(string fileName, Timings[] timimngsPerBechmarkArray, string[] benchmarkNameArray)
		//{
		//	foreach ()
		//	perfArray
  //          long ticksPerSec = Stopwatch.Frequency;
  //          long nanoSecPerTick = (1000L * 1000L * 1000L) / ticksPerSec;

		//}
  //  }

}
