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
			if (isHashing)
			{
				for (int i = 0; i < slots.Length; i++)
				{
					int index = slots[i];
					if (index != NullIndex)
					{
						chainCount++;
						int itemsInChain = 1;

						while (buckets[index].nextIndex != NullIndex)
						{
							index = buckets[index].nextIndex;
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

		public void CreateNodeArrayWithAdjacentChainedNodes()
		{
			if (isHashing)
			{
				TNode[] newNodeArray = new TNode[buckets.Length];

				// copy elements using the indexArray chains so there is better locality in the chains
				int index;
				int nextIndex;
				int newIndex = 0;
				for (int i = 0; i < slots.Length; i++)
				{
					index = slots[i];
					if (index != NullIndex)
					{
						slots[i] = newIndex + 1;
						while (true)
						{
							newIndex++;
							ref TNode t = ref buckets[index];
							ref TNode tNew = ref newNodeArray[newIndex];
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
				buckets = newNodeArray;
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

#if false
//#define Exclude_Comparing_Cached_Hash
//#define Exclude_Check_For_Set_Modifications_In_Enumerator
//#define Exclude_Check_For_Is_Disposed_In_Enumerator
//#define Use_Primes_Above_PowersOf2_For_IndexArray_Length
#define Add_Item_To_Front_Of_Chain

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace FastHashSet
{
	public partial class FastHashSet<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ISet<T> //, ISerializable, IDeserializationCallback - ??? implement this within #define
    {
		private const int InitialItemsTableSize = 16;

		private const int LargestPrimeLessThanMaxInt = 2147483629;

		// if a node's nextIndex = this value, then it is a blank node - this isn't a valid nextIndex when unmarked and also when marked (because we don't allow int.MaxValue items)
		private const int BlankNextIndexIndicator = unchecked((int)0b1111_1111_1111_1111_1111_1111_1111_1111);

		private const int HighBitNotSet = unchecked((int)0b0111_1111_1111_1111_1111_1111_1111_1111); //??? use this instead of the negate negative logic when getting hashindex - this saves an if (hashindex < 0) which can have bad branch prediction

		private const int MarkNextIndexBitMask = unchecked((int)0b1000_0000_0000_0000_0000_0000_0000_0000); // doing an | (bitwise or) with this and the nextIndex marks the node, setting the bit back will give the original nextIndex value since
		
		private const int MarkNextIndexBitMaskInverted = ~MarkNextIndexBitMask; // doing an & (bitwise and) with this sets it back to the original value

		// what if someone wants less than double the # of items allocated next time??? - need to get a prime between 2 numbers in this list? - just let them specify the number, which maybe should be a prime
#if Use_Primes_Above_PowersOf2_For_IndexArray_Length
		private static readonly int[] indexArraySizeArray = { 7, 17, 37, 79, 163, 331, 673, 1361, 2729, 5471, 10_949, 21_911, 43_853, 87_719, 175_447,
			350_899, 701_819, 1_403_641, 2_807_303, 5_614_657, 11_229_331, 22_458_671, 44_917_381, 89_834_777, 179_669_557, 359_339_171, 718_678_369, 1_437_356_741, LargestPrimeLessThanMaxInt /* don't use int.MaxValue (2147483647), because some index values might go 2 past the size of arrays and we don't want them to roll over to negative values */ };
#else
		// these are primes above the .75 loadfactor of the power of 2
		private static readonly int[] indexArraySizeArray = { 7, 17, 47, 89, 173, 347, 691, 1367, 2741, 5471, 10_937, 21_851, 43_711, 87_383, 174_767,
			349_529, 699_053, 1_398_107, 2_796_221, 5_592_407, 11_184_829, 22_369_661, 44_739_259, 89_478_503, 17_8956_983, 35_7913_951, 715_827_947, 143_1655_777, LargestPrimeLessThanMaxInt};

		// primes above power of 2
		//private static readonly int[] indexArraySizeArray = { 7, 17, 37, 67, 131, 257, 521, 1031, 2053, 4099, 8209, 16411, 32771, 65537, 131101, 262147, 524309, 1048583,
		//	2097169, 4194319, 8388617, 16777259, 33554467, 67108879, 134217757, 268435459, 536870923, 1073741827, LargestPrimeLessThanMaxInt};
#endif
		private int currentIndexIntoSizeArray;

		// when an initial capacity is selected in the constructor or later, allocate the required space for indexArray, but
		// limit the # of used elements to optimize for cpu caches
		private static readonly int[] indexArraySizeArrayForCacheOptimization = { 1361, 87719, 701819 };

		private enum FoundType
		{
			FoundFirstTime,
			FoundNotFirstTime,
			NotFound
		}

		// are having private backing variables faster to access than properties???
		// One argument I’ve heard for using fields over properties is that “fields are faster”, but for trivial properties that’s actually not true, as the CLR’s Just-In-Time (JIT) compiler will inline
		// the property access and generate code that’s as efficient as accessing a field directly.
		// But this suggests that the JIT will have to do extra work (cpu time) to do this?

		#if !Exclude_Check_For_Set_Modifications_In_Enumerator
		private int incrementForEverySetModification;
		#endif

		private bool isHashing; // start out just using the itemsTable as an array without hashing

		private int usedItemsCount;

		private double loadFactor = .75;

		private int usedItemsLoadFactorThreshold;

		private int nextBlankIndex;

		private int firstBlankAtEndIndex; // this is needed because if items are removed, they get added into the blank list starting at nextBlankIndex, but we may want to TrimExcess capacity, so this is a quick way to see what the ExcessCapacity is

		private IEqualityComparer<T> comparer;

		private int[] indexArray; // make the index table a primary number to make the mod function less predictable - use a constant array of prime numbers

		private TNode[] nodeArray;

		private T[] initialArray;

		//private int indexArraySize; // this could be the stored size of the array because it is used every time we lookup any item and it might be faster than always getting indexArray.Length
		//private int nodeArraySize;

		private struct TNode
		{
			// putting these variables together (instead of having a separate index array - or an extension of the indexTable array) makes them close in memory, which might make things faster with cpu caching

			public int hashOrNextIndexForBlanks; // the cached hash code of the item - this is so we don't have to call GetHashCode multiple times, also doubles as a nextIndex for blanks, since blanks don't need a hash code anymore

			public int nextIndex;

			public T item;

			public TNode(T elem, int nextIndex, int hash)
			{
				this.item = elem;

				this.nextIndex = nextIndex;

				this.hashOrNextIndexForBlanks = hash;
			}
		}

		// Note: instead of blindly allocating double the # of items each time
		// have a way to specify the # of items that should be allocated next time
		// this is useful if the code knows something, ex. If adding dates from 1950 to 2019 in order we know if we are in 2018 that there can't be many more dates
		// so allocating 2x the amount would be a waste, the program could (at every year of processing, set the allocation to 366 * years remaining to be processed or something like that) -
		// this would be better than 2x if there were alot of dates (or dates and something else being the value of each item)

		// 1 - same constructor params as HashSet
		public FastHashSet()
		{
			comparer = EqualityComparer<T>.Default;
			CreateInitialArray(InitialItemsTableSize);
		}

		// 2 - same constructor params as HashSet
		public FastHashSet(IEnumerable<T> collection)
		{
			comparer = EqualityComparer<T>.Default;
			AddInitialEnumerable(collection);
		}

		// 3 - same constructor params as HashSet
		public FastHashSet(IEqualityComparer<T> comparer)
		{
			this.comparer = comparer ?? EqualityComparer<T>.Default;
			CreateInitialArray(InitialItemsTableSize);
		}

		// 4 - same constructor params as HashSet
		public FastHashSet(int capacity)
		{
			comparer = EqualityComparer<T>.Default;
			SetInitialCapacity(capacity);
		}

		// 5 - same constructor params as HashSet
		public FastHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
		{
			this.comparer = comparer ?? EqualityComparer<T>.Default;
			AddInitialEnumerable(collection);
		}

		// 6 - same constructor params as HashSet
		public FastHashSet(int capacity, IEqualityComparer<T> comparer)
		{
			this.comparer = comparer ?? EqualityComparer<T>.Default;
			SetInitialCapacity(capacity);
		}

		// 7th HashSet constructor has params for serialization - not sure we want to support this??? - probably should


		public FastHashSet(IEnumerable<T> collection, bool areAllCollectionItemsDefinitelyUnique = false, int initialCapacity = -1, int initialArraySize = -1, IEqualityComparer<T> comparer = null)
		{
			//??? what about an initialArraySize = 0 means go straight into hashing

			this.comparer = comparer;
			CreateInitialArray(initialArraySize);
		}

		//??? this could be public if useful somehow
		// maybe add a param to override the initial compacity
		private void AddInitialUniqueValuesEnumerable(IEnumerable<T> collection, int countOfItemsInCollection)
		{
			if (isHashing)
			{
				nextBlankIndex = 1;
				foreach (T item in collection)
				{
					int hash = item == null ? 0 : comparer.GetHashCode(item);
					int hashIndex = (hash & HighBitNotSet) % indexArray.Length;

					int index = indexArray[hashIndex];

					indexArray[hashIndex] = nextBlankIndex;
					if (index == 0) // 0 means item does not yet exist in the HashSet
					{
						//??? is it faster to create a new TNode and assign it or to set each item of the node like: could even use a ref TNode - nodeArray[nextBlankIndex].item = item;
						nodeArray[nextBlankIndex] = new TNode(item, nextBlankIndex, hash);
					}
					else
					{
						nodeArray[nextBlankIndex] = new TNode(item, index, hash);
					}
					nextBlankIndex++;
				}
			}
			else
			{
				int i = 0;
				foreach (T item in collection)
				{
					initialArray[i++] = item;
				}
			}
			usedItemsCount = countOfItemsInCollection;
			firstBlankAtEndIndex = nextBlankIndex;
		}

		private void AddInitialEnumerable(IEnumerable<T> collection)
		{
			HashSet<T> hset = collection as HashSet<T>;
			if (hset != null && Equals(hset.Comparer, Comparer))
			{
				// a set with the same item comparer must have all items unique
				// so Count will be the exact Count of the items added
				// also don't have to check for equals of items

				int count = hset.Count;
				SetInitialCapacity(count);

				AddInitialUniqueValuesEnumerable(collection, hset.Count);
			}
			else
			{
				FastHashSet<T> fhset = collection as FastHashSet<T>;
				if (fhset != null && Equals(fhset.Comparer, Comparer))
				{
					// a set with the same item comparer must have all items unique
					// so Count will be the exact Count of the items added
					// also don't have to check for equals of items
					// and a FastHashSet has the additional advantage of not having to call GetHashCode() if it is hashing
					// and it has access to the internal nodeArray so we don't have to use the foreach/enumerator

					int count = fhset.Count;
					SetInitialCapacity(count);

					if (isHashing)
					{
						if (fhset.isHashing)
						{
							nextBlankIndex = 1;
							int maxNodeIndex = fhset.nodeArray.Length - 1;
							if (fhset.firstBlankAtEndIndex <= maxNodeIndex)
							{
								maxNodeIndex = fhset.firstBlankAtEndIndex - 1;
							}

							for (int i = 1; i <= maxNodeIndex; i++)
							{
								ref TNode tnodeRef = ref fhset.nodeArray[i];
								if (tnodeRef.nextIndex != 0)
								{
									int hash = tnodeRef.hashOrNextIndexForBlanks;
									int hashIndex = (hash & HighBitNotSet) % indexArray.Length;

									int index = indexArray[hashIndex];

									indexArray[hashIndex] = nextBlankIndex;
									if (index == 0) // 0 means item does not yet exist in the HashSet
									{
										nodeArray[nextBlankIndex] = new TNode(tnodeRef.item, nextBlankIndex, hash);
									}
									else
									{
										nodeArray[nextBlankIndex] = new TNode(tnodeRef.item, index, hash);
									}
									nextBlankIndex++;
								}
							}
							usedItemsCount = count;
							firstBlankAtEndIndex = nextBlankIndex;
						}
						else
						{
							// this is an odd case, so don't try to make it really fast
							AddInitialUniqueValuesEnumerable(collection, count);
						}
					}
					else
					{
						SetInitialCapacity(count);
						int i = 0;
						foreach (T item in collection)
						{
							initialArray[i++] = item;
						}
						usedItemsCount = count;
					}
				}
				else
				{
					CreateInitialArray(InitialItemsTableSize);

					foreach (T item in collection)
					{
						Add(in item);
					}
				}
			}
		}

		private void SetInitialCapacity(int capacity)
		{
			if (capacity > InitialItemsTableSize)
			{
				// skip using the array and go right into hashing
				SwitchFromArrayToHashing(capacity);
			}
			else
			{
				CreateInitialArray(capacity);
			}
		}

		// this function can be called to switch from using initialArray and go into hasing
		// this function can also be called before initialArray is even allocated in order to skip using the array and go right into hasing
		private void SwitchFromArrayToHashing(int capacityIncrease = -1)
		{
			int newNodeArraySizeIncrease;
			int oldNodeArraySize;

			if (capacityIncrease == -1)
			{
				newNodeArraySizeIncrease = GetNewNodeArraySizeIncrease(out oldNodeArraySize, true);
			}
			else
			{
				newNodeArraySizeIncrease = capacityIncrease;

				// use 1 as the initial value because the first node (node 0) is not counted towards capacity because it is never used for items
				oldNodeArraySize = nodeArray == null ? 1 : nodeArray.Length; //??? not sure why we need the old nodeArray size, I think the nodeArray will always be null in this function?
			}

			int newNodeArraySize = oldNodeArraySize + newNodeArraySizeIncrease;

			int newIndexArraySize = 0;
			for (currentIndexIntoSizeArray = 0; currentIndexIntoSizeArray < indexArraySizeArray.Length; currentIndexIntoSizeArray++)
			{
				newIndexArraySize = indexArraySizeArray[currentIndexIntoSizeArray];
				if (newIndexArraySize >= newNodeArraySize)
				{
					break;
				}
			}
			
			if (newNodeArraySize == 0)
			{
				// this is an error, the int.MaxValue has been used for capacity and we require more - throw an Exception for this
				//??? it's not really out of memory, if running 64 bit and you have alot of virtual memory you could possibly get here and still have memory - try this with HashSet<uint> and see what it error it gives
				//throw new OutOfMemoryException();
			}

			nodeArray = new TNode[newNodeArraySize]; // the nodeArray has an extra item as it's first item (0 index) that is for available items - the memory is wasted, but it simplifies things
			indexArray = new int[newIndexArraySize]; // these will be initially set to 0, so make 0 the blank(available) value and reduce all indices by one to get to the actual index into the nodeArray
			CalcUsedItemsLoadFactorThreshold();

			// don't call AddRange here because we know for sure that these items aren't equal to each other and we can skip the equals checks
			// also we know all nodes are blank and we can avoid any logic with that as well

			int index = 1; // start at 1 because 0 is the blank item

			if (initialArray != null)
			{
				for (int i = 0; i < usedItemsCount; i++)
				{
					T item = initialArray[i];
					int hash = item == null ? 0 : comparer.GetHashCode(item);
					int hashIndex = (hash & HighBitNotSet) % newIndexArraySize;

					// idx is now from 0 to newIndexArraySize - 1

					if (indexArray[hashIndex] == 0)
					{
						nodeArray[index].nextIndex = index; // this node is the ending node, so set the nextIndex to itself
					}
					else
					{
						// item with same hash code already exists, so need to put in the bucket - put as first item in the bucket because it's easier
						nodeArray[index].nextIndex = indexArray[hashIndex];
					}
					indexArray[hashIndex] = index;
					nodeArray[index].item = initialArray[i];
					nodeArray[index].hashOrNextIndexForBlanks = hash;

					index++;
				}
				initialArray = null; // this array can now be garbage collected because it is no longer referenced
			}

			nextBlankIndex = index; // any blank item with a 0 nextIndex pointer means it really points to the next one after it
			firstBlankAtEndIndex = index;

			isHashing = true;
		}

		private void CreateInitialArray(int initialArraySize)
		{
			if (initialArraySize < 0)
			{
				initialArraySize = InitialItemsTableSize;
			}

			initialArray = new T[initialArraySize];
			//isHashing = false;
		}

		private void CalcUsedItemsLoadFactorThreshold()
		{
			if (indexArray != null)
			{
				usedItemsLoadFactorThreshold = (int)(LoadFactor * (double)indexArray.Length);
			}
		}

		// this is only present to implement ICollection<T> - it has no real value otherwise
		bool ICollection<T>.IsReadOnly
		{
			get { return false; }
		}

		// this implements ICollection<T>.CopyTo(T[], Int32)
		public void CopyTo(T[] array, int arrayIndex)
		{
			CopyTo(array, arrayIndex, usedItemsCount);
		}

		public void CopyTo(T[] array)
		{
			CopyTo(array, 0, usedItemsCount);
		}

		// not really sure how this can be useful because you never know exactly what elements you will get copied (unless you copy them all)
		// it could easily vary for different implementations or if items were added in different order or if items were added removed and then added, instead of just added 
		public void CopyTo(T[] array, int arrayIndex, int count)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array), "Value cannot be null.");
			}

			if (arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Non negative number is required.");
			}

			if (count < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(count), "Non negative number is required.");
			}

			if (arrayIndex + count > array.Length)
			{
				throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");
			}

			if (count == 0)
			{
				return;
			}

			//??? maybe need to throw other exceptions, like if array is null or arrayIndex outside the bounds of the array or arrayIndex + count is outside the bounds of the array
			// also add exceptions to other functions

			if (isHashing)
			{
				int pastNodeIndex = nodeArray.Length;
				if (firstBlankAtEndIndex < pastNodeIndex)
				{
					pastNodeIndex = firstBlankAtEndIndex;
				}

				int cnt = 0;
				for (int i = 1; i < pastNodeIndex; i++)
				{
					if (nodeArray[i].nextIndex != 0) // nextIndex == 0 indicates a blank/available node
					{
						array[arrayIndex++] = nodeArray[i].item;
						if (++cnt == count)
						{
							break;
						}
					}
				}
			}
			else
			{
				int cnt = usedItemsCount;
				if (cnt > count)
				{
					cnt = count;
				}

				// for small arrays, I think the for loop below will actually be faster than Array.Copy - could test this???
				//Array.Copy(initialArray, 0, array, arrayIndex, cnt);

				for (int i = 0; i < cnt; i++)
				{
					array[arrayIndex++] = initialArray[i];
				}
			}
		}

		public IEqualityComparer<T> Comparer
		{
			get
			{
				// if not set, return the default - this is what HashSet does
				// even if it is set to null explicitly, it will still return the default
				//return (comparer == null) ? EqualityComparer<T>.Default : comparer;
				return comparer;
			}
		}

		public int Count
		{
			get
			{
				return usedItemsCount;
			}
		}

		public bool IsHashing
		{
			get
			{
				return isHashing;
			}
		}

		// this is the percent of used items to all items (used + blank/available)
		// at which point (calculated ratio is >= property value) any additional added items will
		// first resize the indexArray to the next prime to avoid too many collisions and buckets becoming too large
		public double LoadFactor
		{
			get
			{
				return loadFactor;
			}

			set
			{
				loadFactor = value;
				CalcUsedItemsLoadFactorThreshold();
			}

		}

		// this is the capacity that can be trimmed with TrimExcessCapacity
		// items that were removed from the hash arrays can't be trimmed by calling TrimExcessCapacity, only the blank items at the end
		// items that were removed from the initialArray can be trimmed by calling TrimExcessCapacity because the items after are moved to fill the blank space
		public int ExcessCapacity
		{
			get
			{
				int excessCapacity;
				if (isHashing)
				{
					excessCapacity = nodeArray.Length - firstBlankAtEndIndex;
				}
				else
				{
					excessCapacity = initialArray.Length - usedItemsCount;
				}
				return excessCapacity;
			}
		}

		public int Capacity
		{
			get
			{
				if (isHashing)
				{
					return nodeArray.Length - 1; // subtract 1 for blank node at 0 index
				}
				else
				{
					return initialArray.Length;
				}
			}
		}

		// -1 means there is no set MaxCapacity, it is only limited by memory and int.MaxValue
		public int MaxCapacity { get; set; } = -1;

		// when ExcessCapacity becomes 0 and we need to allocate for more items, this overrides the next default increase, which is usually double
		// -1 indicates to use the default increase
		public int NextCapacityIncreaseOverride { get; set; } = -1;

		public int NextCapacityIncreaseDefault
		{
			get
			{
				return GetNewNodeArraySizeIncrease(out int oldNodeArraySize, true);
			}
		}

		public int NextCapacityIncrease
		{
			get
			{
				return GetNewNodeArraySizeIncrease(out int oldNodeArraySize);
			}
		}

		// allocate enough space (or make sure existing space is enough) for capacity # of items to be stored in the hashset without any further allocations
		// the actual capacity at the end of this function may be more than specified
		// (in the case when it was more before this function was called - nothing is trimmed by this function, or in the case that slighly more capacity was allocated by this function)
		// return the actual capacity at the end of this function
		public int EnsureCapacity(int capacity)
		{
			//??? this function is only in .net core, so to test if this always sets modified or only sometimes test in .net core
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			int currentCapacity;

			if (isHashing)
			{
				currentCapacity = nodeArray.Length - usedItemsCount;
			}
			else
			{
				currentCapacity = initialArray.Length - usedItemsCount;
			}

			if (currentCapacity < capacity)
			{
				IncreaseCapacity(capacity - currentCapacity);
			}

			//??? is this correct - this should be the number where the next lowest number would force a resize of indexArray with the current loadfactor and the entire nodeArray is full
			int calcedNewIndexArraySize = (int)(nodeArray.Length / loadFactor) + 1;

			if (calcedNewIndexArraySize < 0 && calcedNewIndexArraySize > LargestPrimeLessThanMaxInt)
			{
				calcedNewIndexArraySize = LargestPrimeLessThanMaxInt;
			}
			else
			{
				calcedNewIndexArraySize = GetEqualOrClosestHigherPrime(calcedNewIndexArraySize);
			}

			if (indexArray.Length < calcedNewIndexArraySize)
			{
				// -1 means stop trying to increase the size based on the array of primes
				// instead calc based on 2 * existing length and then get the next higher prime
				currentIndexIntoSizeArray = -1;

				ResizeIndexArrayReverse(calcedNewIndexArraySize);
			}

			return nodeArray.Length - usedItemsCount;
		}

		// return the prime number that is equal to n (if n is a prime number) or the closest prime number greather than n
		public static int GetEqualOrClosestHigherPrime(int n)
		{
			if (n >= LargestPrimeLessThanMaxInt)
			{
				// the next prime above this number is int.MaxValue, which we don't want to return that value because some indices increment one or two ints past this number and we don't want them to overflow
				return LargestPrimeLessThanMaxInt;
			}

			if ((n & 1) == 0)
			{
				n++; // make n odd
			}

			bool found;

			do
			{
				found = true;

				int sqrt = (int)Math.Sqrt(n);
				for (int i = 3; i <= sqrt; i += 2)
				{
					if (n % i == 0)
					{
						found = false;
						n += 2;
						break;
					}
				}
			} while (!found);
 
			return n;
		}

		private int GetNewNodeArraySizeIncrease(out int oldArraySize, bool getOnlyDefaultSize = false)
		{
			if (nodeArray != null)
			{
				oldArraySize = nodeArray.Length;
			}
			else if (initialArray != null)
			{
				oldArraySize = initialArray.Length; // this isn't the old node array, but it is the old # of items that could be stored without resizing
			}
			else
			{
				oldArraySize = InitialItemsTableSize; // this isn't the old node array or the initialArray, but it is the initial size we should start at
			}
				
			int increaseInSize;
			if (getOnlyDefaultSize || NextCapacityIncreaseOverride < 0)
			{
				increaseInSize = oldArraySize;
			}
			else
			{
				increaseInSize = NextCapacityIncreaseOverride;
			}

			int maxIncreaseInSize;
			if (getOnlyDefaultSize || MaxCapacity < 0)
			{
				maxIncreaseInSize = (int.MaxValue - 2) - oldArraySize;
			}
			else
			{
				maxIncreaseInSize = MaxCapacity - oldArraySize;
				if (maxIncreaseInSize < 0)
				{
					maxIncreaseInSize = 0;
				}
			}

			if (increaseInSize > maxIncreaseInSize)
			{
				increaseInSize = maxIncreaseInSize;
			}
			return increaseInSize;
		}

		// if the value returned gets used and that value is different than the current indexArray.Length, then the calling code should increment currentIndexIntoSizeArray because this would now be the current
		private int GetNewIndexArraySize()
		{
			//??? to avoid to many allocations of this array, setting the initialCapacity in the constructor or MaxCapacity or NextCapacityIncreaseOverride should determine
			// where the currentIndexIntoSizeArray is pointing to and also the capacity of this indexArray (which should be a prime)		public int MaxCapacity { get; set; } = -1;

			int newArraySize;
			if (currentIndexIntoSizeArray == -1)
			{
				// -1 means stop trying to increase the size based on the array of primes
				// instead calc based on 2 * existing length and then get the next higher prime
				newArraySize = indexArray.Length;
				if (newArraySize < int.MaxValue / 2)
				{
					newArraySize = GetEqualOrClosestHigherPrime(newArraySize + newArraySize);
				}
				else
				{
					newArraySize = LargestPrimeLessThanMaxInt;
				}
			}
			else
			{
				if (currentIndexIntoSizeArray + 1 < indexArraySizeArray.Length)
				{
					newArraySize = indexArraySizeArray[currentIndexIntoSizeArray + 1];
				}
				else
				{
					newArraySize = indexArray.Length;
				}
			}

			return newArraySize;
		}

		// if hashing, increase the size of the nodeArray
		// if not yet hashing, switch to hashing
		private void IncreaseCapacity(int capacityIncrease = -1)
		{
			if (isHashing)
			{
				int newNodeArraySizeIncrease;
				int oldNodeArraySize;

				if (capacityIncrease == -1)
				{
					newNodeArraySizeIncrease = GetNewNodeArraySizeIncrease(out oldNodeArraySize);
				}
				else
				{
					newNodeArraySizeIncrease = capacityIncrease;
					oldNodeArraySize = nodeArray.Length;
				}

				if (newNodeArraySizeIncrease <= 0)
				{
					//??? throw an error
				}

				int newNodeArraySize = oldNodeArraySize + newNodeArraySizeIncrease;

				#if false
				TNode[] newNodeArray = new TNode[newNodeArraySize];
				Array.Copy(nodeArray, 0, newNodeArray, 0, nodeArray.Length);
				nodeArray = newNodeArray;
				#endif
				Array.Resize(ref nodeArray, newNodeArraySize);

				// the below should be the same after a nodeArray size increase
				//firstBlankAtEndIndex = oldNodeArraySize;
				//nextBlankIndex = firstBlankAtEndIndex;
			}
			else
			{
				SwitchFromArrayToHashing(capacityIncrease);
			}
		}

		private TNode[] IncreaseCapacityNoCopy(int capacityIncrease = -1)
		{
			if (isHashing)
			{
				int newNodeArraySizeIncrease;
				int oldNodeArraySize;

				if (capacityIncrease == -1)
				{
					newNodeArraySizeIncrease = GetNewNodeArraySizeIncrease(out oldNodeArraySize);
				}
				else
				{
					newNodeArraySizeIncrease = capacityIncrease;
					oldNodeArraySize = nodeArray.Length;
				}

				if (newNodeArraySizeIncrease <= 0)
				{
					//??? throw an error
				}

				int newNodeArraySize = oldNodeArraySize + newNodeArraySizeIncrease;

				TNode[] newNodeArray = new TNode[newNodeArraySize];
				return newNodeArray;
				//Array.Resize(ref nodeArray, newNodeArraySize);

				// the below should be the same after a nodeArray size increase
				//firstBlankAtEndIndex = oldNodeArraySize;
				//nextBlankIndex = firstBlankAtEndIndex;
			}
			else
			{
				SwitchFromArrayToHashing(capacityIncrease);
				return null;
			}
		}

		private void ResizeIndexArrayForward(int newIndexArraySize)
		{
			if (newIndexArraySize == indexArray.Length)
			{
				// this will still work if no increase in size - it just might be slower than if you could increase the indexArray size
			}
			else
			{
				//??? what if there is a high percent of blank/unused items in the nodeArray before the firstBlankAtEndIndex (mabye because of lots of removes)?
				// It would probably be faster to loop through the indexArray and then do chaining to find the used nodes - one problem with this is that you would have to find blank nodes - but they would be chained
				// this probably isn't a very likely scenario
				indexArray = new int[newIndexArraySize];

				if (currentIndexIntoSizeArray >= 0)
				{
					currentIndexIntoSizeArray++; // when the newIndexArraySize gets used in the above code, point to the next avaialble size - ??? not sure this is the best place to increment this
				}

				CalcUsedItemsLoadFactorThreshold();

				int indexArrayLength = indexArray.Length;

				int pastNodeIndex = nodeArray.Length;
				if (firstBlankAtEndIndex < pastNodeIndex)
				{
					pastNodeIndex = firstBlankAtEndIndex;
				}

				//??? for a loop where the end is array.Length, the compiler can skip any array bounds checking - can it do it for this code - it should be able to because pastIndex is no more than indexArray.Length
				for (int i = 1; i < pastNodeIndex; i++)
				{
					ref TNode tNodeRef = ref nodeArray[i];
					if (tNodeRef.nextIndex != 0) // nextIndex == 0 indicates a blank/available node
					{
						int hashIndex = (tNodeRef.hashOrNextIndexForBlanks & HighBitNotSet) % indexArrayLength;

						int index = indexArray[hashIndex];
						if (index == 0)
						{
							indexArray[hashIndex] = i;
							tNodeRef.nextIndex = i;
						}
						else
						{
							indexArray[hashIndex] = i;
							tNodeRef.nextIndex = index;
						}
					}
				}
			}
		}

		private void ResizeIndexArrayForwardZeroEndsChain(int newIndexArraySize)
		{
			if (newIndexArraySize == indexArray.Length)
			{
				// this will still work if no increase in size - it just might be slower than if you could increase the indexArray size
			}
			else
			{
				//??? what if there is a high percent of blank/unused items in the nodeArray before the firstBlankAtEndIndex (mabye because of lots of removes)?
				// It would probably be faster to loop through the indexArray and then do chaining to find the used nodes - one problem with this is that you would have to find blank nodes - but they would be chained
				// this probably isn't a very likely scenario
				indexArray = new int[newIndexArraySize];

				if (currentIndexIntoSizeArray >= 0)
				{
					currentIndexIntoSizeArray++; // when the newIndexArraySize gets used in the above code, point to the next avaialble size - ??? not sure this is the best place to increment this
				}

				CalcUsedItemsLoadFactorThreshold();

				int indexArrayLength = indexArray.Length;

				int pastNodeIndex = nodeArray.Length;
				if (firstBlankAtEndIndex < pastNodeIndex)
				{
					pastNodeIndex = firstBlankAtEndIndex;
				}

				//??? for a loop where the end is array.Length, the compiler can skip any array bounds checking - can it do it for this code - it should be able to because pastIndex is no more than indexArray.Length
				if (firstBlankAtEndIndex == usedItemsCount + 1)
				{
					// this means there aren't any blank nodes
					for (int i = 1; i < pastNodeIndex; i++)
					{
						ref TNode tNodeRef = ref nodeArray[i];

						int hashIndex = tNodeRef.hashOrNextIndexForBlanks % indexArrayLength;

						int index = indexArray[hashIndex];
						indexArray[hashIndex] = i;

						tNodeRef.nextIndex = index;
					}
				}
				else
				{
					// this means there are some blank nodes
					for (int i = 1; i < pastNodeIndex; i++)
					{
						ref TNode tNodeRef = ref nodeArray[i];
						if (tNodeRef.nextIndex != BlankNextIndexIndicator) // skip blank nodes
						{
							int hashIndex = tNodeRef.hashOrNextIndexForBlanks % indexArrayLength;

							int index = indexArray[hashIndex];
							indexArray[hashIndex] = i;

							tNodeRef.nextIndex = index;
						}
					}
				}
			}
		}

		private void ResizeIndexArrayReverse(int newIndexArraySize)
		{
			if (newIndexArraySize == indexArray.Length)
			{
				// this will still work if no increase in size - it just might be slower than if you could increase the indexArray size
			}
			else
			{
				//??? what if there is a high percent of blank/unused items in the nodeArray before the firstBlankAtEndIndex (mabye because of lots of removes)?
				// It would probably be faster to loop through the indexArray and then do chaining to find the used nodes - one problem with this is that you would have to find blank nodes - but they would be chained
				// this probably isn't a very likely scenario
				indexArray = new int[newIndexArraySize];

				if (currentIndexIntoSizeArray >= 0)
				{
					currentIndexIntoSizeArray++; // when the newIndexArraySize gets used in the above code, point to the next avaialble size - ??? not sure this is the best place to increment this
				}

				CalcUsedItemsLoadFactorThreshold();

				int indexArrayLength = indexArray.Length;

				int lastNodeIndex = nodeArray.Length - 1;
				if (firstBlankAtEndIndex < lastNodeIndex)
				{
					lastNodeIndex = firstBlankAtEndIndex - 1;
				}

				if (nextBlankIndex >= firstBlankAtEndIndex) // we know there aren't any blanks within the nodes, so skip the if (tNodeRef.nextIndex != 0) check
				{
					for (int i = lastNodeIndex; i >= 1 ; i--)
					{
						ref TNode tNodeRef = ref nodeArray[i];
						int hashIndex = (tNodeRef.hashOrNextIndexForBlanks & HighBitNotSet) % indexArray.Length;

						int index = indexArray[hashIndex];
						if (index == 0)
						{
							indexArray[hashIndex] = i;
							tNodeRef.nextIndex = i;
						}
						else
						{
							indexArray[hashIndex] = i;
							tNodeRef.nextIndex = index;
						}
					}				
				}
				else
				{
					for (int i = lastNodeIndex; i >= 1 ; i--)
					{
						ref TNode tNodeRef = ref nodeArray[i];
						if (tNodeRef.nextIndex != 0) // nextIndex == 0 indicates a blank/available node
						{
							int hashIndex = (tNodeRef.hashOrNextIndexForBlanks & HighBitNotSet) % indexArray.Length;

							int index = indexArray[hashIndex];
							if (index == 0)
							{
								indexArray[hashIndex] = i;
								tNodeRef.nextIndex = i;
							}
							else
							{
								indexArray[hashIndex] = i;
								tNodeRef.nextIndex = index;
							}
						}
					}
				}
			}
		}

		private void ResizeIndexArrayForwardAndCopy(int newIndexArraySize, TNode[] oldNodeArray)
		{
			if (newIndexArraySize == indexArray.Length)
			{
				// this will still work if no increase in size - it just might be slower than if you could increase the indexArray size
			}
			else
			{
				//??? what if there is a high percent of blank/unused items in the nodeArray before the firstBlankAtEndIndex (mabye because of lots of removes)?
				// It would probably be faster to loop through the indexArray and then do chaining to find the used nodes - one problem with this is that you would have to find blank nodes - but they would be chained
				// this probably isn't a very likely scenario
				indexArray = new int[newIndexArraySize];

				if (currentIndexIntoSizeArray >= 0)
				{
					currentIndexIntoSizeArray++; // when the newIndexArraySize gets used in the above code, point to the next avaialble size - ??? not sure this is the best place to increment this
				}

				CalcUsedItemsLoadFactorThreshold();

				int indexArrayLength = indexArray.Length;

				int lastNodeIndex = nodeArray.Length - 1;
				if (firstBlankAtEndIndex < lastNodeIndex)
				{
					lastNodeIndex = firstBlankAtEndIndex - 1;
				}

				if (nextBlankIndex >= firstBlankAtEndIndex) // we know there aren't any blanks within the nodes, so skip the if (tNodeRef.nextIndex != 0) check
				{
					for (int i = 1; i <= lastNodeIndex; i++)
					{
						ref TNode tNodeRef = ref nodeArray[i];
						tNodeRef = oldNodeArray[i];

						int hashIndex = (tNodeRef.hashOrNextIndexForBlanks & HighBitNotSet) % indexArray.Length;

						int index = indexArray[hashIndex];
						if (index == 0)
						{
							indexArray[hashIndex] = i;
							tNodeRef.nextIndex = i;
						}
						else
						{
							indexArray[hashIndex] = i;
							tNodeRef.nextIndex = index;
						}
					}				
				}
				else
				{
					for (int i = 1; i <= lastNodeIndex; i++)
					{
						ref TNode tNodeRef = ref nodeArray[i];
						tNodeRef = oldNodeArray[i];
						if (tNodeRef.nextIndex != 0) // nextIndex == 0 indicates a blank/available node
						{
							int hashIndex = (tNodeRef.hashOrNextIndexForBlanks & HighBitNotSet) % indexArray.Length;

							int index = indexArray[hashIndex];
							if (index == 0)
							{
								indexArray[hashIndex] = i;
								tNodeRef.nextIndex = i;
							}
							else
							{
								indexArray[hashIndex] = i;
								tNodeRef.nextIndex = index;
							}
						}
					}
				}
			}
		}

		private void ResizeIndexArrayWithMarks(int newIndexArraySize)
		{
			if (newIndexArraySize == indexArray.Length)
			{
				// this will still work if no increase in size - it just might be slower than if you could increase the indexArray size
			}
			else
			{
				//??? what if there is a high percent of blank/unused items in the nodeArray before the firstBlankAtEndIndex (mabye because of lots of removes)?
				// It would probably be faster to loop through the indexArray and then do chaining to find the used nodes - one problem with this is that you would have to find blank nodes - but they would be chained
				// this probably isn't a very likely scenario
				indexArray = new int[newIndexArraySize];

				if (currentIndexIntoSizeArray >= 0)
				{
					currentIndexIntoSizeArray++; // when the newIndexArraySize gets used in the above code, point to the next avaialble size - ??? not sure this is the best place to increment this
				}

				CalcUsedItemsLoadFactorThreshold();

				int indexArrayLength = indexArray.Length;

				int pastNodeIndex = nodeArray.Length;
				if (firstBlankAtEndIndex < pastNodeIndex)
				{
					pastNodeIndex = firstBlankAtEndIndex;
				}

				//??? for a loop where the end is array.Length, the compiler can skip any array bounds checking - can it do it for this code - it should be able to because pastIndex is no more than indexArray.Length
				int nextIndex;
				for (int i = 1; i < pastNodeIndex; i++)
				{
					ref TNode tNodeRef = ref nodeArray[i];
					nextIndex = tNodeRef.nextIndex;
					if (nextIndex != 0) // nextIndex == 0 indicates a blank/available node
					{
						bool isMarked = (nextIndex & MarkNextIndexBitMask) != 0;

						int hashIndex = (tNodeRef.hashOrNextIndexForBlanks & HighBitNotSet) % indexArray.Length;

						int index = indexArray[hashIndex];
						if (index == 0)
						{
							indexArray[hashIndex] = i;
							if (isMarked)
							{
								tNodeRef.nextIndex = i | MarkNextIndexBitMask;
							}
							else
							{
								tNodeRef.nextIndex = i;
							}
						}
						else
						{
							indexArray[hashIndex] = i;
							if (isMarked)
							{
								tNodeRef.nextIndex = index | MarkNextIndexBitMask;
							}
							else
							{
								tNodeRef.nextIndex = index;
							}
						}
					}
				}
			}
		}

		// this removes all items, but does not do any trimming of the resulting unused memory
		// to trim the unused memory, call TrimExcess
		public void Clear()
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			if (isHashing)
			{
				firstBlankAtEndIndex = 1;
				nextBlankIndex = 1;
				Array.Clear(indexArray, 0, indexArray.Length);
			}

			usedItemsCount = 0;
		}

		//
		public void ClearAndTrimAll()
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			// this would deallocate the arrays - would need to lazy allocate the arrays if allowing this (if (nodeArray == null) .. InitForHashing() if (initialArray == null) InitForInitialArray()
			//??? I don't think the time to always check for the arrays would make this worth it - could just set the BasicHashSet variable to null in this case and reset it with constructor when needed?
			// maybe could just check for the initialArray this way, because it would set isHashing to false?
		}

		//??? what about a function the cleans up blank internal nodes by rechaining used nodes to fill up the blank nodes
		// and then the TrimeExcess can do a better job of trimming excess - add a function to do that?  call it CompactNodeArray
		// there is a perfect structure where the first non-blank indexArray points to index 1 in the nodeArray and anything that follows does so in nodeArray index 2, 3, etc.
		// this way you have locality of reference when doing lookups and you also remove all internal blank nodes
		// it would be easy to create this structure when doing a resize of the nodeArray, so maybe doing an Array.Resize isn't the best for the nodeArray, although you are usually only doing this when you have no more blank nodes, so that part of the advantage is not valid for this scenario

		//??? I wonder how TrimExcess works for HashSet?

		// documentation states:
		// You can use the TrimExcess method to minimize a HashSet<T> object's memory overhead once it is known that no new elements will be added
		// To completely clear a HashSet<T> object and release all memory referenced by it, call this method after calling the Clear method.
		public void TrimExcess()
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			if (isHashing)
			{
				// do we have to check for nodeArray != null??? - is this possible?
				if (nodeArray != null && nodeArray.Length > firstBlankAtEndIndex && firstBlankAtEndIndex > 0)
				{
					Array.Resize(ref nodeArray, firstBlankAtEndIndex);
					// when firstBlankAtEndIndex == nodeArray.Length, that means there are no blank at end items
				}
			}
			else
			{
				if (initialArray != null && initialArray.Length > usedItemsCount && usedItemsCount > 0)
				{
					Array.Resize(ref initialArray, usedItemsCount);
				}
			}
		}

		// this is only present to implement ICollection<T> - it has no real value otherwise because the Add method with bool return value already does this
		void ICollection<T>.Add(T item)
		{
			Add(in item);
		}

		public bool Add(in T item)//??? do we need 2 versions, one with in and one without
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			if (isHashing)
			{
				return AddToHashSetIfNotFound(in item, item == null ? 0 : comparer.GetHashCode(item), out int addedNodeIndex);
			}
			else
			{
				int i;
				for (i = 0; i < usedItemsCount; i++)
				{
					//if (comparer == null ? EqualityComparer<T>.Default.Equals(item, initialArray[i]) : comparer.Equals(item, initialArray[i]))
					if (comparer.Equals(item, initialArray[i]))
					{
						return false;
					}
				}

				if (i == initialArray.Length)
				{
					SwitchFromArrayToHashing();
					return AddToHashSetIfNotFound(in item, item == null ? 0 : comparer.GetHashCode(item), out int addedNodeIndex);
				}
				else
				{
					// add to initialArray
					initialArray[i] = item;
					usedItemsCount++;
					return true;
				}
			}
		}

		public bool Add2(T item)
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			if (isHashing)
			{
				int hash = item == null ? 0 : comparer.GetHashCode(item);
				int hashIndex = (hash & HighBitNotSet) % indexArray.Length;

				int index = indexArray[hashIndex];

				bool increasedCapacity = false;
				if (index == 0) // 0 means item does not yet exist in the HashSet, so add it
				{
					if (nextBlankIndex >= nodeArray.Length)
					{
						// there aren't any more blank nodes to add items, so we need to increase capacity
						IncreaseCapacity();
						increasedCapacity = true;
						//ResizeIndexArrayForward(GetNewIndexArraySize());
						//TNode[] newNodeArray = IncreaseCapacityNoCopy();
						//if (newNodeArray == null)
						//{
						//	ResizeIndexArrayForward(GetNewIndexArraySize());
						//}
						//else
						//{
						//	TNode[] oldNodeArray = nodeArray;
						//	nodeArray = newNodeArray;
						//	ResizeIndexArrayForwardAndCopy(GetNewIndexArraySize(), oldNodeArray);
						//}
						//increasedCap = true;
					}
					indexArray[hashIndex] = nextBlankIndex;

					ref TNode tNodeRefBlank = ref nodeArray[nextBlankIndex];
					if (nextBlankIndex >= firstBlankAtEndIndex)
					{
						// the blank nodes starting at firstBlankAtEndIndex aren't chained

						tNodeRefBlank.hashOrNextIndexForBlanks = hash;
						tNodeRefBlank.nextIndex = nextBlankIndex;
						tNodeRefBlank.item = item;

						nextBlankIndex = ++firstBlankAtEndIndex;
					}
					else
					{
						int nextForBlanks = tNodeRefBlank.hashOrNextIndexForBlanks;

						tNodeRefBlank.hashOrNextIndexForBlanks = hash;
						tNodeRefBlank.nextIndex = nextBlankIndex;
						tNodeRefBlank.item = item;

						// the blank nodes before firstBlankAtEndIndex are chained (the hashOrNextIndexForBlanks points to the next blank node)
						nextBlankIndex = nextForBlanks;
					}
				}
				else
				{
					// item with same hashIndex already exists, so need to put it in the bucket if it doesn't already exist there (using Equals)

					#if Add_Item_To_Front_Of_Chain
					int firstIndex = index;
					#endif

					ref TNode tNodeRef = ref nodeArray[index];
					while (true)
					{

						// check if hash codes are equal before calling Equals (this may be faster to compare items that aren't equal, but slower for items that are equal)
						// items that are Equal must have the same hash code
						#if Exclude_Comparing_Cached_Hash
						if (comparer.Equals(tNodeRef.item, item))
						#else
						if (tNodeRef.hashOrNextIndexForBlanks == hash && comparer.Equals(tNodeRef.item, item))
						#endif
						{
							return false; // item was found, so return false to indicate it was not added
						}

						int nextIndex = tNodeRef.nextIndex;
						if (nextIndex == index)
						{
							// a nextIndex that equals this index indicates the end of the items with the same hashIndex, this way all blanks can be determined by a nextIndex = 0
							break; // item was not found, so add it
						}
						else
						{
							index = nextIndex;
							tNodeRef = ref nodeArray[index];
						}
					}

					if (nextBlankIndex >= nodeArray.Length)
					{
						IncreaseCapacity();
						increasedCapacity = true;

						//ResizeIndexArrayForward(GetNewIndexArraySize());
						//TNode[] newNodeArray = IncreaseCapacityNoCopy();
						//if (newNodeArray == null)
						//{
						//	ResizeIndexArrayForward(GetNewIndexArraySize());
						//}
						//else
						//{
						//	TNode[] oldNodeArray = nodeArray;
						//	nodeArray = newNodeArray;
						//	ResizeIndexArrayForwardAndCopy(GetNewIndexArraySize(), oldNodeArray);
						//}
						//increasedCap = true;
						#if Add_Item_To_Front_Of_Chain
						#else
						// it's likely that the nodeArray reference changed with the increase in capacity, so set tNodeRef within the new nodeArray
						//??? do we want to keep a reference to the old nodeArray until this point - this may prevent the old nodeArray from being garbage collected until the following code executes
						tNodeRef = ref nodeArray[index];
						#endif
					}

					#if Add_Item_To_Front_Of_Chain
					indexArray[hashIndex] = nextBlankIndex;
					#else
					tNodeRef.nextIndex = nextBlankIndex;
					#endif
	
					ref TNode tNodeRefBlank = ref nodeArray[nextBlankIndex];
					if (nextBlankIndex >= firstBlankAtEndIndex)
					{
						// the blank nodes starting at firstBlankAtEndIndex aren't chained
						nextBlankIndex = ++firstBlankAtEndIndex;

						tNodeRefBlank.hashOrNextIndexForBlanks = hash;

						#if Add_Item_To_Front_Of_Chain
						tNodeRefBlank.nextIndex = firstIndex;
						#else
						tNodeRefBlank.nextIndex = nextBlankIndex; // end of chain points to itself
						#endif

						tNodeRefBlank.item = item;
					}
					else
					{
						// the blank nodes before firstBlankAtEndIndex are chained (the hashOrNextIndexForBlanks points to the next blank node)
						nextBlankIndex = tNodeRefBlank.hashOrNextIndexForBlanks;

						tNodeRefBlank.hashOrNextIndexForBlanks = hash;

						#if Add_Item_To_Front_Of_Chain
						tNodeRefBlank.nextIndex = firstIndex;
						#else
						tNodeRefBlank.nextIndex = nextBlankIndex; // end of chain points to itself
						#endif

						tNodeRefBlank.item = item;
					}

					//??? don't add the node at the end of the list, always add it to the front, this way we are always consistent since adding to the front is used when adding items that we know are unique so we don't have to traverse the chain
					//nodeArray[firstNodeIndex].nextIndex = nextBlankIndex;
				}

				usedItemsCount++;

				//if (usedItemsCount >= usedItemsLoadFactorThreshold)
				if (increasedCapacity)
				{
					//System.Diagnostics.Debug.Assert(increasedCap, "increasedCap = false before calling ResizeIndexArray");
					ResizeIndexArrayForward(GetNewIndexArraySize());
				}

				return true;
			}
			else
			{
				int i;
				for (i = 0; i < usedItemsCount; i++)
				{
					if (comparer.Equals(item, initialArray[i]))
					{
						return false;
					}
				}

				if (i == initialArray.Length)
				{
					SwitchFromArrayToHashing();
					return AddToHashSetIfNotFound(in item, item == null ? 0 : comparer.GetHashCode(item), out int addedNodeIndex);
				}
				else
				{
					// add to initialArray
					initialArray[i] = item;
					usedItemsCount++;
					return true;
				}
			}
		}

		public bool Add(T item)
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			if (isHashing)
			{
				//??? consider doing the  &  & HighBitNotSet on the comparer.GetHashCode(item); - this could mess with comparing equals (maybe -2,000,000,000 would compare equals to 1...) by just their hash codes, but this should be rare
				//int hash = item == null ? 0 : comparer.GetHashCode(item);
				int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
				int hashIndex = hash % indexArray.Length;

				for (int index = indexArray[hashIndex]; index != 0; )
				{
					ref TNode tNodeRef = ref nodeArray[index];

					if (tNodeRef.hashOrNextIndexForBlanks == hash && comparer.Equals(tNodeRef.item, item))
					{
						return false; // item was found, so return false to indicate it was not added
					}

					index = tNodeRef.nextIndex;
				}

				if (nextBlankIndex >= nodeArray.Length)
				{
					// there aren't any more blank nodes to add items, so we need to increase capacity
					IncreaseCapacity();
					ResizeIndexArrayForwardZeroEndsChain(GetNewIndexArraySize());

					// fix things messed up by indexArray resize
					hashIndex = hash % indexArray.Length;
				}

				int firstIndex = indexArray[hashIndex];
				indexArray[hashIndex] = nextBlankIndex;

				ref TNode tNodeRefBlank = ref nodeArray[nextBlankIndex];
				if (nextBlankIndex >= firstBlankAtEndIndex)
				{
					// the blank nodes starting at firstBlankAtEndIndex aren't chained
					nextBlankIndex = ++firstBlankAtEndIndex;
				}
				else
				{
					// the blank nodes before firstBlankAtEndIndex are chained (the hashOrNextIndexForBlanks points to the next blank node)
					nextBlankIndex = tNodeRefBlank.hashOrNextIndexForBlanks;
				}

				tNodeRefBlank.hashOrNextIndexForBlanks = hash;
				tNodeRefBlank.nextIndex = firstIndex;
				tNodeRefBlank.item = item;

				usedItemsCount++;

				return true;
			}
			else
			{
				int i;
				for (i = 0; i < usedItemsCount; i++)
				{
					if (comparer.Equals(item, initialArray[i]))
					{
						return false;
					}
				}

				if (i == initialArray.Length)
				{
					SwitchFromArrayToHashingZeroEndsChain();
					return AddToHashSetIfNotFoundZeroEndsChain(in item, item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet)) != 0;
				}
				else
				{
					// add to initialArray
					initialArray[i] = item;
					usedItemsCount++;
					return true;
				}
			}
		}

		// this function can be called to switch from using initialArray and go into hashing
		// this function can also be called before initialArray is even allocated in order to skip using the array and go right into hashing
		private void SwitchFromArrayToHashingZeroEndsChain(int capacityIncrease = -1)
		{
			int newNodeArraySizeIncrease;
			int oldNodeArraySize;

			if (capacityIncrease == -1)
			{
				newNodeArraySizeIncrease = GetNewNodeArraySizeIncrease(out oldNodeArraySize, true);
			}
			else
			{
				newNodeArraySizeIncrease = capacityIncrease;

				// use 1 as the initial value because the first node (node 0) is not counted towards capacity because it is never used for items
				oldNodeArraySize = nodeArray == null ? 1 : nodeArray.Length; //??? not sure why we need the old nodeArray size, I think the nodeArray will always be null in this function?
			}

			int newNodeArraySize = oldNodeArraySize + newNodeArraySizeIncrease;

			int newIndexArraySize = 0;
			for (currentIndexIntoSizeArray = 0; currentIndexIntoSizeArray < indexArraySizeArray.Length; currentIndexIntoSizeArray++)
			{
				newIndexArraySize = indexArraySizeArray[currentIndexIntoSizeArray];
				if (newIndexArraySize >= newNodeArraySize)
				{
					break;
				}
			}
			
			if (newNodeArraySize == 0)
			{
				// this is an error, the int.MaxValue has been used for capacity and we require more - throw an Exception for this
				//??? it's not really out of memory, if running 64 bit and you have alot of virtual memory you could possibly get here and still have memory - try this with HashSet<uint> and see what it error it gives
				//throw new OutOfMemoryException();
			}

			nodeArray = new TNode[newNodeArraySize]; // the nodeArray has an extra item as it's first item (0 index) that is for available items - the memory is wasted, but it simplifies things
			indexArray = new int[newIndexArraySize]; // these will be initially set to 0, so make 0 the blank(available) value and reduce all indices by one to get to the actual index into the nodeArray
			CalcUsedItemsLoadFactorThreshold();

			// don't call AddRange here because we know for sure that these items aren't equal to each other and we can skip the equals checks
			// also we know all nodes are blank and we can avoid any logic with that as well

			int index = 1; // start at 1 because 0 is the blank item

			if (initialArray != null)
			{
				// i is the index into initialArray, index is the index into nodeArray
				for (int i = 0; i < usedItemsCount; i++, index++)
				{
					ref T itemRef = ref initialArray[i];
					ref TNode tNodeRef = ref nodeArray[index];

					//??? if item is a value type, won't comparing to null cause boxing? - or is the jit compiler smart enough to not do that?
					int hash = itemRef == null ? 0 : (comparer.GetHashCode(itemRef) & HighBitNotSet);
					int hashIndex = hash % newIndexArraySize;

					tNodeRef.hashOrNextIndexForBlanks = hash;
					tNodeRef.nextIndex = indexArray[hashIndex];
					tNodeRef.item = itemRef;

					indexArray[hashIndex] = index;
				}
				initialArray = null; // this array can now be garbage collected because it is no longer referenced
			}

			nextBlankIndex = index; // any blank item with a 0 nextIndex pointer means it really points to the next one after it
			firstBlankAtEndIndex = index;

			isHashing = true;
		}

		// return the node found, or 0 if not found
		private int AddToHashSetIfNotFoundZeroEndsChain(in T item, int hash)
		{
			int hashIndex = hash % indexArray.Length;

			for (int index = indexArray[hashIndex]; index != 0; )
			{
				ref TNode tNodeRef = ref nodeArray[index];

				if (tNodeRef.hashOrNextIndexForBlanks == hash && comparer.Equals(tNodeRef.item, item))
				{
					return index; // item was found, so return false to indicate it was not added
				}

				index = tNodeRef.nextIndex;
			}

			if (nextBlankIndex >= nodeArray.Length)
			{
				// there aren't any more blank nodes to add items, so we need to increase capacity
				IncreaseCapacity();
				ResizeIndexArrayForward(GetNewIndexArraySize());

				// fix things messed up by indexArray resize
				hashIndex = hash % indexArray.Length;
			}

			int firstIndex = indexArray[hashIndex];
			indexArray[hashIndex] = nextBlankIndex;

			ref TNode tNodeRefBlank = ref nodeArray[nextBlankIndex];
			if (nextBlankIndex >= firstBlankAtEndIndex)
			{
				// the blank nodes starting at firstBlankAtEndIndex aren't chained
				nextBlankIndex = ++firstBlankAtEndIndex;
			}
			else
			{
				// the blank nodes before firstBlankAtEndIndex are chained (the hashOrNextIndexForBlanks points to the next blank node)
				nextBlankIndex = tNodeRefBlank.hashOrNextIndexForBlanks;
			}

			tNodeRefBlank.hashOrNextIndexForBlanks = hash;
			tNodeRefBlank.nextIndex = firstIndex;
			tNodeRefBlank.item = item;

			usedItemsCount++;

			return 0; // item was not found, so return 0 to indicate not found
		}

		public bool AddHighBit(T item)
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			if (isHashing)
			{
				int hash = item == null ? 0 : comparer.GetHashCode(item);
				int hashIndex = (hash & HighBitNotSet) % indexArray.Length;

				int index = indexArray[hashIndex];
				if (index == 0) // 0 means item does not yet exist in the HashSet, so add it
				{
					if (nextBlankIndex >= nodeArray.Length)
					{
						// there aren't any more blank nodes to add items, so we need to increase capacity
						IncreaseCapacity();
						//ResizeIndexArrayForward(GetNewIndexArraySize());
						//TNode[] newNodeArray = IncreaseCapacityNoCopy();
						//if (newNodeArray == null)
						//{
						//	ResizeIndexArrayForward(GetNewIndexArraySize());
						//}
						//else
						//{
						//	TNode[] oldNodeArray = nodeArray;
						//	nodeArray = newNodeArray;
						//	ResizeIndexArrayForwardAndCopy(GetNewIndexArraySize(), oldNodeArray);
						//}
						//increasedCap = true;
					}
					indexArray[hashIndex] = nextBlankIndex;

					ref TNode tNodeRefBlank = ref nodeArray[nextBlankIndex];
					if (nextBlankIndex >= firstBlankAtEndIndex)
					{
						// the blank nodes starting at firstBlankAtEndIndex aren't chained

						tNodeRefBlank.nextIndex = nextBlankIndex;
						tNodeRefBlank.hashOrNextIndexForBlanks = hash;
						tNodeRefBlank.item = item;

						nextBlankIndex = ++firstBlankAtEndIndex;
					}
					else
					{
						int nextForBlanks = tNodeRefBlank.hashOrNextIndexForBlanks;

						tNodeRefBlank.nextIndex = nextBlankIndex;
						tNodeRefBlank.hashOrNextIndexForBlanks = hash;
						tNodeRefBlank.item = item;

						// the blank nodes before firstBlankAtEndIndex are chained (the hashOrNextIndexForBlanks points to the next blank node)
						nextBlankIndex = nextForBlanks;
					}
				}
				else
				{
					// item with same hashIndex already exists, so need to put it in the bucket if it doesn't already exist there (using Equals)

					#if Add_Item_To_Front_Of_Chain
					int firstIndex = index;
					#endif

					ref TNode tNodeRef = ref nodeArray[index];
					while (true)
					{

						// check if hash codes are equal before calling Equals (this may be faster to compare items that aren't equal, but slower for items that are equal)
						// items that are Equal must have the same hash code
						#if Exclude_Comparing_Cached_Hash
						if (comparer.Equals(tNodeRef.item, item))
						#else
						if (tNodeRef.hashOrNextIndexForBlanks == hash && comparer.Equals(tNodeRef.item, item))
						#endif
						{
							return false; // item was found, so return false to indicate it was not added
						}

						int nextIndex = tNodeRef.nextIndex;
						if (nextIndex == index)
						{
							// a nextIndex that equals this index indicates the end of the items with the same hashIndex, this way all blanks can be determined by a nextIndex = 0
							break; // item was not found, so add it
						}
						else
						{
							index = nextIndex;
							tNodeRef = ref nodeArray[index];
						}
					}

					if (nextBlankIndex >= nodeArray.Length)
					{
						IncreaseCapacity();
						//ResizeIndexArrayForward(GetNewIndexArraySize());
						//TNode[] newNodeArray = IncreaseCapacityNoCopy();
						//if (newNodeArray == null)
						//{
						//	ResizeIndexArrayForward(GetNewIndexArraySize());
						//}
						//else
						//{
						//	TNode[] oldNodeArray = nodeArray;
						//	nodeArray = newNodeArray;
						//	ResizeIndexArrayForwardAndCopy(GetNewIndexArraySize(), oldNodeArray);
						//}
						//increasedCap = true;
						#if Add_Item_To_Front_Of_Chain
						#else
						// it's likely that the nodeArray reference changed with the increase in capacity, so set tNodeRef within the new nodeArray
						//??? do we want to keep a reference to the old nodeArray until this point - this may prevent the old nodeArray from being garbage collected until the following code executes
						tNodeRef = ref nodeArray[index];
						#endif
					}

					#if Add_Item_To_Front_Of_Chain
					indexArray[hashIndex] = nextBlankIndex;
					#else
					tNodeRef.nextIndex = nextBlankIndex;
					#endif
	
					ref TNode tNodeRefBlank = ref nodeArray[nextBlankIndex];
					if (nextBlankIndex >= firstBlankAtEndIndex)
					{
						// the blank nodes starting at firstBlankAtEndIndex aren't chained

						#if Add_Item_To_Front_Of_Chain
						tNodeRefBlank.nextIndex = firstIndex;
						#else
						tNodeRefBlank.nextIndex = nextBlankIndex; // end of chain points to itself
						#endif

						tNodeRefBlank.hashOrNextIndexForBlanks = hash;

						tNodeRefBlank.item = item;

						nextBlankIndex = ++firstBlankAtEndIndex;
					}
					else
					{
						//nodeArray[nextBlankIndex] = new TNode(item, firstIndex, hash);
						#if Add_Item_To_Front_Of_Chain
						tNodeRefBlank.nextIndex = firstIndex;
						#else
						tNodeRefBlank.nextIndex = nextBlankIndex; // end of chain points to itself
						#endif

						int nextForBlanks = tNodeRefBlank.hashOrNextIndexForBlanks;

						//Debug.Assert(tNodeRefFirst.nextIndex != 0, "nextIndex == 0");
						tNodeRefBlank.hashOrNextIndexForBlanks = hash;

						tNodeRefBlank.item = item;

						// the blank nodes before firstBlankAtEndIndex are chained (the hashOrNextIndexForBlanks points to the next blank node)
						nextBlankIndex = nextForBlanks;
					}

					//??? don't add the node at the end of the list, always add it to the front, this way we are always consistent since adding to the front is used when adding items that we know are unique so we don't have to traverse the chain
					//nodeArray[firstNodeIndex].nextIndex = nextBlankIndex;
				}

				usedItemsCount++;

				if (usedItemsCount >= usedItemsLoadFactorThreshold)
				//if (increasedCap)
				{
					//System.Diagnostics.Debug.Assert(increasedCap, "increasedCap = false before calling ResizeIndexArray");
					ResizeIndexArrayForward(GetNewIndexArraySize());
				}

				return true;
			}
			else
			{
				int i;
				for (i = 0; i < usedItemsCount; i++)
				{
					if (comparer.Equals(item, initialArray[i]))
					{
						return false;
					}
				}

				if (i == initialArray.Length)
				{
					SwitchFromArrayToHashing();
					return AddToHashSetIfNotFound(in item, item == null ? 0 : comparer.GetHashCode(item), out int addedNodeIndex);
				}
				else
				{
					// add to initialArray
					initialArray[i] = item;
					usedItemsCount++;
					return true;
				}
			}
		}


		// return true if the item was added or false if it was found
		//??? could return the addedOrFoundNodeIndex and then you would know it was added if not 0 - also change AddToHashSetIfNotFoundAndMark
		private bool AddToHashSetIfNotFound(in T item, int hash, out int addedOrFoundNodeIndex)
		{
			int hashIndex = (hash & HighBitNotSet) % indexArray.Length;

			int index = indexArray[hashIndex];
			if (index == 0) // 0 means item does not yet exist in the HashSet, so add it
			{
				if (nextBlankIndex >= nodeArray.Length)
				{
					// there aren't any more blank nodes to add items, so we need to increase capacity
					IncreaseCapacity();
				}
				indexArray[hashIndex] = nextBlankIndex;

				addedOrFoundNodeIndex = nextBlankIndex;

				if (nextBlankIndex >= firstBlankAtEndIndex)
				{
					// the blank nodes starting at firstBlankAtEndIndex aren't chained
					firstBlankAtEndIndex++;

					nodeArray[nextBlankIndex] = new TNode(item, nextBlankIndex, hash);

					nextBlankIndex = firstBlankAtEndIndex;
				}
				else
				{
					int nextForBlanks = nodeArray[nextBlankIndex].hashOrNextIndexForBlanks;

					nodeArray[nextBlankIndex] = new TNode(item, nextBlankIndex, hash);

					// the blank nodes before firstBlankAtEndIndex are chained (the hashOrNextIndexForBlanks points to the next blank node)
					nextBlankIndex = nextForBlanks;
				}
			}
			else
			{
				// item with same hashIndex already exists, so need to put it in the bucket if it doesn't already exist there (using Equals)

				int firstIndex = index;
				while (true)
				{
					ref TNode tNodeRef = ref nodeArray[index];

					// check if hash codes are equal before calling Equals (this may be faster to compare items that aren't equal, but slower for items that are equal)
					// items that are Equal must have the same hash code
					#if Exclude_Comparing_Cached_Hash
					if (comparer.Equals(tNodeRef.item, item))
					#else
					if (tNodeRef.hashOrNextIndexForBlanks == hash && comparer.Equals(tNodeRef.item, item))
					#endif
					{
						addedOrFoundNodeIndex = index;
						return false; // item was found, so return false to indicate it was not added
					}

					int nextIndex = tNodeRef.nextIndex;
					if (nextIndex == index)
					{
						// a nextIndex that equals this index indicates the end of the items with the same hashIndex, this way all blanks can be determined by a nextIndex = 0
						break; // item was not found, so add it
					}
					else
					{
						index = nextIndex;
					}
				}

				if (nextBlankIndex >= nodeArray.Length)
				{
					IncreaseCapacity();
					//AddToHashSet(item, hash, out addedNodeIndex, false);
					//return true;
				}
				indexArray[hashIndex] = nextBlankIndex;
	
				addedOrFoundNodeIndex = nextBlankIndex;

				if (nextBlankIndex >= firstBlankAtEndIndex)
				{
					// the blank nodes starting at firstBlankAtEndIndex aren't chained
					firstBlankAtEndIndex++;
	
					nodeArray[nextBlankIndex] = new TNode(item, firstIndex, hash);

					nextBlankIndex = firstBlankAtEndIndex;
				}
				else
				{
					int nextForBlanks = nodeArray[nextBlankIndex].hashOrNextIndexForBlanks;

					nodeArray[nextBlankIndex] = new TNode(item, firstIndex, hash);

					// the blank nodes before firstBlankAtEndIndex are chained (the hashOrNextIndexForBlanks points to the next blank node)
					nextBlankIndex = nextForBlanks;
				}

				//??? don't add the node at the end of the list, always add it to the front, this way we are always consistent since adding to the front is used when adding items that we know are unique so we don't have to traverse the chain
				//nodeArray[firstNodeIndex].nextIndex = nextBlankIndex;
			}

			usedItemsCount++;

			if (usedItemsCount >= usedItemsLoadFactorThreshold)
			{
				ResizeIndexArrayReverse(GetNewIndexArraySize());
			}

			return true;
		}

		// this is the same as AddToHashSet except that it marks any new item and it allows for marks in the nextIndex value
		private bool AddToHashSetIfNotFoundAndMark(in T item, int hash, out int addedOrFoundNodeIndex)
		{
			int hashIndex = (hash & HighBitNotSet) % indexArray.Length;

			int index = indexArray[hashIndex];
			if (index == 0) // 0 means item does not yet exist in the HashSet, so add it
			{
				if (nextBlankIndex >= nodeArray.Length)
				{
					// there aren't any more blank nodes to add items, so we need to increase capacity
					IncreaseCapacity();
				}
				indexArray[hashIndex] = nextBlankIndex;

				addedOrFoundNodeIndex = nextBlankIndex;

				if (nextBlankIndex >= firstBlankAtEndIndex)
				{
					// the blank nodes starting at firstBlankAtEndIndex aren't chained
					firstBlankAtEndIndex++;

					nodeArray[nextBlankIndex] = new TNode(item, nextBlankIndex | MarkNextIndexBitMask, hash);

					nextBlankIndex = firstBlankAtEndIndex;
				}
				else
				{
					int nextForBlanks = nodeArray[nextBlankIndex].hashOrNextIndexForBlanks;

					nodeArray[nextBlankIndex] = new TNode(item, nextBlankIndex | MarkNextIndexBitMask, hash);

					// the blank nodes before firstBlankAtEndIndex are chained (the hashOrNextIndexForBlanks points to the next blank node)
					nextBlankIndex = nextForBlanks;
				}
			}
			else
			{
				// item with same hashIndex already exists, so need to put it in the bucket if it doesn't already exist there (using Equals)

				int firstIndex = index;
				while (true)
				{
					ref TNode tNodeRef = ref nodeArray[index];

					// check if hash codes are equal before calling Equals (this may be faster to compare items that aren't equal, but slower for items that are equal)
					// items that are Equal must have the same hash code
					#if Exclude_Comparing_Cached_Hash
					if (comparer.Equals(tNodeRef.item, item))
					#else
					if (tNodeRef.hashOrNextIndexForBlanks == hash && comparer.Equals(tNodeRef.item, item))
					#endif
					{
						addedOrFoundNodeIndex = index;
						return false; // item was found, so return false to indicate it was not added
					}

					int nextIndex = tNodeRef.nextIndex & MarkNextIndexBitMaskInverted;
					if (nextIndex == index)
					{
						// a nextIndex that equals this index indicates the end of the items with the same hashIndex, this way all blanks can be determined by a nextIndex = 0
						break; // item was not found, so add it
					}
					else
					{
						index = nextIndex;
					}
				}

				if (nextBlankIndex >= nodeArray.Length)
				{
					IncreaseCapacity();
					//AddToHashSet(item, hash, out addedNodeIndex, false);
					//return true;
				}
				indexArray[hashIndex] = nextBlankIndex;

				addedOrFoundNodeIndex = nextBlankIndex;

				if (nextBlankIndex >= firstBlankAtEndIndex)
				{
					// the blank nodes starting at firstBlankAtEndIndex aren't chained
					firstBlankAtEndIndex++;
	
					nodeArray[nextBlankIndex] = new TNode(item, firstIndex | MarkNextIndexBitMask, hash);

					nextBlankIndex++;
				}
				else
				{
					int nextForBlanks = nodeArray[nextBlankIndex].hashOrNextIndexForBlanks;

					nodeArray[nextBlankIndex] = new TNode(item, firstIndex | MarkNextIndexBitMask, hash);

					// the blank nodes before firstBlankAtEndIndex are chained (the hashOrNextIndexForBlanks points to the next blank node)
					nextBlankIndex = nextForBlanks;
				}
				
				//??? don't add the node at the end of the list, always add it to the front, this way we are always consistent since adding to the front is used when adding items that we know are unique so we don't have to traverse the chain
				//nodeArray[firstNodeIndex].nextIndex = nextBlankIndex;
			}

			usedItemsCount++;

			if (usedItemsCount >= usedItemsLoadFactorThreshold)
			{
				ResizeIndexArrayWithMarks(GetNewIndexArraySize());
			}

			return true;
		}

		// this is only present to implement ICollection<T>
		//??? for some reason the Contains with the in param value doesn't implement this even though they can be called the same way
		// i'm not sure this will be inlined? - also will by readonly ref (in) param be faster for ref types or very small values types (int) - probably not - so this might be slower for them
		public bool Contains(T item)
		{
			if (isHashing)
			{
				return FindInNodeArray(in item, item == null ? 0 : comparer.GetHashCode(item));
			}
			else
			{
				return FindInInitialArray(in item);
			}
		}

		public bool Contains(in T item)
		{
			if (isHashing)
			{
				return FindInNodeArray(in item, item == null ? 0 : comparer.GetHashCode(item));
			}
			else
			{
				return FindInInitialArray(in item);
			}
		}

		// only remove the found item if the predicate on the found item evaluates to true
		//??? look at HashSet<T>.RemoveWhere
		// this can be used to remove items if their reference count = 0 - but it really isn't good to use this that way because we want to call Find, then see if the ref count goes to 0 and then remove if it does, but otherwise just decrease the ref count
		// so maybe this should return a reference to the found item instead of bool (like the Find call does)
		public bool RemoveIf(T item, Predicate<T> pred)
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			bool isRemoved = false;

			return isRemoved;
		}

		public bool Remove(T item)
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			bool isRemoved = false;

			if (isHashing)
			{
				// ??? don't call a function, just do the same thing as the code and then do the remove - this will be faster
				FindInNodeArray(item, out int foundNodeIndex, out int priorNodeIndex, out int indexArrayIndex);
				if (foundNodeIndex > 0)
				{
					if (priorNodeIndex == 0)
					{
						if (nodeArray[foundNodeIndex].nextIndex == foundNodeIndex) // there are no more nodes in the chain - this was the only node, because the if above is only true when there is no prior node
						{
							indexArray[indexArrayIndex] = 0;
						}
						else
						{
							indexArray[indexArrayIndex] = nodeArray[foundNodeIndex].nextIndex;
						}
					}
					else
					{
						if (nodeArray[foundNodeIndex].nextIndex == foundNodeIndex) // the node being removed was the last node in a chain
						{
							nodeArray[priorNodeIndex].nextIndex = priorNodeIndex; // make the prior node the last node in the chain
						}
						else
						{
							nodeArray[priorNodeIndex].nextIndex = nodeArray[foundNodeIndex].nextIndex;
						}
					}

					// add node to blank chain or to the blanks at the end (if possible)
					if (foundNodeIndex == firstBlankAtEndIndex - 1)
					{
						if (nextBlankIndex == firstBlankAtEndIndex)
						{
							nextBlankIndex--;
						}
						firstBlankAtEndIndex--;
					}
					else
					{
						nodeArray[foundNodeIndex].hashOrNextIndexForBlanks = nextBlankIndex;
						nextBlankIndex = foundNodeIndex;
					}

					nodeArray[foundNodeIndex].nextIndex = 0;

					usedItemsCount--;
					isRemoved = true;
				}
			}
			else
			{
				for (int i = 0; i < usedItemsCount; i++)
				{
					if (comparer.Equals(item, initialArray[i]))
					{
						// remove the item by moving all remaining items to fill over this one
						for (int j = i + 1; j < usedItemsCount; j++, i++)
						{
							initialArray[i] = initialArray[j];
						}
						usedItemsCount--;
						isRemoved = true;
						break;
					}
				}
			}
			return isRemoved;
		}

		//??? do we need the same FindOrAdd for a reference type? or does this function take care of that?
		//??? don't do isAdded, do isFound because of Find below
		public ref T FindOrAdd(in T item, out bool isFound)
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			isFound = false;
			if (isHashing)
			{
				isFound = !AddToHashSetIfNotFound(in item, item == null ? 0 : comparer.GetHashCode(item), out int addedNodeIndex);
				return ref nodeArray[addedNodeIndex].item;
			}
			else
			{
				int i;
				for (i = 0; i < usedItemsCount; i++)
				{
					if (comparer.Equals(item, initialArray[i]))
					{
						isFound = true;
						return ref initialArray[i];
					}
				}

				if (i == initialArray.Length)
				{
					SwitchFromArrayToHashing();
					return ref FindOrAdd(in item, out isFound);
				}
				else
				{
					// add to initialArray and keep isAdded true
					initialArray[i] = item;
					usedItemsCount++;
					return ref initialArray[i];
				}
			}
		}

		// return index into nodeArray or 0 if not found

		//??? to make things faster, could have a FindInNodeArray that just returns foundNodeIndex and another version called FindWithPriorInNodeArray that has the 3 out params
		// first test to make sure this works as is
		private void FindInNodeArray(in T item, out int foundNodeIndex, out int priorNodeIndex, out int indexArrayIndex)
		{
			foundNodeIndex = 0;
			priorNodeIndex = 0;

			int hash = item == null ? 0 : comparer.GetHashCode(item);
			int hashIndex = (hash & HighBitNotSet) % indexArray.Length;

			int index = indexArray[hashIndex];
			indexArrayIndex = hashIndex;

			int priorIndex = 0;
			if (index > 0) // 0 means item does not yet exist in the HashSet
			{
				// item with same hashIndex already exists, so need to look in the bucket for an equal item (using Equals)

				while (true)
				{
					// check if hash codes are equal before calling Equals (which may take longer) items that are Equals must have the same hash code
					#if Exclude_Comparing_Cached_Hash
					if (comparer.Equals(nodeArray[index].item, item))
					#else
					if (nodeArray[index].hashOrNextIndexForBlanks == hash && comparer.Equals(nodeArray[index].item, item))
					#endif
					{
						foundNodeIndex = index;
						priorNodeIndex = priorIndex;
						return;
					}

					int nextIndex = nodeArray[index].nextIndex;
					if (nextIndex == index)
					{
						return; // not found
					}
					else
					{
						priorIndex = index;
						index = nextIndex;
					}
				}
			}
		}

		private bool FindInNodeArray(in T item, int hash)
		{
			int hashIndex = (hash & HighBitNotSet) % indexArray.Length;

			int index = indexArray[hashIndex];

			if (index > 0) // 0 means item does not yet exist in the HashSet
			{
				// item with same hashIndex already exists, so need to look in the bucket for an equal item (using Equals)

				while (true)
				{
					// check if hash codes are equal before calling Equals (which may take longer) items that are Equals must have the same hash code
					#if Exclude_Comparing_Cached_Hash
					if (comparer.Equals(nodeArray[index].item, item))
					#else
					if (nodeArray[index].hashOrNextIndexForBlanks == hash && comparer.Equals(nodeArray[index].item, item))
					#endif
					{
						return true;
					}

					int nextIndex = nodeArray[index].nextIndex;
					if (nextIndex == index)
					{
						return false; // not found
					}
					else
					{
						index = nextIndex;
					}
				}
			}
			return false;
		}

		private bool FindInInitialArray(in T item)
		{
			for (int i = 0; i < usedItemsCount; i++)
			{
				if (comparer.Equals(item, initialArray[i]))
				{
					// if here then item was not found
					return true;
				}
			}
			return false;
		}

		private void UnmarkAllNextIndexValues(int maxNodeIndex)
		{
			// must be hashing to be here
			for (int i = 1; i <= maxNodeIndex; i++)
			{
				nodeArray[i].nextIndex &= MarkNextIndexBitMaskInverted;
			}
		}

		// removeMarked = true, means remove the marked items and keep the unmarked items
		// removeMarked = false, means remove the unmarked items and keep the marked items
		private void UnmarkAllNextIndexValuesAndRemoveAnyMarkedOrUnmarked(bool removeMarked)
		{
			// must be hashing to be here

			// must traverse all of the chains instead of just looping through the nodeArray because going through the chains is the only way to set
			// nodes within a chain to blank and still be able to remove the blnak node from the chain

			int index;
			int nextIndex;
			int priorIndex;
			int lastNonBlankIndex = firstBlankAtEndIndex - 1;
			for (int i = 0; i < indexArray.Length; i++)
			{
				priorIndex = 0; // 0 means use indexArray
				index = indexArray[i];
				if (index != 0)
				{
					while (true)
					{
						ref TNode tNodeRef = ref nodeArray[index];
						nextIndex = tNodeRef.nextIndex;
						bool isMarked = (nextIndex & MarkNextIndexBitMask) != 0;
						if (isMarked)
						{
							// this node is marked, so unmark it
							nextIndex &= MarkNextIndexBitMaskInverted;
							tNodeRef.nextIndex = nextIndex;
						}

						if (removeMarked == isMarked)
						{
							// set this node to blank

							usedItemsCount--;

							// first try to set it to blank by adding it to the blank at end group
							if (index == lastNonBlankIndex)
							{
								//??? does it make sense to attempt this because any already blank items before this will not get added to the 
								lastNonBlankIndex--;
								if (nextBlankIndex == firstBlankAtEndIndex)
								{
									nextBlankIndex--;
								}
								firstBlankAtEndIndex--;
							}
							else
							{
								// add to the blank group

								tNodeRef.nextIndex = 0;

								tNodeRef.hashOrNextIndexForBlanks = nextBlankIndex;
								nextBlankIndex = index;
							}

							if (priorIndex == 0)
							{
								if (nextIndex == index)
								{
									indexArray[i] = 0;
								}
								else
								{
									indexArray[i] = nextIndex;
								}
							}
							else
							{
								nodeArray[priorIndex].nextIndex = nextIndex;
							}

							// keep priorIndex the same because we removed the node in the chain, so the priorIndex is still the same value
						}
						else
						{
							priorIndex = index; // node was not removed from the chain, so the priorIndex now points to the node that was not removed
						}

						if (nextIndex == index)
						{
							break;
						}
						index = nextIndex;
					}
				}
			}
		}

		private FoundType FindInNodeArrayAndMark(in T item, out int foundNodeIndex)
		{
			int hash = item == null ? 0 : comparer.GetHashCode(item);
			int hashIndex = (hash & HighBitNotSet) % indexArray.Length;

			int index = indexArray[hashIndex];

			if (index == 0) // 0 means item does not yet exist in the HashSet
			{
				foundNodeIndex = 0;
				return FoundType.NotFound;
			}
			else
			{
				// item with same hashIndex already exists, so need to look in the chained list for an equal item (using Equals)

				int nextIndex;
				while (true)
				{
					nextIndex = nodeArray[index].nextIndex;

					// check if hash codes are equal before calling Equals (which may take longer) items that are Equals must have the same hash code
					#if Exclude_Comparing_Cached_Hash
					if (comparer.Equals(nodeArray[index].item, item))
					#else
					if (nodeArray[index].hashOrNextIndexForBlanks == hash && comparer.Equals(nodeArray[index].item, item))
					#endif
					{
						foundNodeIndex = index;
						if ((nextIndex & MarkNextIndexBitMask) == 0)
						{
							// not marked, so mark it
							nodeArray[index].nextIndex |= MarkNextIndexBitMask;

							return FoundType.FoundFirstTime;
						}
						return FoundType.FoundNotFirstTime;
					}

					nextIndex &= MarkNextIndexBitMaskInverted;
					if (nextIndex == index)
					{
						foundNodeIndex = 0;
						return FoundType.NotFound; // not found
					}
					else
					{
						index = nextIndex;
					}
				}
			}
		}

		public void FindAndRemoveIf(Predicate<T> match, Predicate<T> removeIf, out bool isFound)
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			isFound = false;

			//??? implement this
		}

		public void FindAndRemove(in T item, Predicate<T> removeIf, out bool isFound)
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			isFound = false;

			//??? implement this
		}

		public ref T FindFirstIf(Predicate<T> match, out bool isFound)
		{
			isFound = false;

			//??? implement this

			return ref initialArray[0];
		}

		// this is similar to HashSet<T>.TryGetValue, except it returns a ref to the value rather than a copy of the value found (using an out parameter)
		// this way you can modify the actual value in the set if it is a value type (you can always modify the object if it is a reference type - except I think if it is a string)
		// also passing the item by in and the return by ref is faster for larger structs than passing by value
		public ref T Find(in T item, out bool isFound)
		{
			isFound = false;
			if (isHashing)
			{
				FindInNodeArray(item, out int foundNodeIndex, out int priorNodeIndex, out int indexArrayIndex);
				if (foundNodeIndex > 0)
				{
					isFound = true;
				}

				return ref nodeArray[foundNodeIndex].item; // if not found, then return a ref to the first node item, which isn't used for anything other than this
			}
			else
			{
				int i;
				for (i = 0; i < usedItemsCount; i++)
				{
					if (comparer.Equals(item, initialArray[i]))
					{
						isFound = true;
						return ref initialArray[i];
					}
				}

				// if item was not found, still need to return a ref to something, so return a ref to the first item in the array
				return ref initialArray[0];
			}
		}

		public bool TryGetValue(T equalValue, out T actualValue)
		{
			if (isHashing)
			{
				FindInNodeArray(equalValue, out int foundNodeIndex, out int priorNodeIndex, out int indexArrayIndex);
				if (foundNodeIndex > 0)
				{
					actualValue = nodeArray[foundNodeIndex].item;
					return true;
				}

				actualValue = default;
				return false;
			}
			else
			{
				int i;
				for (i = 0; i < usedItemsCount; i++)
				{
					if (comparer.Equals(equalValue, initialArray[i]))
					{
						actualValue = initialArray[i];
						return true;
					}
				}
			}

			actualValue = default;
			return false;
		}

		public void UnionWith(IEnumerable<T> other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other), "Value cannot be null.");
			}

			//??? Note: HashSet doesn't seem to increment this unless it really changes something - like doing an Add(3) when 3 is already in the hashset doesn't increment, same as doing a UnionWith with an empty set as the param
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif
					   
			if (other == this)
			{
				return;
			}

			//??? maybe there is a faster way to add a bunch at one time - I copied the Add code below to make this faster
			//foreach (T item in range)
			//{
			//	Add(item);
			//}

			// do this with more code because it might get called in some high performance situations

			int addedNodeIndex;

			if (isHashing)
			{
				foreach (T item in other)
				{
					AddToHashSetIfNotFound(in item, item == null ? 0 : comparer.GetHashCode(item), out addedNodeIndex);
				}
			}
			else
			{
				int i;

				foreach (T item in other)
				{
					//??? if it's easier for the jit compiler or il compiler to remove the array bounds checking then
					// have i < initialArray.Length and do the check for usedItemsCount within the loop with a break statment
					for (i = 0; i < usedItemsCount; i++)
					{
						if (comparer.Equals(item, initialArray[i]))
						{
							goto found; // break out of inner for loop
						}
					}

					// if here then item was not found
					if (i == initialArray.Length)
					{
						SwitchFromArrayToHashing();
						AddToHashSetIfNotFound(in item, item == null ? 0 : comparer.GetHashCode(item), out addedNodeIndex);
					}
					else
					{
						// add to initialArray
						initialArray[i] = item;
						usedItemsCount++;
					}

			found:;
				}
			}
		}

		public void ExceptWith(IEnumerable<T> other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other), "Value cannot be null.");
			}

			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif
			if (other == this)
			{
				Clear();
			}
			else
			{
				foreach (T item in other)
				{
					Remove(item);
				}
			}
		}

		public void IntersectWith(IEnumerable<T> other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other), "Value cannot be null.");
			}

			if (other == this)
			{
				return;
			}

			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			// if hashing, find each item in the nodeArray and mark anything found, but remove from being found again
			// after

			//??? implement the faster methof if other is HashSet or FastHashSet
			//FastHashSet<T> otherSet = other as FastHashSet<T>;
			//if (otherSet != null && Equals(comparer, otherSet.comparer)) //??? also make sure the comparers are equal - how, they are probably references? - maybe they override Equals?
			//{
			//	//???
			//}

			if (isHashing)
			{
				int foundItemCount = 0; // the count of found items in the hash - without double counting
				foreach (T item in other)
				{
					FoundType foundType = FindInNodeArrayAndMark(in item, out int foundIndex);
					if (foundType == FoundType.FoundFirstTime)
					{
						foundItemCount++;

						if (foundItemCount == usedItemsCount)
						{
							break;
						}
					}
				}

				if (foundItemCount == 0)
				{
					Clear();
				}
				else
				{
					UnmarkAllNextIndexValuesAndRemoveAnyMarkedOrUnmarked(false);
				}
			}
			else
			{
				// Note: we could actually do this faster by moving any found items to the front and keeping track of the found items
				// with a single int index
				// the problem with this method is it reorders items and even though that shouldn't matter in a set
				// it might cause issues with code that incorrectly assumes order stays the same for operations like this

				// possibly a faster implementation would be to use the method above, but keep track of original order with an int array of the size of usedItemsCount (ex. item at 0 was originally 5, and also item at 5 was originally 0)

				byte[] foundItemArray = new byte[usedItemsCount]; // 0 means corresponding item was not yet found, 1 means it was

				int i;

				int foundItemCount = 0; // the count of found items in the hash - without double counting
				foreach (T item in other)
				{
					for (i = 0; i < usedItemsCount; i++)
					{
						if (comparer.Equals(item, initialArray[i]))
						{
							if (foundItemArray[i] == 0)
							{
								foundItemArray[i] = 1;
								foundItemCount++;
							}
							goto found; // break out of inner for loop
						}
					}

			found:
					if (foundItemCount == usedItemsCount)
					{
						// all items in the set were found, so there is nothing to remove - the set isn't changed
						return;
					}
				}

				if (foundItemCount == 0)
				{
					usedItemsCount = 0; // this is the equivalent of calling Clear
				}
				else
				{
					// remove any items that are unmarked (unfound)
					// go backwards because this can be faster
					for (i = usedItemsCount - 1; i >= 0 ; i--)
					{
						if (foundItemArray[i] == 0)
						{
							if (i < usedItemsCount - 1)
							{
								// a faster method if there are multiple unfound items in a row is to find the first used item (make i go backwards until the item is used and then increment i by 1)
								// if there aren't multiple unused in a row, then this is a bit of a waste

								int j = i + 1; // j now points to the next item after the unfound one that we want to keep

								i--;
								while (i >= 0)
								{
									if (foundItemArray[i] == 1)
									{
										break;
									}
									i--;
								}
								i++;

								int k = i;
								for ( ; j < usedItemsCount; j++, k++)
								{
									initialArray[k] = initialArray[j];
								}
							}

							usedItemsCount--;
						}
					}
				}
			}
		}

		//??? surround all of the Is... Sub/Super methods with an #if Allow_Set_Operations so that these can easily be removed if not needed

		// An empty set is a proper subset of any other collection. Therefore, this method returns true if the collection represented by the current HashSet<T> object
		// is empty unless the other parameter is also an empty set.
		// This method always returns false if Count is greater than or equal to the number of elements in other.
		// If the collection represented by other is a HashSet<T> collection with the same equality comparer as the current HashSet<T> object,
		// then this method is an O(n) operation. Otherwise, this method is an O(n + m) operation, where n is Count and m is the number of elements in other.
		public bool IsProperSubsetOf(IEnumerable<T> other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other), "Value cannot be null.");
			}

			if (other == this)
			{
				return false;
			}

			ICollection<T> collection = other as ICollection<T>;
			if (collection != null)
			{
				if (usedItemsCount == 0 && collection.Count > 0)
				{
					return true; // by definition, an empty set is a proper subset of any non-empty collection
				}

				if (usedItemsCount >= collection.Count)
				{
					return false;
				}
			}
			else
			{
				if (usedItemsCount == 0)
				{
					foreach (T item in other)
					{
						//??? is there a faster way to do this? (determine if there are some items in other)
						return true;
					}
					return false;
				}
			}

			//if (collection != null)
			//{

			//	//??? how can this be faster if other is a HashSet or FastHashSet with the same comparer
			//	// don't have to mark because we know all items are unique and with FastHashSet we can re-use the hash
			//}

			if (isHashing)
			{
				int foundItemCount = 0; // the count of found items in the hash - without double counting
				int maxFoundIndex = 0;
				bool notFoundAtLeastOne = false;
				foreach (T item in other)
				{
					FoundType foundType = FindInNodeArrayAndMark(in item, out int foundIndex);
					if (foundType == FoundType.FoundFirstTime)
					{
						foundItemCount++;
						if (maxFoundIndex < foundIndex)
						{
							maxFoundIndex = foundIndex;
						}
					}
					else if (foundType == FoundType.NotFound)
					{
						notFoundAtLeastOne = true;
					}

					if (notFoundAtLeastOne && foundItemCount == usedItemsCount)
					{
						// true means all of the items in the set were found in other and at least one item in other was not found in the set
						break; // will return true below after unmarking
					}
				}

				UnmarkAllNextIndexValues(maxFoundIndex);

				return notFoundAtLeastOne && foundItemCount == usedItemsCount; // true if all of the items in the set were found in other and at least one item in other was not found in the set
			}
			else
			{
				byte[] foundItemArray = new byte[usedItemsCount]; // 0 means corresponding item was not yet found, 1 means it was

				int i;

				int foundItemCount = 0; // the count of found items in the hash - without double counting
				bool notFoundAtLeastOne = false;
				foreach (T item in other)
				{
					for (i = 0; i < usedItemsCount; i++)
					{
						if (comparer.Equals(item, initialArray[i]))
						{
							if (foundItemArray[i] == 0)
							{
								foundItemArray[i] = 1;
								foundItemCount++;
							}
							goto found; // break out of inner for loop
						}
					}

					// if here then item was not found
					notFoundAtLeastOne = true;

			found:
					if (notFoundAtLeastOne && foundItemCount == usedItemsCount)
					{
						// true means all of the items in the set were found in other and at least one item in other was not found in the set
						return true;
					}
				}

				return false;
			}
		}

		public bool IsSubsetOf(IEnumerable<T> other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other), "Value cannot be null.");
			}

			if (other == this)
			{
				return true;
			}

			if (usedItemsCount == 0)
			{
				return true; // by definition, an empty set is a subset of any collection
			}

			ICollection<T> collection = other as ICollection<T>;
			if (collection != null)
			{
				if (usedItemsCount > collection.Count)
				{
					return false;
				}
			}

			//if (collection != null)
			//{

			//	//??? how can this be faster if other is a HashSet or FastHashSet with the same comparer
			//	// don't have to mark because we know all items are unique and with FastHashSet we can re-use the hash
			//}

			if (isHashing)
			{
				int foundItemCount = 0; // the count of found items in the hash - without double counting
				int maxFoundIndex = 0;
				foreach (T item in other)
				{
					FoundType foundType = FindInNodeArrayAndMark(in item, out int foundIndex);
					if (foundType == FoundType.FoundFirstTime)
					{
						foundItemCount++;
						if (maxFoundIndex < foundIndex)
						{
							maxFoundIndex = foundIndex;
						}

						if (foundItemCount == usedItemsCount)
						{
							break;
						}
					}
				}

				UnmarkAllNextIndexValues(maxFoundIndex);

				return foundItemCount == usedItemsCount; // true if all of the items in the set were found in other
			}
			else
			{
				byte[] foundItemArray = new byte[usedItemsCount]; // 0 means corresponding item was not yet found, 1 means it was

				int i;

				int foundItemCount = 0; // the count of found items in the hash - without double counting
				foreach (T item in other)
				{
					for (i = 0; i < usedItemsCount; i++)
					{
						if (comparer.Equals(item, initialArray[i]))
						{
							if (foundItemArray[i] == 0)
							{
								foundItemArray[i] = 1;
								foundItemCount++;
							}
							goto found; // break out of inner for loop
						}
					}

			found:
					if (foundItemCount == usedItemsCount)
					{
						break;
					}
				}

				return foundItemCount == usedItemsCount; // true if all of the items in the set were found in other
			}
		}
		
		public bool IsProperSupersetOf(IEnumerable<T> other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other), "Value cannot be null.");
			}

			if (other == this)
			{
				return false;
			}

			if (usedItemsCount == 0)
			{
				return false; // an empty set can never be a proper superset of anything (not even an empty collection)
			}

			ICollection<T> collection = other as ICollection<T>;
			if (collection != null)
			{
				if (collection.Count == 0)
				{
					return true; // by definition, an empty other means the set is a proper superset of it if the set has at least one value
				}
			}
			else
			{
				foreach (T item in other)
				{
					//??? is there a faster way to do this?
					goto someItemsInOther;
				}
				return true;
			}

someItemsInOther:

			//if (collection != null)
			//{

			//	//??? how can this be faster if other is a HashSet or FastHashSet with the same comparer
			//	// don't have to mark because we know all items are unique and with FastHashSet we can re-use the hash
			//}

			if (isHashing)
			{
				int foundItemCount = 0; // the count of found items in the hash - without double counting
				int maxFoundIndex = 0;
				foreach (T item in other)
				{
					FoundType foundType = FindInNodeArrayAndMark(in item, out int foundIndex);
					if (foundType == FoundType.FoundFirstTime)
					{
						foundItemCount++;
						if (maxFoundIndex < foundIndex)
						{
							maxFoundIndex = foundIndex;
						}

						if (foundItemCount == usedItemsCount)
						{
							break;
						}
					}
					else if (foundType == FoundType.NotFound)
					{
						// any unfound item means this can't be a proper superset of
						UnmarkAllNextIndexValues(maxFoundIndex);
						return false;
					}
				}

				UnmarkAllNextIndexValues(maxFoundIndex);

				return foundItemCount < usedItemsCount; // true if all of the items in other were found in set and at least one item in set was not found in other
			}
			else
			{
				byte[] foundItemArray = new byte[usedItemsCount]; // 0 means corresponding item was not yet found, 1 means it was

				int i;

				int foundItemCount = 0; // the count of found items in the hash - without double counting
				foreach (T item in other)
				{
					for (i = 0; i < usedItemsCount; i++)
					{
						if (comparer.Equals(item, initialArray[i]))
						{
							if (foundItemArray[i] == 0)
							{
								foundItemArray[i] = 1;
								foundItemCount++;
							}
							goto found; // break out of inner for loop
						}
					}

					// if here then item was not found
					return false;

			found:
					if (foundItemCount == usedItemsCount)
					{
						break;
					}
				}

				return foundItemCount < usedItemsCount; // true if all of the items in other were found in set and at least one item in set was not found in other
			}
		}

		public bool IsSupersetOf(IEnumerable<T> other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other), "Value cannot be null.");
			}

			if (other == this)
			{
				return true;
			}

			ICollection<T> collection = other as ICollection<T>;
			if (collection != null)
			{
				if (collection.Count == 0)
				{
					return true; // by definition, an empty other means the set is a superset of it
				}
			}
			else
			{
				foreach (T item in other)
				{
					//??? is there a faster way to do this?
					goto someItemsInOther;
				}
				return true;
			}

someItemsInOther:

			if (usedItemsCount == 0)
			{
				return false; // an empty set can never be a proper superset of anything (except an empty collection - but an empty collection returns true above)
			}

			//if (collection != null)
			//{

			//	//??? how can this be faster if other is a HashSet or FastHashSet with the same comparer
			//	// don't have to mark because we know all items are unique and with FastHashSet we can re-use the hash
			//}

			if (isHashing)
			{
				foreach (T item in other)
				{
					if (!FindInNodeArray(in item, item == null ? 0 : comparer.GetHashCode(item)))
					{
						return false;
					}
				}

				return true; // true if all of the items in other were found in the set, false if at least one item in other was not found in the set
			}
			else
			{
				int i;

				foreach (T item in other)
				{
					for (i = 0; i < usedItemsCount; i++)
					{
						if (comparer.Equals(item, initialArray[i]))
						{
							goto found; // break out of inner for loop
						}
					}

					// if here then item was not found
					return false;

			found:;

				}

				return true; // true if all of the items in other were found in the set, false if at least one item in other was not found in the set
			}
		}

		public bool Overlaps(IEnumerable<T> other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other), "Value cannot be null.");
			}

			if (other == this)
			{
				return usedItemsCount > 0; // return false if there are no items when both sets are the same, otherwise return true when both sets are the same
			}

			//??? maybe could do something like:
			// and then call Contains with in or Contains without in?
			//if (typeof(T).IsValueType) // or maybe typeof(T).GetType().IsValueType so support .net core?
			//{
			//}

			foreach (T item in other)
			{
				if (Contains(in item))
				{
					return true;
				}
			}
			return false;
		}

		public bool SetEquals(IEnumerable<T> other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other), "Value cannot be null.");
			}

			if (other == this)
			{
				return true;
			}

			// if other is ICollection, then it has count

			ICollection c = other as ICollection;

			if (c != null)
			{
				if (c.Count < usedItemsCount)
				{
					return false;
				}

				HashSet<T> hset = other as HashSet<T>;
				if (hset != null && Equals(hset.Comparer, Comparer))
				{
					if (hset.Count != usedItemsCount)
					{
						return false;
					}

					foreach (T item in other)
					{
						if (!Contains(in item))
						{
							return false;
						}
					}
					return true;
				}

				FastHashSet<T> fhset = other as FastHashSet<T>;
				if (fhset != null && Equals(fhset.Comparer, Comparer))
				{
					if (fhset.Count != usedItemsCount)
					{
						return false;
					}

					if (isHashing)
					{
						int pastNodeIndex = nodeArray.Length;
						if (firstBlankAtEndIndex < pastNodeIndex)
						{
							pastNodeIndex = firstBlankAtEndIndex;
						}

						if (fhset.isHashing)
						{
							for (int i = 1; i < pastNodeIndex; i++)
							{
								if (nodeArray[i].nextIndex != 0) // skip any blank nodes
								{
									if (!fhset.FindInNodeArray(in nodeArray[i].item, nodeArray[i].hashOrNextIndexForBlanks))
									{
										return false;
									}
								}
							}
						}
						else
						{
							for (int i = 1; i < pastNodeIndex; i++)
							{
								if (nodeArray[i].nextIndex != 0) // skip any blank nodes
								{
									if (!fhset.FindInInitialArray(in nodeArray[i].item))
									{
										return false;
									}
								}
							}
						}
					}
					else
					{
						foreach (T item in other)
						{
							if (!FindInInitialArray(in item))
							{
								return false;
							}
						}
					}
					return true;
				}
			}

			if (isHashing)
			{
				int foundItemCount = 0; // the count of found items in the hash - without double counting
				int maxFoundIndex = 0;
				foreach (T item in other)
				{
					FoundType foundType = FindInNodeArrayAndMark(in item, out int foundIndex);
					if (foundType == FoundType.FoundFirstTime)
					{
						foundItemCount++;
						if (maxFoundIndex < foundIndex)
						{
							maxFoundIndex = foundIndex;
						}
					}
					else if (foundType == FoundType.NotFound)
					{
						UnmarkAllNextIndexValues(maxFoundIndex);
						return false;
					}
				}

				UnmarkAllNextIndexValues(maxFoundIndex);

				return foundItemCount == usedItemsCount;
			}
			else
			{
				byte[] foundItemArray = new byte[usedItemsCount]; // 0 means corresponding item was not yet found, 1 means it was

				int i;

				int foundItemCount = 0; // the count of found items in the hash - without double counting
				foreach (T item in other)
				{
					for (i = 0; i < usedItemsCount; i++)
					{
						if (comparer.Equals(item, initialArray[i]))
						{
							if (foundItemArray[i] == 0)
							{
								foundItemArray[i] = 1;
								foundItemCount++;
							}
							goto found; // break out of inner for loop
						}
					}
					// if here then item was not found
					return false;
			found:;
				}

				return foundItemCount == usedItemsCount;
			}
		}

		// From the online document: Modifies the current HashSet<T> object to contain only elements that are present either in that object or in the specified collection, but not both.
		public void SymmetricExceptWith(IEnumerable<T> other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other), "Value cannot be null.");
			}

			if (other == this)
			{
				Clear();
			}

			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			//??? implement the faster methof if other is HashSet or FastHashSet
			//FastHashSet<T> otherSet = other as FastHashSet<T>;
			//if (otherSet != null && Equals(comparer, otherSet.comparer)) //??? also make sure the comparers are equal - how, they are probably references? - maybe they override Equals?
			//{
			//	//???
			//}

			if (!isHashing)
			{
				// to make things easier for now, just switch to hashing if calling this function and deal with only one set of code
				SwitchFromArrayToHashing();
			}

			//DebugOutput.OutputSortedEnumerableItems(other, nameof(other));
			//DebugOutput.OutputSortedEnumerableItems(this, "this set");

			//System.Diagnostics.Debug.WriteLine("other");
			//List<T> lst = new List<T>(other);
			//lst.Sort();
			//foreach (T item in lst)
			//{
			//	System.Diagnostics.Debug.WriteLine(item.ToString());
			//}

			//System.Diagnostics.Debug.WriteLine("");
			//System.Diagnostics.Debug.WriteLine("set");
			//List<T> lst2 = new List<T>(this);
			//lst2.Sort();
			//foreach (T item in lst2)
			//{
			//	System.Diagnostics.Debug.WriteLine(item.ToString());
			//}

			// for the first loop through other, add any unfound items and mark
			//int foundItemCount = 0; // the count of found items in the hash - without double counting
			int addedNodeIndex;
			int maxAddedNodeIndex = 0;
			foreach (T item in other)
			{
				AddToHashSetIfNotFoundAndMark(in item, item == null ? 0 : comparer.GetHashCode(item), out addedNodeIndex);
				if (addedNodeIndex > 0)
				{
					if (addedNodeIndex > maxAddedNodeIndex)
					{
						maxAddedNodeIndex = addedNodeIndex;
					}
				}
			}

			foreach (T item in other)
			{
				RemoveIfNotMarked(in item);
			}

			UnmarkAllNextIndexValues(maxAddedNodeIndex);
			//}
			//else
			//{
			//	// Note: we could actually do this faster by moving any found items to the front and keeping track of the found items
			//	// with a single int index
			//	// the problem with this method is it reorders items and even though that shouldn't matter in a set
			//	// it might cause issues with code that incorrectly assumes order stays the same for operations like this

			//	// possibly a faster implementation would be to use the method above, but keep track of original order with an int array of the size of usedItemsCount (ex. item at 0 was originally 5, and also item at 5 was originally 0)


			//	int i;

			//	int originalUsedItemsCount = usedItemsCount;

			//	foreach (T item in other)
			//	{
			//		for (i = 0; i < usedItemsCount; i++)
			//		{
			//			if (comparer == null ? item.Equals(initialArray[i]) : comparer.Equals(item, initialArray[i]))
			//			{
			//				goto found; // break out of inner for loop
			//			}
			//		}

			//		// not found, so add to the end
			//		if (usedItemsCount < initialArray.Length)
			//		{
			//			usedItemsCount++;
			//		}
			//		else
			//		{
			//			// must switch from using initialArray to hashing, but also mark all of the items at index originalUsedItemsCount and after and
			//			// return the largest index of the marked items

			//		}
			//found:;
			//	}

			//	// remove any items that are unmarked (unfound)
			//	// go backwards because this can be faster
			//	for (i = usedItemsCount - 1; i >= 0 ; i--)
			//	{
			//		if (foundItemArray[i] == 0)
			//		{
			//			if (i < usedItemsCount - 1)
			//			{
			//				// a faster method if there are multiple unfound items in a row is to find the first used item (make i go backwards until the item is used and then increment i by 1)
			//				// if there aren't multiple unused in a row, then this is a bit of a waste

			//				int j = i + 1; // j now points to the next item after the unfound one that we want to keep

			//				i--;
			//				while (i >= 0)
			//				{
			//					if (foundItemArray[i] == 1)
			//					{
			//						break;
			//					}
			//					i--;
			//				}
			//				i++;

			//				int k = i;
			//				for ( ; j < usedItemsCount; j++, k++)
			//				{
			//					initialArray[k] = initialArray[j];
			//				}
			//			}

			//			usedItemsCount--;
			//		}
			//	}
			//}
		}

		private void RemoveIfNotMarked(in T item)
		{
			// calling this function assumes we are hashing
			int hash = item == null ? 0 : comparer.GetHashCode(item);
			int hashIndex = (hash & HighBitNotSet) % indexArray.Length;

			int index = indexArray[hashIndex];

			if (index > 0) // 0 means item does not yet exist in the HashSet
			{
				// item with same hashIndex already exists, so need to look in the chained list for an equal item (using Equals)

				int priorIndex = 0;
				int lastNonBlankIndex = firstBlankAtEndIndex - 1;

				int nextIndex;

				while (true)
				{
					ref TNode tNodeRef = ref nodeArray[index]; //??? if ref is faster, then use it in other places also

					bool isMarked = (tNodeRef.nextIndex & MarkNextIndexBitMask) != 0;
					nextIndex = tNodeRef.nextIndex & MarkNextIndexBitMaskInverted;

					// check if hash codes are equal before calling Equals (which may take longer) items that are Equals must have the same hash code
					#if Exclude_Comparing_Cached_Hash
					if (!isMarked && comparer.Equals(tNodeRef.item, item))
					#else
					if (!isMarked && tNodeRef.hashOrNextIndexForBlanks == hash && comparer.Equals(tNodeRef.item, item))
					#endif
					{
						// remove this item

						usedItemsCount--;

						// first try to set it to blank by adding it to the blank at end group
						if (index == lastNonBlankIndex)
						{
							lastNonBlankIndex--;
							if (nextBlankIndex == firstBlankAtEndIndex)
							{
								nextBlankIndex--;
							}
							firstBlankAtEndIndex--;
						}
						else
						{
							// add to the blank chain

							tNodeRef.nextIndex = 0;

							tNodeRef.hashOrNextIndexForBlanks = nextBlankIndex;
							nextBlankIndex = index;
						}

						if (priorIndex == 0)
						{
							if (nextIndex == index)
							{
								indexArray[hashIndex] = 0;
							}
							else
							{
								indexArray[hashIndex] = nextIndex;
							}
						}
						else
						{
							if ((nodeArray[priorIndex].nextIndex & MarkNextIndexBitMask) != 0)
							{
								// this nexIndex is marked, so it needs to stay marked
								nodeArray[priorIndex].nextIndex = nextIndex | MarkNextIndexBitMask;
							}
							else
							{
								nodeArray[priorIndex].nextIndex = nextIndex;
							}
						}

						// keep priorIndex the same because we removed the node in the chain, so the priorIndex is still the same value
					}
					else
					{
						priorIndex = index; // node was not removed from the chain, so the priorIndex now points to the node that was not removed
					}

					if (nextIndex == index)
					{
						return; // not found
					}
					index = nextIndex;
				}
			}
		}

		public int RemoveWhere(Predicate<T> match)
		{
			if (match == null)
			{
				throw new ArgumentNullException(nameof(match), "Value cannot be null.");
			}

			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			int removeCount = 0;

			if (isHashing)
			{
				// must traverse all of the chains instead of just looping through the nodeArray because going through the chains is the only way to set
				// nodes within a chain to blank and still be able to remove the blnak node from the chain

				int index;
				int nextIndex;
				int priorIndex;
				int lastNonBlankIndex = firstBlankAtEndIndex - 1;
				for (int i = 0; i < indexArray.Length; i++)
				{
					priorIndex = 0; // 0 means use indexArray
					index = indexArray[i];
					if (index != 0)
					{
						while (true)
						{
							ref TNode tNodeRef = ref nodeArray[index];
							nextIndex = tNodeRef.nextIndex;
							if (match.Invoke(tNodeRef.item))
							{
								//  set this node to blank
								removeCount++;

								usedItemsCount--;

								// first try to set it to blank by adding it to the blank at end group
								if (index == lastNonBlankIndex)
								{
									lastNonBlankIndex--;
									if (nextBlankIndex == firstBlankAtEndIndex)
									{
										nextBlankIndex--;
									}
									firstBlankAtEndIndex--;
								}
								else
								{
									// add to the blank group

									tNodeRef.nextIndex = 0;

									tNodeRef.hashOrNextIndexForBlanks = nextBlankIndex;
									nextBlankIndex = index;
								}

								if (priorIndex == 0)
								{
									if (nextIndex == index)
									{
										indexArray[i] = 0;
									}
									else
									{
										indexArray[i] = nextIndex;
									}
								}
								else
								{
									nodeArray[priorIndex].nextIndex = nextIndex;
								}

								// keep priorIndex the same because we removed the node in the chain, so the priorIndex is still the same value
							}
							else
							{
								priorIndex = index; // node was not removed from the chain, so the priorIndex now points to the node that was not removed
							}

							if (nextIndex == index)
							{
								break;
							}
							index = nextIndex;
						}
					}
				}
			}
			else
			{
				int i;
				for (i = usedItemsCount - 1; i >= 0 ; i--)
				{
					if (match.Invoke(initialArray[i]))
					{
						removeCount++;

						if (i < usedItemsCount - 1)
						{
							int j = i + 1;
							int k = i;
							for ( ; j < usedItemsCount; j++, k++)
							{
								initialArray[k] = initialArray[j];
							}
						}

						usedItemsCount--;
					}
				}
			}

			return removeCount;
		}

		private class FastHashSetEqualityComparer : IEqualityComparer<FastHashSet<T>>
		{
			public bool Equals(FastHashSet<T> x, FastHashSet<T> y)
			{
				if (x == null && y == null)
				{
					return true;
				}

				if (x != null)
				{
					return x.SetEquals(y);
				}
				else
				{
					return false;
				}
			}

			public int GetHashCode(FastHashSet<T> set)
			{
				if (set == null)
				{
					// oddly the documentation for the IEqualityComparer.GetHashCode function says it will throw an ArgumentNullException if the param is null
					return 0; // 0 seems to be what .NET framework uses when passing in null, so return the same thing to be consistent
				}
				else
				{
					unchecked
					{
						int hashCode = 0;
						if (set.isHashing)
						{
							int pastNodeIndex = set.nodeArray.Length;
							if (set.firstBlankAtEndIndex < pastNodeIndex)
							{
								pastNodeIndex = set.firstBlankAtEndIndex;
							}

							for (int i = 1; i < pastNodeIndex; i++)
							{
								if (set.nodeArray[i].nextIndex != 0) // nextIndex == 0 indicates a blank/available node
								{
									// maybe do ^= instead of add? - will this produce the same thing regardless of order? - if ^= maybe we don't need unchecked
									// sum up the individual item hash codes - this way it won't matter what order the items are in, the same resulting hash code will be produced
									hashCode += set.nodeArray[i].hashOrNextIndexForBlanks;
								}
							}
						}
						else
						{
							for (int i = 0; i < set.usedItemsCount; i++)
							{
								// sum up the individual item hash codes - this way it won't matter what order the items are in, the same resulting hash code will be produced
								hashCode += set.initialArray[i].GetHashCode();
							}
						}
						return hashCode;
					}
				}
			}
		}

		public static IEqualityComparer<FastHashSet<T>> CreateSetComparer()
		{
			return new FastHashSetEqualityComparer();
		}

		//??? if disposed then yield break?
		public IEnumerator<T> GetEnumerator()
		{
			return new FastHashSetEnumerator<T>(this);
			//if (isHashing)
			//{
			//	currentNodeIdx = 1;
				
			//	// it's easiest to just loop through the node array and skip any nodes with nextIndex = 0
			//	// rather than looping through the indexArray and following the nextIndex to the end of each bucket

			//	while (currentNodeIdx < firstBlankAtEndIndex)
			//	{
			//		if (nodeArray[currentNodeIdx].nextIndex != 0)
			//		{
			//			yield return nodeArray[currentNodeIdx].item;
			//		}

			//		currentNodeIdx++;
			//	}
			//}
			//else
			//{
			//	currentNodeIdx = 0; // the initialArray doesn't really have nodes, but it's still just an index into the array

			//	while (currentNodeIdx < usedItemsCount)
			//	{
			//		yield return initialArray[currentNodeIdx];

			//		currentNodeIdx++;
			//	}
			//}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			//return GetEnumerator();
			return new FastHashSetEnumerator<T>(this);
		}

		private class FastHashSetEnumerator<T2> : IEnumerator<T2>
		{
			private FastHashSet<T2> set;
			private int currentIndex = -1;

			#if !Exclude_Check_For_Is_Disposed_In_Enumerator
			private bool isDisposed;
			#endif

			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			private int incrementForEverySetModification;
			#endif

			public FastHashSetEnumerator(FastHashSet<T2> set)
			{
				this.set = set;
				if (set.isHashing)
				{
					currentIndex = 0; // 0 is the index before the first possible node (0 is the blank node)
				}
				else
				{
					currentIndex = -1;
				}

				#if !Exclude_Check_For_Set_Modifications_In_Enumerator
				incrementForEverySetModification = set.incrementForEverySetModification;
				#endif
				//currentItem = default(T);
			}

			public bool MoveNext()
			{
				#if !Exclude_Check_For_Is_Disposed_In_Enumerator
				if (isDisposed)
				{
					// the only reason we do something when Disposed is called and return false after it is called is to be compatable with HashSet
					// I'm not sure this level of compatability is necessary and maybe we should remove this because it is unnecessary and makes things slightly slower
					//???
					return false;
				}
				#endif

				#if !Exclude_Check_For_Set_Modifications_In_Enumerator
				if (incrementForEverySetModification != set.incrementForEverySetModification)
				{
					throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
				}
				#endif

				if (set.isHashing)
				{
					// it's easiest to just loop through the node array and skip any nodes with nextIndex = 0
					// rather than looping through the indexArray and following the nextIndex to the end of each bucket

					while (true)
					{
						currentIndex++;
						if (currentIndex < set.firstBlankAtEndIndex)
						{
							if (set.nodeArray[currentIndex].nextIndex != 0)
							{
								return true;
							}
						}
						else
						{
							currentIndex = set.firstBlankAtEndIndex;
							return false;
						}
					}
				}
				else
				{
					currentIndex++;
					if (currentIndex < set.usedItemsCount)
					{
						return true;
					}
					else
					{
						currentIndex--;
						return false;
					}
				}
			}

			public void Reset()
			{
				#if !Exclude_Check_For_Set_Modifications_In_Enumerator
				if (incrementForEverySetModification != set.incrementForEverySetModification)
				{
					throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
				}
				#endif

				if (set.isHashing)
				{
					currentIndex = 0; // 0 is the index before the first possible node (0 is the blank node)
				}
				else
				{
					currentIndex = -1;
				}
			}

			void IDisposable.Dispose()
			{
				#if !Exclude_Check_For_Is_Disposed_In_Enumerator
				isDisposed = true;
				#endif
			}

			public T2 Current
			{
				get
				{
					//??? why doesn't Current check if the set was modified and throw an exception - there might not even be a current anymore if something was removed?
					// maybe because they thought throwing an exception for a Property isn't good?
					if (set.isHashing)
					{
						// it's easiest to just loop through the node array and skip any nodes with nextIndex = 0
						// rather than looping through the indexArray and following the nextIndex to the end of each bucket

						if (currentIndex > 0 && currentIndex < set.firstBlankAtEndIndex)
						{
							return set.nodeArray[currentIndex].item;
						}
					}
					else
					{
						if (currentIndex >= 0 && currentIndex < set.usedItemsCount)
						{
							return set.initialArray[currentIndex];
						}
					}
					return default;
				}
			}

			public ref T2 CurrentRef
			{
				get
				{
					if (set.isHashing)
					{
						// it's easiest to just loop through the node array and skip any nodes with nextIndex = 0
						// rather than looping through the indexArray and following the nextIndex to the end of each bucket

						if (currentIndex > 0 && currentIndex < set.firstBlankAtEndIndex)
						{
							return ref set.nodeArray[currentIndex].item;
						}
						else
						{
							// we can just return a ref to the 0 node's item instead of throwing an exception? - this should have a default item value
							return ref set.nodeArray[0].item;
						}
					}
					else
					{
						if (currentIndex >= 0 && currentIndex < set.usedItemsCount)
						{
							return ref set.initialArray[currentIndex];
						}
						else
						{
							// we can just return a ref to the 0 node's item instead of throwing an exception?
							return ref set.initialArray[0];
						}
					}
				}
			}

			public bool IsCurrentValid
			{
				get
				{
					//??? why doesn't Current check if the set was modified and throw an exception - there might not even be a current anymore if something was removed?
					if (set.isHashing)
					{
						// it's easiest to just loop through the node array and skip any nodes with nextIndex = 0
						// rather than looping through the indexArray and following the nextIndex to the end of each bucket

						if (currentIndex > 0 && currentIndex < set.firstBlankAtEndIndex)
						{
							return true;
						}
					}
					else
					{
						if (currentIndex >= 0 && currentIndex < set.usedItemsCount)
						{
							return true;
						}
					}
					return false;
				}
			}

			object IEnumerator.Current
			{
				get { return Current; }
			}
		}
		#if false
        // Implement GetEnumerator to return IEnumerator<T> to enable 
        // foreach iteration of our list. Note that in C# 2.0  
        // you are not required to implement Current and MoveNext. 
        // The compiler will create a class that implements IEnumerator<T>. 
        public IEnumerator<T> GetEnumerator() 
        { 
            Node current = head; 
 
            while (current != null) 
            { 
                yield return current.Data; 
                current = current.Next; 
            } 
        } 
 
        // We must implement this method because  
        // IEnumerable<T> inherits IEnumerable 
        IEnumerator IEnumerable.GetEnumerator() 
        { 
            return GetEnumerator(); 
        }
		#endif
	}

	public class a<T> : IEnumerable<T>
	{
		public IEnumerator<T> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}
	}

}

#endif