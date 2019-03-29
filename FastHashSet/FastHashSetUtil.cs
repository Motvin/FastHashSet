#define Include_FastHashSet_Util_Functions

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//??? the namespace shouldn't be the same as the class - lookup the rules for namespace naming
namespace FastHashSet
{
	// static utility functions for debugging/information/statistics about FastHashSet only
#if Include_FastHashSet_Util_Functions
	public struct LevelAndCount : IComparable<LevelAndCount>
	{
		public LevelAndCount(int level, int count)
		{
			this.Level = level;
			this.Count = count;
		}

		public int Level;
		public int Count;

		public int CompareTo(LevelAndCount other)
		{
			return Level.CompareTo(other.Level);
		}
	}

	public partial class FastHashSet<T>
	{
		public List<LevelAndCount> GetChainLevelsCounts(out double avgNodeVisitPerChain)
		{
			Dictionary<int, int> itemsInChainToCountDict = new Dictionary<int, int>();

			// this function only makes sense when hashing
			int chainCount = 0;
			if (buckets != null)
			{
				for (int i = 0; i < buckets.Length; i++)
				{
					int index = buckets[i];
					if (index != NullIndex)
					{
						chainCount++;
						int itemsInChain = 1;

						while (slots[index].nextIndex != NullIndex)
						{
							index = slots[index].nextIndex;
							itemsInChain++;
						}

						itemsInChainToCountDict.TryGetValue(itemsInChain, out int cnt);
						cnt++;
						itemsInChainToCountDict[itemsInChain] = cnt;
					}
				}
			}

			double totalAvgNodeVisitsIfVisitingAllChains = 0;
			List<LevelAndCount> lst = new List<LevelAndCount>(itemsInChainToCountDict.Count);
			foreach (KeyValuePair<int, int> keyVal in itemsInChainToCountDict)
			{
				lst.Add(new LevelAndCount(keyVal.Key, keyVal.Value));
				if (keyVal.Key == 1)
				{
					totalAvgNodeVisitsIfVisitingAllChains += keyVal.Value;
				}
				else
				{
					totalAvgNodeVisitsIfVisitingAllChains += keyVal.Value * (keyVal.Key + 1.0) / 2.0;
				}
			}

			avgNodeVisitPerChain = totalAvgNodeVisitsIfVisitingAllChains / chainCount;

			lst.Sort();

			return lst;
		}
		
		public void ReorderChainedNodesToBeAdjacent()
		{
			if (slots != null)
			{
				TNode[] newSlotsArray = new TNode[slots.Length];

				// copy elements using the buckets array chains so there is better locality in the chains
				int index;
				int nextIndex;
				int newIndex = 0;
				for (int i = 0; i < buckets.Length; i++)
				{
					index = buckets[i];
					if (index != NullIndex)
					{
						buckets[i] = newIndex + 1;
						while (true)
						{
							newIndex++;
							ref TNode t = ref slots[index];
							ref TNode tNew = ref newSlotsArray[newIndex];
							nextIndex = t.nextIndex;

							// copy
							tNew.hashOrNextIndexForBlanks = t.hashOrNextIndexForBlanks;
							tNew.item = t.item;
							if (nextIndex == NullIndex)
							{
								tNew.nextIndex = NullIndex;
								break;
							}
							tNew.nextIndex = newIndex + 1;
							index = nextIndex;
						}
					}
				}
				newIndex++;
				nextBlankIndex = newIndex;
				firstBlankAtEndIndex = newIndex;
				slots = newSlotsArray;
			}
		}
	}
#endif

#if DEBUG
	public static class DebugOutput
	{
		public static void OutputEnumerableItems<T>(IEnumerable<T> e, string enumerableName)
		{
			System.Diagnostics.Debug.WriteLine("---start items: " + enumerableName + "---");
			int count = 0;
			foreach (T item in e)
			{
				System.Diagnostics.Debug.WriteLine(item.ToString());
				count++;
			}
			System.Diagnostics.Debug.WriteLine("---end items: " + enumerableName + "; count = " + count.ToString("N0") + "---");
		}

		public static void OutputSortedEnumerableItems<T>(IEnumerable<T> e, string enumerableName)
		{
			List<T> lst = new List<T>(e);
			lst.Sort();
			System.Diagnostics.Debug.WriteLine("---start items (sorted): " + enumerableName + "---");
			int count = 0;
			foreach (T item in lst)
			{
				System.Diagnostics.Debug.WriteLine(item.ToString());
				count++;
			}
			System.Diagnostics.Debug.WriteLine("---end items: " + enumerableName + "; count = " + count.ToString("N0") + "---");
		}
	}
#endif

}
