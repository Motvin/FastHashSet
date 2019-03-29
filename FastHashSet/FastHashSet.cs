//#define Exclude_Check_For_Set_Modifications_In_Enumerator
//#define Exclude_Check_For_Is_Disposed_In_Enumerator
//#define Exclude_Lazy_Initialization_Of_Internal_Arrays
//#define Exclude_No_Hash_Array_Implementation
#define Cache_Optimize_Resize

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics;

//??? Add XML comments so that summary, etc. shows in intellisense just like it does for HashSet<T>
namespace FastHashSet
{
	public partial class FastHashSet<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ISet<T> //, ISerializable, IDeserializationCallback - ??? implement this within #define
    {
		// this is the size of the non-hash array used for small
		private const int InitialArraySize = 8;

		// this is the # of initial nodes for the buckets array after going into hashing after using the noHashArray
		// this is 16 + 1 for the first node (node at index 0) which doesn't get used because 0 is the NullIndex
		private const int InitialBucketsArraySize = 17;

		// this indicates end of chain if the nextIndex of a node has this value and also indicates no chain if a buckets array element has this value
		private const int NullIndex = 0;

		// if a node's nextIndex = this value, then it is a blank node - this isn't a valid nextIndex when unmarked and also when marked (because we don't allow int.MaxValue items)
		private const int BlankNextIndexIndicator = int.MaxValue;

		// use this instead of the negate negative logic when getting hashindex - this saves an if (hashindex < 0) which can be the source of bad branch prediction
		private const int HighBitNotSet = unchecked((int)0b0111_1111_1111_1111_1111_1111_1111_1111);

		// The Mark... constants below are for marking, unmarking, and checking if an item is marked.
		// This is usefull for some set operations.

		// doing an | (bitwise or) with this and the nextIndex marks the node, setting the bit back will give the original nextIndex value
		private const int MarkNextIndexBitMask = unchecked((int)0b1000_0000_0000_0000_0000_0000_0000_0000);
		
		// doing an & (bitwise and) with this and the nextIndex sets it back to the original value (unmarks it)
		private const int MarkNextIndexBitMaskInverted = ~MarkNextIndexBitMask;

		// FastHashSet doesn't allow using an item/node index as high as int.MaxValue.
		// There are 2 reasons for this: The first is that int.MaxValue is used as a special indicator
		private const int LargestPrimeLessThanMaxInt = 2147483629;

		// what if someone wants less than double the # of items allocated next time??? - need to get a prime between 2 numbers in this list? - just let them specify the number, which maybe should be a prime

		// these are primes above the .75 loadfactor of the power of 2 except from 30,000 through 80,000, where we conserve space to help with cache space
		private static readonly int[] bucketsSizeArray = { 11, 23, 47, 89, 173, 347, 691, 1367, 2741, 5471, 10_937, 19_841/*16_411/*21_851*/, 40_241/*32_771/*43_711*/, 84_463/*65_537/*87_383*/, /*131_101*/174_767,
			/*262_147*/349_529, 699_053, 1_398_107, 2_796_221, 5_592_407, 11_184_829, 22_369_661, 44_739_259, 89_478_503, 17_8956_983, 35_7913_951, 715_827_947, 143_1655_777, LargestPrimeLessThanMaxInt};
			
		// the buckets array can be pre-allocated to a large size, but it's not good to use that entire size for hashing because of cache locality
		// instead do at most 3 size steps (for 3 levels of cache) before using its actual allocated size

		// when an initial capacity is selected in the constructor or later, allocate the required space for the buckets array, but only use a subset of this space until the load factor is met
		// limit the # of used elements to optimize for cpu caches
		private static readonly int[] bucketsSizeArrayForCacheOptimization = { 3_371, 62_851, 701_819 };

		private const double LoadFactorConst = .75;

		private int currentIndexIntoBucketsSizeArray;

		private int bucketsModSize;

		#if !Exclude_Check_For_Set_Modifications_In_Enumerator
		private int incrementForEverySetModification;
		#endif

		// resize the buckets array when the count reaches this value
		private int resizeBucketsCountThreshold;

		private int count;

		private int nextBlankIndex;

		// this is needed because if items are removed, they get added into the blank list starting at nextBlankIndex, but we may want to TrimExcess capacity, so this is a quick way to see what the ExcessCapacity is
		private int firstBlankAtEndIndex;

		private IEqualityComparer<T> comparer;

		// make the buckets size a primary number to make the mod function less predictable
		private int[] buckets;

		private TNode[] slots;

		#if !Exclude_No_Hash_Array_Implementation
		// used for small sets - when the count of items is small, it is usually faster to just use an array of the items and not do hashing at all (this can also use slightly less memory)
		// There may be some cases where the sets can be very small, but there can be very many of these sets.  This can be good for these cases.
		private T[] noHashArray;
		private bool isHashing; // start out just using the itemsTable as an array without hashing??? do we need this bool - could just check if noHashArray is not null? do this as a property
		#endif

		internal enum FoundType
		{
			FoundFirstTime,
			FoundNotFirstTime,
			NotFound
		}

		internal struct TNode
		{
			// the cached hash code of the item - this is so we don't have to call GetHashCode multiple times, also doubles as a nextIndex for blanks, since blank nodes don't need a hash code
			public int hashOrNextIndexForBlanks;

			public int nextIndex;

			public T item;

			public TNode(T elem, int nextIndex, int hash)
			{
				this.item = elem;

				this.nextIndex = nextIndex;

				this.hashOrNextIndexForBlanks = hash;
			}
		}

		//???
		// Note: instead of blindly allocating double the # of items each time
		// have a way to specify the # of items that should be allocated next time
		// this is useful if the code knows something, ex. If adding dates from 1950 to 2019 in order we know if we are in 2018 that there can't be many more dates
		// so allocating 2x the amount would be a waste, the program could (at every year of processing, set the allocation to 366 * years remaining to be processed or something like that) -
		// this would be better than 2x if there were alot of dates (or dates and something else being the value of each item)
		// I think there is a property called NextSizeIncrease or something, but not sure if it's used or not

		// 1 - same constructor params as HashSet
		public FastHashSet()
		{
			comparer = EqualityComparer<T>.Default;
			SetInitialCapacity(InitialArraySize);
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
			SetInitialCapacity(InitialArraySize);
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


		public FastHashSet(IEnumerable<T> collection, bool areAllCollectionItemsDefinitelyUnique, int initialCapacity, IEqualityComparer<T> comparer = null)
		{
			//??? what about an initialCapacity = 0 means go straight into hashing

			this.comparer = comparer;
			SetInitialCapacity(initialCapacity);
		}

		//??? this could be public if useful somehow
		// maybe add a param to override the initial capacity???
		private void AddInitialUniqueValuesEnumerable(IEnumerable<T> collection, int countOfItemsInCollection)
		{
			#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
			#endif
				nextBlankIndex = 1;
				foreach (T item in collection)
				{
					//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
					int hash = (comparer.GetHashCode(item) & HighBitNotSet);
					int hashIndex = hash % bucketsModSize;

					int index = buckets[hashIndex];
					buckets[hashIndex] = nextBlankIndex;

					ref TNode t = ref slots[nextBlankIndex];

					t.hashOrNextIndexForBlanks = hash;
					t.nextIndex = index;
					t.item = item;

					nextBlankIndex++;
				}
			#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				int i = 0;
				foreach (T item in collection)
				{
					noHashArray[i++] = item;
				}
			}
			#endif
			count = countOfItemsInCollection;
			firstBlankAtEndIndex = nextBlankIndex;
		}

		private void AddInitialEnumerableWithEnoughCapacity(IEnumerable<T> collection)
		{
			// this assumes we are hashing
			foreach (T item in collection)
			{
				//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
				int hash = (comparer.GetHashCode(item) & HighBitNotSet);
				int hashIndex = hash % bucketsModSize;

				for (int index = buckets[hashIndex]; index != NullIndex; )
				{
					ref TNode t = ref slots[index];

					if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
					{
						goto Found; // item was found
					}

					index = t.nextIndex;
				}

				ref TNode tBlank = ref slots[nextBlankIndex];

				tBlank.hashOrNextIndexForBlanks = hash;
				tBlank.nextIndex = buckets[hashIndex];
				tBlank.item = item;

				buckets[hashIndex] = nextBlankIndex;

				nextBlankIndex++;

#if Cache_Optimize_Resize
				count++;

				if (count >= resizeBucketsCountThreshold)
				{
					ResizeBucketsArrayForward(GetNewBucketsArraySize());
				}
#endif
			Found:;
			}
			firstBlankAtEndIndex = nextBlankIndex;
#if !Cache_Optimize_Resize
			usedItemsCount = nextBlankIndex - 1;
#endif
		}

		private void AddInitialEnumerable(IEnumerable<T> collection)
		{
			FastHashSet<T> fhset = collection as FastHashSet<T>;
			if (fhset != null && Equals(fhset.Comparer, Comparer))
			{
				// a set with the same item comparer must have all items unique
				// so Count will be the exact Count of the items added
				// also don't have to check for equals of items
				// and a FastHashSet has the additional advantage of not having to call GetHashCode() if it is hashing
				// and it has access to the internal slots array so we don't have to use the foreach/enumerator

				int count = fhset.Count;
				SetInitialCapacity(count);

#if !Exclude_No_Hash_Array_Implementation
				if (isHashing)
				{
					if (fhset.isHashing)
					{
#endif
						// this FastHashSet is hashing and collection is a FastHashSet (with equal comparer) and it is also hashing

						nextBlankIndex = 1;
						int maxNodeIndex = fhset.slots.Length - 1;
						if (fhset.firstBlankAtEndIndex <= maxNodeIndex)
						{
							maxNodeIndex = fhset.firstBlankAtEndIndex - 1;
						}

						for (int i = 1; i <= maxNodeIndex; i++)
						{
							ref TNode t2 = ref fhset.slots[i];
							if (t2.nextIndex != BlankNextIndexIndicator)
							{
								int hash = t2.hashOrNextIndexForBlanks;
								int hashIndex = hash % bucketsModSize;

								ref TNode t = ref slots[nextBlankIndex];

								t.hashOrNextIndexForBlanks = hash;
								t.nextIndex = buckets[hashIndex];;
								t.item = t2.item;

								buckets[hashIndex] = nextBlankIndex;

								nextBlankIndex++;
							}
						}
						this.count = count;
						firstBlankAtEndIndex = nextBlankIndex;
#if !Exclude_No_Hash_Array_Implementation
					}
					else
					{
						// this FastHashSet is hashing and collection is a FastHashSet (with equal comparer) and it is NOT hashing

						nextBlankIndex = 1;
						for (int i = 0; i < fhset.count; i++)
						{
							ref T item = ref noHashArray[i];

							//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
							int hash = (comparer.GetHashCode(item) & HighBitNotSet);
							int hashIndex = hash % bucketsModSize;

							ref TNode t = ref slots[nextBlankIndex];

							t.hashOrNextIndexForBlanks = hash;
							t.nextIndex = buckets[hashIndex];
							t.item = item;

							buckets[hashIndex] = nextBlankIndex;

							nextBlankIndex++;
						}
					}
				}
				else
				{
					// this FastHashSet is not hashing

					AddInitialUniqueValuesEnumerable(collection, count);
				}
#endif
			}
			else
			{
				// collection is not a FastHashSet with equal comparer

				HashSet<T> hset = collection as HashSet<T>;
				if (hset != null && Equals(hset.Comparer, Comparer))
				{
					// a set with the same item comparer must have all items unique
					// so Count will be the exact Count of the items added
					// also don't have to check for equals of items

					int usedCount = hset.Count;
					SetInitialCapacity(usedCount);

					AddInitialUniqueValuesEnumerable(collection, usedCount);
				}
				else
				{
					ICollection<T> coll = collection as ICollection<T>;
					if (coll != null)
					{
						SetInitialCapacity(coll.Count);
#if !Exclude_No_Hash_Array_Implementation
						if (isHashing)
						{
#endif
							// call SetInitialCapacity and then set the capacity back to get rid of the excess?

							AddInitialEnumerableWithEnoughCapacity(collection);

							TrimExcess();
#if !Exclude_No_Hash_Array_Implementation
						}
						else
						{
							foreach (T item in collection)
							{
								Add(item);
							}
						}
#endif
					}
					else
					{
						SetInitialCapacity(InitialArraySize);

						foreach (T item in collection)
						{
							Add(in item);
						}
					}
				}
			}
		}

		private void SetInitialCapacity(int capacity)
		{
#if !Exclude_No_Hash_Array_Implementation
			if (capacity > InitialArraySize)
			{
#endif
				// skip using the array and go right into hashing
				InitHashing(capacity);
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				CreateNoHashArray(); // don't set the capacity/size of the noHashArray
			}
#endif
		}

#if !Exclude_No_Hash_Array_Implementation
		// this function can be called to switch from using the noHashArray and start using the hashing arrays (slots and buckets)
		// this function can also be called before noHashArray is even allocated in order to skip using the array and go right into hashing
		private void SwitchToHashing(int capacityIncrease = -1)
		{
			InitHashing(capacityIncrease);

			if (noHashArray != null)
			{
				// i is the index into noHashArray
				for (int i = 0; i < count; i++)
				{
					ref T item = ref noHashArray[i];

					int hash = (comparer.GetHashCode(item) & HighBitNotSet);
					int hashIndex = hash % bucketsModSize;

					ref TNode t = ref slots[nextBlankIndex];

					t.hashOrNextIndexForBlanks = hash;
					t.nextIndex = buckets[hashIndex];
					t.item = item;

					buckets[hashIndex] = nextBlankIndex;

					nextBlankIndex++;
				}
				noHashArray = null; // this array can now be garbage collected because it is no longer referenced
			}

			firstBlankAtEndIndex = nextBlankIndex;
		}
#endif

		private void InitHashing(int capacity = -1)
		{
			int newSlotsArraySize;
			int newBucketsArraySize;
			int newBucketsArrayModSize;

			bool setThresh = false;
			if (capacity == -1)
			{
				newSlotsArraySize = InitialBucketsArraySize;

				newBucketsArraySize = bucketsSizeArray[0];
				if (newBucketsArraySize < newSlotsArraySize)
				{
					for (currentIndexIntoBucketsSizeArray = 1; currentIndexIntoBucketsSizeArray < bucketsSizeArray.Length; currentIndexIntoBucketsSizeArray++)
					{
						newBucketsArraySize = bucketsSizeArray[currentIndexIntoBucketsSizeArray];
						if (newBucketsArraySize >= newSlotsArraySize)
						{
							break;
						}
					}
				}
				newBucketsArrayModSize = newBucketsArraySize;
			}
			else
			{
				newSlotsArraySize = capacity + 1; // add 1 to accomodate blank first node (node at 0 index)

				newBucketsArraySize = GetEqualOrClosestHigherPrime((int)(newSlotsArraySize / LoadFactorConst));

				#if Cache_Optimize_Resize
				if (newBucketsArraySize > bucketsSizeArrayForCacheOptimization[0])
				{
					newBucketsArrayModSize = bucketsSizeArrayForCacheOptimization[0];
					setThresh = true;
				}
				else
				#endif
				{
					newBucketsArrayModSize = newBucketsArraySize;
				}
			}
			
			if (newSlotsArraySize == 0)
			{
				// this is an error, the int.MaxValue has been used for capacity and we require more - throw an Exception for this
				//??? it's not really out of memory, if running 64 bit and you have alot of virtual memory you could possibly get here and still have memory - try this with HashSet<uint> and see what it error it gives
				//throw new OutOfMemoryException();
			}

			slots = new TNode[newSlotsArraySize]; // the slots array has an extra item as it's first item (0 index) that is for available items - the memory is wasted, but it simplifies things
			buckets = new int[newBucketsArraySize]; // these will be initially set to 0, so make 0 the blank(available) value and reduce all indices by one to get to the actual index into the slots array
			bucketsModSize = newBucketsArrayModSize;

			if (setThresh)
			{
				resizeBucketsCountThreshold = (int)(newBucketsArrayModSize * LoadFactorConst);
			}
			else
			{
				CalcUsedItemsLoadFactorThreshold();
			}

			nextBlankIndex = 1; // start at 1 because 0 is the blank item

			firstBlankAtEndIndex = nextBlankIndex;

#if !Exclude_No_Hash_Array_Implementation
			isHashing = true;
#endif
		}
		
#if !Exclude_No_Hash_Array_Implementation
		private void CreateNoHashArray()
		{
			noHashArray = new T[InitialArraySize];
		}
#endif

		private void CalcUsedItemsLoadFactorThreshold()
		{
			if (buckets != null)
			{
				if (buckets.Length == bucketsModSize)
				{
					resizeBucketsCountThreshold = slots.Length; // with this value, the buckets array should always resize after the slots array (in the same public function call)
				}
				else
				{
					// when buckets.Length > bucketsModSize, this means we want to more slowly increase the bucketsModSize to keep things in the L1-3 caches
					resizeBucketsCountThreshold = (int)(bucketsModSize * LoadFactorConst);
				}
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
			CopyTo(array, arrayIndex, count);
		}

		public void CopyTo(T[] array)
		{
			CopyTo(array, 0, count);
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

#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				int pastNodeIndex = slots.Length;
				if (firstBlankAtEndIndex < pastNodeIndex)
				{
					pastNodeIndex = firstBlankAtEndIndex;
				}

				int cnt = 0;
				for (int i = 1; i < pastNodeIndex; i++)
				{
					if (slots[i].nextIndex != BlankNextIndexIndicator)
					{
						array[arrayIndex++] = slots[i].item;
						if (++cnt == count)
						{
							break;
						}
					}
				}
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				int cnt = this.count;
				if (cnt > count)
				{
					cnt = count;
				}

				// for small arrays, I think the for loop below will actually be faster than Array.Copy because of the overhead of that function - could test this???
				//Array.Copy(noHashArray, 0, array, arrayIndex, cnt);

				for (int i = 0; i < cnt; i++)
				{
					array[arrayIndex++] = noHashArray[i];
				}
			}
#endif
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
				return count;
			}
		}

		// this is the percent of used items to all items (used + blank/available)
		// at which point any additional added items will
		// first resize the buckets array to the next prime to avoid too many collisions and chains becoming too large
		public double LoadFactor
		{
			get
			{
				return LoadFactorConst;
			}
		}

		// this is the capacity that can be trimmed with TrimExcessCapacity
		// items that were removed from the hash arrays can't be trimmed by calling TrimExcessCapacity, only the blank items at the end
		// items that were removed from the noHashArray can be trimmed by calling TrimExcessCapacity because the items after are moved to fill the blank space
		public int ExcessCapacity
		{
			get
			{
				int excessCapacity;
#if !Exclude_No_Hash_Array_Implementation
				if (isHashing)
				{
#endif
					excessCapacity = slots.Length - firstBlankAtEndIndex;
#if !Exclude_No_Hash_Array_Implementation
				}
				else
				{
					excessCapacity = noHashArray.Length - count;
				}
#endif
				return excessCapacity;
			}
		}

		public int Capacity
		{
			get
			{
#if !Exclude_No_Hash_Array_Implementation
				if (isHashing)
				{
#endif
					return slots.Length - 1; // subtract 1 for blank node at 0 index
#if !Exclude_No_Hash_Array_Implementation
				}
				else
				{
					return noHashArray.Length;
				}
#endif
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
				return GetNewSlotsArraySizeIncrease(out int oldSlotsArraySize, true);
			}
		}

		public int NextCapacityIncrease
		{
			get
			{
				return GetNewSlotsArraySizeIncrease(out int oldSlotsArraySize);
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

#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				currentCapacity = slots.Length - count;
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				currentCapacity = noHashArray.Length - count;
			}
#endif

			if (currentCapacity < capacity)
			{
				IncreaseCapacity(capacity - currentCapacity);
			}

			//??? is this correct - this should be the number where the next lowest number would force a resize of buckets array with the current loadfactor and the entire slots array is full
			int calcedNewBucketsArraySize = (int)(slots.Length / LoadFactorConst) + 1;

			if (calcedNewBucketsArraySize < 0 && calcedNewBucketsArraySize > LargestPrimeLessThanMaxInt)
			{
				calcedNewBucketsArraySize = LargestPrimeLessThanMaxInt;
			}
			else
			{
				calcedNewBucketsArraySize = GetEqualOrClosestHigherPrime(calcedNewBucketsArraySize);
			}

			if (buckets.Length < calcedNewBucketsArraySize)
			{
				// -1 means stop trying to increase the size based on the array of primes
				// instead calc based on 2 * existing length and then get the next higher prime
				currentIndexIntoBucketsSizeArray = -1;

				ResizeBucketsArrayForward(calcedNewBucketsArraySize);
			}

			return slots.Length - count;
		}

		// return true if bucketsModSize was set, false otherwise
		private bool CheckForModSizeIncrease()
		{
			if (bucketsModSize < buckets.Length)
			{
				// instead of array, just have 3 constants
				int partLength = (int)(buckets.Length * .75);

				int size0 = bucketsSizeArrayForCacheOptimization[0];
				int size1 = bucketsSizeArrayForCacheOptimization[1];
				if (bucketsModSize == size0)
				{
					if (size1 <= partLength)
					{
						bucketsModSize = size1;
						return true;
					}
					else
					{
						bucketsModSize = buckets.Length;
						return true;
					}
				}
				else
				{
					int size2 = bucketsSizeArrayForCacheOptimization[2];
					if (bucketsModSize == size1)
					{
						if (size2 <= partLength)
						{
							bucketsModSize = size2;
							return true;
						}
						else
						{
							bucketsModSize = buckets.Length;
							return true;
						}
					}
					else if (bucketsModSize == size2)
					{
						bucketsModSize = buckets.Length;
						return true;
					}
				}
			}
			return false;
		}

		// make this private???
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

		private int GetNewSlotsArraySizeIncrease(out int oldArraySize, bool getOnlyDefaultSize = false)
		{
			if (slots != null)
			{
				oldArraySize = slots.Length;
			}
			else
			{
				//??? should probably start at 8 if NOT doing the initial non-hashing array - we start at 32 if doing the initial non-hashing array
				oldArraySize = 17; // this isn't the old node array or the noHashArray, but it is the initial size we should start at - this will create a new node array of Length = 33
			}
			//else if (noHashArray != null)
			//{
			//	oldArraySize = noHashArray.Length; // this isn't the old node array, but it is the old # of items that could be stored without resizing
			//}
			//else
			//{
			//	oldArraySize = InitialItemsTableSize; // this isn't the old node array or the noHashArray, but it is the initial size we should start at
			//}
				
			int increaseInSize;
			if (getOnlyDefaultSize || NextCapacityIncreaseOverride < 0)
			{
				if (oldArraySize == 1)
				{
					increaseInSize = InitialBucketsArraySize - 1;
				}
				else
				{
					increaseInSize = oldArraySize - 1;
				}
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

		// if the value returned gets used and that value is different than the current buckets.Length, then the calling code should increment currentIndexIntoSizeArray because this would now be the current
		private int GetNewBucketsArraySize()
		{
			//??? to avoid to many allocations of this array, setting the initial capacity in the constructor or MaxCapacity or NextCapacityIncreaseOverride should determine
			// where the currentIndexIntoBucketsSizeArray is pointing to and also the capacity of this buckets array (which should be a prime)		public int MaxCapacity { get; set; } = -1;

			int newArraySize;

			if (currentIndexIntoBucketsSizeArray >= 0)
			{
				if (currentIndexIntoBucketsSizeArray + 1 < bucketsSizeArray.Length)
				{
					newArraySize = bucketsSizeArray[currentIndexIntoBucketsSizeArray + 1];
				}
				else
				{
					newArraySize = buckets.Length;
				}
			}
			else
			{
				// -1 means stop trying to increase the size based on the array of primes
				// instead calc based on 2 * existing length and then get the next higher prime
				newArraySize = buckets.Length;
				if (newArraySize < int.MaxValue / 2)
				{
					newArraySize = GetEqualOrClosestHigherPrime(newArraySize + newArraySize);
				}
				else
				{
					newArraySize = LargestPrimeLessThanMaxInt;
				}
			}

			return newArraySize;
		}

		// if hashing, increase the size of the slots array
		// if not yet hashing, switch to hashing
		private void IncreaseCapacity(int capacityIncrease = -1)
		{
			//??? this function might be a fair bit over overhead for resizing at small sizes (like 33 and 65)
			//- could try to reduce the overhead - there could just be a nextSlotsArraySize (don't need increase?), or nextSlotsArraySizeIncrease?
			// then we don't have to call GetNewSlotsArraySizeIncrease at all?
			// could test the overhead by just replacing all of the code with 
#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				int newSlotsArraySizeIncrease;
				int oldSlotsArraySize;

				if (capacityIncrease == -1)
				{
					newSlotsArraySizeIncrease = GetNewSlotsArraySizeIncrease(out oldSlotsArraySize);
				}
				else
				{
					newSlotsArraySizeIncrease = capacityIncrease;
					oldSlotsArraySize = slots.Length;
				}

				if (newSlotsArraySizeIncrease <= 0)
				{
					//??? throw an error
				}

				int newSlotsArraySize = oldSlotsArraySize + newSlotsArraySizeIncrease;

				//#if false
				TNode[] newSlotsArray = new TNode[newSlotsArraySize];
				Array.Copy(slots, 0, newSlotsArray, 0, slots.Length); // check the IL, I think Array.Resize and Array.Copy without the start param calls this, so avoid the overhead by calling directly
				slots = newSlotsArray;
				//#endif
				//Array.Resize(ref slots, newSlotsArraySize);
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				SwitchToHashing(capacityIncrease);
			}
#endif
		}

		private TNode[] IncreaseCapacityNoCopy(int capacityIncrease = -1)
		{
#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				int newSlotsrraySizeIncrease;
				int oldSlotsArraySize;

				if (capacityIncrease == -1)
				{
					newSlotsrraySizeIncrease = GetNewSlotsArraySizeIncrease(out oldSlotsArraySize);
				}
				else
				{
					newSlotsrraySizeIncrease = capacityIncrease;
					oldSlotsArraySize = slots.Length;
				}

				if (newSlotsrraySizeIncrease <= 0)
				{
					//??? throw an error
				}

				int newSlotsArraySize = oldSlotsArraySize + newSlotsrraySizeIncrease;

				TNode[] newSlotsArray = new TNode[newSlotsArraySize];
				return newSlotsArray;
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				SwitchToHashing(capacityIncrease);
				return null;
			}
#endif
		}

		private void ResizeBucketsArrayForward(int newBucketsArraySize)
		{
			if (newBucketsArraySize == buckets.Length)
			{
				// this will still work if no increase in size - it just might be slower than if you could increase the buckets array size
			}
			else
			{
				//??? what if there is a high percent of blank/unused items in the slots array before the firstBlankAtEndIndex (mabye because of lots of removes)?
				// It would probably be faster to loop through the buckets array and then do chaining to find the used nodes - one problem with this is that you would have to find blank nodes - but they would be chained
				// this probably isn't a very likely scenario

				if (!CheckForModSizeIncrease()) //??? clean this up, it isn't really good to do it this way - no need to call GetNewBucketsArraySize before calling this function
				{
					buckets = new int[newBucketsArraySize];
					bucketsModSize = newBucketsArraySize;

					if (currentIndexIntoBucketsSizeArray >= 0)
					{
						currentIndexIntoBucketsSizeArray++; // when the newBucketsArraySize gets used in the above code, point to the next avaialble size - ??? not sure this is the best place to increment this
					}
				}
				else
				{
					Array.Clear(buckets, 0, bucketsModSize);
				}

				CalcUsedItemsLoadFactorThreshold();

				int bucketsArrayLength = buckets.Length;

				int pastNodeIndex = slots.Length;
				if (firstBlankAtEndIndex < pastNodeIndex)
				{
					pastNodeIndex = firstBlankAtEndIndex;
				}

				//??? for a loop where the end is array.Length, the compiler can skip any array bounds checking - can it do it for this code - it should be able to because pastIndex is no more than buckets.Length
				if (firstBlankAtEndIndex == count + 1)
				{
					// this means there aren't any blank nodes
					for (int i = 1; i < pastNodeIndex; i++)
					{
						ref TNode t = ref slots[i];

						int hashIndex = t.hashOrNextIndexForBlanks % bucketsArrayLength;
						t.nextIndex = buckets[hashIndex];

						buckets[hashIndex] = i;
					}
				}
				else
				{
					// this means there are some blank nodes
					for (int i = 1; i < pastNodeIndex; i++)
					{
						ref TNode t = ref slots[i];
						if (t.nextIndex != BlankNextIndexIndicator) // skip blank nodes
						{
							int hashIndex = t.hashOrNextIndexForBlanks % bucketsArrayLength;
							t.nextIndex = buckets[hashIndex];

							buckets[hashIndex] = i;
						}
					}
				}
			}
		}

		private void ResizeBucketsArrayForwardKeepMarks(int newBucketsArraySize)
		{
			if (newBucketsArraySize == buckets.Length)
			{
				// this will still work if no increase in size - it just might be slower than if you could increase the buckets array size
			}
			else
			{
				//??? what if there is a high percent of blank/unused items in the slots array before the firstBlankAtEndIndex (mabye because of lots of removes)?
				// It would probably be faster to loop through the buckets array and then do chaining to find the used nodes - one problem with this is that you would have to find blank nodes - but they would be chained
				// this probably isn't a very likely scenario

				if (!CheckForModSizeIncrease()) //??? clean this up, it isn't really good to do it this way - no need to call GetNewBucketsArraySize before calling this function
				{
					buckets = new int[newBucketsArraySize];
					bucketsModSize = newBucketsArraySize;

					if (currentIndexIntoBucketsSizeArray >= 0)
					{
						currentIndexIntoBucketsSizeArray++; // when the newBucketsArraySize gets used in the above code, point to the next avaialble size - ??? not sure this is the best place to increment this
					}
				}

				CalcUsedItemsLoadFactorThreshold();

				int bucketsArrayLength = buckets.Length;

				int pastNodeIndex = slots.Length;
				if (firstBlankAtEndIndex < pastNodeIndex)
				{
					pastNodeIndex = firstBlankAtEndIndex;
				}

				//??? for a loop where the end is array.Length, the compiler can skip any array bounds checking - can it do it for this code - it should be able to because pastIndex is no more than buckets.Length
				if (firstBlankAtEndIndex == count + 1)
				{
					// this means there aren't any blank nodes
					for (int i = 1; i < pastNodeIndex; i++)
					{
						ref TNode t = ref slots[i];

						int hashIndex = t.hashOrNextIndexForBlanks % bucketsArrayLength;
						t.nextIndex = buckets[hashIndex] | (t.nextIndex & MarkNextIndexBitMask);

						buckets[hashIndex] = i;
					}
				}
				else
				{
					// this means there are some blank nodes
					for (int i = 1; i < pastNodeIndex; i++)
					{
						ref TNode t = ref slots[i];
						if (t.nextIndex != BlankNextIndexIndicator) // skip blank nodes
						{
							int hashIndex = t.hashOrNextIndexForBlanks % bucketsArrayLength;
							t.nextIndex = buckets[hashIndex] | (t.nextIndex & MarkNextIndexBitMask);

							buckets[hashIndex] = i;
						}
					}
				}
			}
		}

		//??? remove if not used
		private void ResizeBucketsArrayReverse(int newBucketsArraySize)
		{
			if (newBucketsArraySize == buckets.Length)
			{
				// this will still work if no increase in size - it just might be slower than if you could increase the buckets array size
			}
			else
			{
				//??? what if there is a high percent of blank/unused items in the slots array before the firstBlankAtEndIndex (mabye because of lots of removes)?
				// It would probably be faster to loop through the buckets array and then do chaining to find the used nodes - one problem with this is that you would have to find blank nodes - but they would be chained
				// this probably isn't a very likely scenario
				buckets = new int[newBucketsArraySize];
				bucketsModSize = newBucketsArraySize;

				if (currentIndexIntoBucketsSizeArray >= 0)
				{
					currentIndexIntoBucketsSizeArray++; // when the newBucketsArraySize gets used in the above code, point to the next avaialble size - ??? not sure this is the best place to increment this
				}

				CalcUsedItemsLoadFactorThreshold();

				int bucketsArrayLength = buckets.Length;

				int lastNodeIndex = slots.Length - 1;
				if (firstBlankAtEndIndex < lastNodeIndex)
				{
					lastNodeIndex = firstBlankAtEndIndex - 1;
				}

				if (nextBlankIndex >= firstBlankAtEndIndex) // we know there aren't any blanks within the nodes, so skip the if (<not blank node>) check
				{
					for (int i = lastNodeIndex; i >= 1 ; i--)
					{
						ref TNode t = ref slots[i];

						int hashIndex = t.hashOrNextIndexForBlanks % bucketsArrayLength;
						t.nextIndex = buckets[hashIndex];

						buckets[hashIndex] = i;
					}				
				}
				else
				{
					for (int i = lastNodeIndex; i >= 1 ; i--)
					{
						ref TNode t = ref slots[i];
						if (t.nextIndex != BlankNextIndexIndicator)
						{
							int hashIndex = t.hashOrNextIndexForBlanks % bucketsArrayLength;
							t.nextIndex = buckets[hashIndex];

							buckets[hashIndex] = i;
						}
					}
				}
			}
		}

		//??? rewrite this to be correct if used
		private void ResizeBucketsArrayForwardAndCopy(int newBucketsArraySize, TNode[] oldSlotsArray)
		{
			if (newBucketsArraySize == buckets.Length)
			{
				// this will still work if no increase in size - it just might be slower than if you could increase the buckets array size
			}
			else
			{
				//??? what if there is a high percent of blank/unused items in the slots array before the firstBlankAtEndIndex (mabye because of lots of removes)?
				// It would probably be faster to loop through the buckets array and then do chaining to find the used nodes - one problem with this is that you would have to find blank nodes - but they would be chained
				// this probably isn't a very likely scenario
				buckets = new int[newBucketsArraySize];
				bucketsModSize = newBucketsArraySize;

				if (currentIndexIntoBucketsSizeArray >= 0)
				{
					currentIndexIntoBucketsSizeArray++; // when the newBucketsArraySize gets used in the above code, point to the next avaialble size - ??? not sure this is the best place to increment this
				}

				CalcUsedItemsLoadFactorThreshold();

				int bucketsArrayLength = buckets.Length;

				int lastNodeIndex = slots.Length - 1;
				if (firstBlankAtEndIndex < lastNodeIndex)
				{
					lastNodeIndex = firstBlankAtEndIndex - 1;
				}

				if (nextBlankIndex >= firstBlankAtEndIndex) // we know there aren't any blanks within the nodes, so skip the if (<not blank node>) check
				{
					for (int i = 1; i <= lastNodeIndex; i++)
					{
						ref TNode t = ref slots[i];
						t = oldSlotsArray[i]; // copy node from old to new

						int hashIndex = t.hashOrNextIndexForBlanks % bucketsModSize;

						buckets[hashIndex] = i;
						t.nextIndex = buckets[hashIndex];
					}				
				}
				else
				{
					for (int i = 1; i <= lastNodeIndex; i++)
					{
						ref TNode t = ref slots[i];
						t = oldSlotsArray[i]; // copy node from old to new
						if (t.nextIndex != BlankNextIndexIndicator)
						{
							int hashIndex = t.hashOrNextIndexForBlanks % bucketsModSize;

							buckets[hashIndex] = i;
							t.nextIndex = buckets[hashIndex];
						}
					}
				}
			}
		}

		//??? why is this not used
		private void ResizeBucketsArrayWithMarks(int newBucketsArraySize)
		{
			if (newBucketsArraySize == buckets.Length)
			{
				// this will still work if no increase in size - it just might be slower than if you could increase the buckets array size
			}
			else
			{
				//??? what if there is a high percent of blank/unused items in the slots array before the firstBlankAtEndIndex (mabye because of lots of removes)?
				// It would probably be faster to loop through the buckets array and then do chaining to find the used nodes - one problem with this is that you would have to find blank nodes - but they would be chained
				// this probably isn't a very likely scenario
				buckets = new int[newBucketsArraySize];
				bucketsModSize = newBucketsArraySize;

				if (currentIndexIntoBucketsSizeArray >= 0)
				{
					currentIndexIntoBucketsSizeArray++; // when the newBucketsArraySize gets used in the above code, point to the next avaialble size - ??? not sure this is the best place to increment this
				}

				CalcUsedItemsLoadFactorThreshold();

				int bucketsArrayLength = buckets.Length; //??? look at the IL code to see if it helps to put this often used property into a local variable or not

				int pastNodeIndex = slots.Length;
				if (firstBlankAtEndIndex < pastNodeIndex)
				{
					pastNodeIndex = firstBlankAtEndIndex;
				}

				//??? for a loop where the end is array.Length, the compiler can skip any array bounds checking - can it do it for this code - it should be able to because pastIndex is no more than buckets.Length
				if (firstBlankAtEndIndex == count + 1)
				{
					// this means there aren't any blank nodes
					for (int i = 1; i < pastNodeIndex; i++)
					{
						ref TNode t = ref slots[i];

						int hashIndex = t.hashOrNextIndexForBlanks % bucketsArrayLength;
						t.nextIndex = buckets[hashIndex] | (t.nextIndex & MarkNextIndexBitMask); // if marked, keep the mark

						buckets[hashIndex] = i;
					}
				}
				else
				{
					// this means there are some blank nodes
					for (int i = 1; i < pastNodeIndex; i++)
					{
						ref TNode t = ref slots[i];
						if (t.nextIndex != BlankNextIndexIndicator) // skip blank nodes
						{
							int hashIndex = t.hashOrNextIndexForBlanks % bucketsArrayLength;
							t.nextIndex = buckets[hashIndex] | (t.nextIndex & MarkNextIndexBitMask); // if marked, keep the mark

							buckets[hashIndex] = i;
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

#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
#endif
			{
				firstBlankAtEndIndex = 1;
				nextBlankIndex = 1;
				Array.Clear(buckets, 0, buckets.Length);
			}

			count = 0;
		}

		//
		public void ClearAndTrimAll()
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			// this would deallocate the arrays - would need to lazy allocate the arrays if allowing this
			//??? I don't think the time to always check for the arrays would make this worth it (but HashSet does this - with branch prediction being correct most of the time I think it wouldn't add that much time) - could just set the BasicHashSet variable to null in this case and reset it with constructor when needed?
			// maybe could just check for the noHashArray this way, because it would set isHashing to false?
		}

		//??? what about a function the cleans up blank internal nodes by rechaining used nodes to fill up the blank nodes
		// and then the TrimeExcess can do a better job of trimming excess - add a function to do that?  call it Compact...
		// there is a perfect structure where the first non-blank buckets array element points to index 1 in the slots array and anything that follows does so in slots array index 2, 3, etc.
		// this way you have locality of reference when doing lookups and you also remove all internal blank nodes
		// it would be easy to create this structure when doing a resize of the slots array, so maybe doing an Array.Resize isn't the best for the slots array, although you are usually only doing this when you have no more blank nodes, so that part of the advantage is not valid for this scenario

		//??? I wonder how TrimExcess works for HashSet?

		// documentation states:
		// You can use the TrimExcess method to minimize a HashSet<T> object's memory overhead once it is known that no new elements will be added
		// To completely clear a HashSet<T> object and release all memory referenced by it, call this method after calling the Clear method.
		public void TrimExcess()
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				// do we have to check for slots != null??? - is this possible?
				if (slots != null && slots.Length > firstBlankAtEndIndex && firstBlankAtEndIndex > 0)
				{
					Array.Resize(ref slots, firstBlankAtEndIndex);
					// when firstBlankAtEndIndex == slots.Length, that means there are no blank at end items
				}
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				if (noHashArray != null && noHashArray.Length > count && count > 0)
				{
					Array.Resize(ref noHashArray, count);
				}
			}
#endif
		}

		// this is only present to implement ICollection<T> - it has no real value otherwise because the Add method with bool return value already does this
		void ICollection<T>.Add(T item)
		{
			Add(in item);
		}

		//??? do we need 2 versions, one with in and one without - if we do then change this one to match the regular Add, which has been tested
		public bool Add(in T item)
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				//bool increasedCapacity = false;

				//??? consider doing the  &  & HighBitNotSet on the comparer.GetHashCode(item); - this could mess with comparing equals (maybe -2,000,000,000 would compare equals to 1...) by just their hash codes, but this should be rare
				int hash = (comparer.GetHashCode(item) & HighBitNotSet);
				int hashIndex = hash % bucketsModSize;

				for (int index = buckets[hashIndex]; index != NullIndex; )
				{
					ref TNode t = ref slots[index];

					if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
					{
						return false; // item was found, so return false to indicate it was not added
					}

					index = t.nextIndex;
				}

				if (nextBlankIndex >= slots.Length)
				{
					// there aren't any more blank nodes to add items, so we need to increase capacity
					IncreaseCapacity();
				}

				int firstIndex = buckets[hashIndex];
				buckets[hashIndex] = nextBlankIndex;

				ref TNode tBlank = ref slots[nextBlankIndex];
				if (nextBlankIndex >= firstBlankAtEndIndex)
				{
					// the blank nodes starting at firstBlankAtEndIndex aren't chained
					nextBlankIndex = ++firstBlankAtEndIndex;
				}
				else
				{
					// the blank nodes before firstBlankAtEndIndex are chained (the hashOrNextIndexForBlanks points to the next blank node)
					nextBlankIndex = tBlank.hashOrNextIndexForBlanks;
				}

				tBlank.hashOrNextIndexForBlanks = hash;
				tBlank.nextIndex = firstIndex;
				tBlank.item = item;

				count++;

				if (count >= resizeBucketsCountThreshold)
				{
					ResizeBucketsArrayForward(GetNewBucketsArraySize());
				}

				return true;
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				int i;
				for (i = 0; i < count; i++)
				{
					if (comparer.Equals(item, noHashArray[i]))
					{
						return false;
					}
				}

				if (i == noHashArray.Length)
				{
					SwitchToHashing();

					int hash = (comparer.GetHashCode(item) & HighBitNotSet);
					int hashIndex = hash % bucketsModSize;

					ref TNode tBlank = ref slots[nextBlankIndex];

					tBlank.hashOrNextIndexForBlanks = hash;
					tBlank.nextIndex = buckets[hashIndex];
					tBlank.item = item;

					buckets[hashIndex] = nextBlankIndex;

					nextBlankIndex = ++firstBlankAtEndIndex;

					count++;

					return true;
				}
				else
				{
					// add to noHashArray
					noHashArray[i] = item;
					count++;
					return true;
				}
			}
#endif
		}

		public bool Add(T item)
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif

				//??? consider doing the  &  & HighBitNotSet on the comparer.GetHashCode(item); - this could mess with comparing equals (maybe -2,000,000,000 would compare equals to 1...) by just their hash codes, but this should be rare
				int hash = (comparer.GetHashCode(item) & HighBitNotSet);
				int hashIndex = hash % bucketsModSize;

				for (int index = buckets[hashIndex]; index != NullIndex; )
				{
					ref TNode t = ref slots[index];

					if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
					{
						return false; // item was found, so return false to indicate it was not added
					}

					index = t.nextIndex;
				}

				if (nextBlankIndex >= slots.Length)
				{
					// there aren't any more blank nodes to add items, so we need to increase capacity
					IncreaseCapacity();
				}

				int firstIndex = buckets[hashIndex];
				buckets[hashIndex] = nextBlankIndex;

				ref TNode tBlank = ref slots[nextBlankIndex];
				if (nextBlankIndex >= firstBlankAtEndIndex)
				{
					// the blank nodes starting at firstBlankAtEndIndex aren't chained
					nextBlankIndex = ++firstBlankAtEndIndex;
				}
				else
				{
					// the blank nodes before firstBlankAtEndIndex are chained (the hashOrNextIndexForBlanks points to the next blank node)
					nextBlankIndex = tBlank.hashOrNextIndexForBlanks;
				}

				tBlank.hashOrNextIndexForBlanks = hash;
				tBlank.nextIndex = firstIndex;
				tBlank.item = item;

				count++;

				if (count >= resizeBucketsCountThreshold)
				{
					ResizeBucketsArrayForward(GetNewBucketsArraySize());
				}

				return true;
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				int i;
				for (i = 0; i < count; i++)
				{
					if (comparer.Equals(item, noHashArray[i]))
					{
						return false;
					}
				}

				if (i == noHashArray.Length)
				{
					SwitchToHashing();

					//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
					int hash = (comparer.GetHashCode(item) & HighBitNotSet);
					int hashIndex = hash % bucketsModSize;

					ref TNode tBlank = ref slots[nextBlankIndex];

					tBlank.hashOrNextIndexForBlanks = hash;
					tBlank.nextIndex = buckets[hashIndex];
					tBlank.item = item;

					buckets[hashIndex] = nextBlankIndex;

					nextBlankIndex = ++firstBlankAtEndIndex;

					count++;

					return true;
				}
				else
				{
					// add to noHashArray
					noHashArray[i] = item;
					count++;
					return true;
				}
			}
#endif
		}

		//??? remove if not used - this was inlined in both Add methods
		private void AddToHashSet(in T item)
		{
			// this assumes we are hashing and there is enough capacity and the item is already not found and there are no chained blanks

			//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
			int hash = (comparer.GetHashCode(item) & HighBitNotSet);
			int hashIndex = hash % bucketsModSize;

			ref TNode tBlank = ref slots[nextBlankIndex];

			tBlank.hashOrNextIndexForBlanks = hash;
			tBlank.nextIndex = buckets[hashIndex];
			tBlank.item = item;

			buckets[hashIndex] = nextBlankIndex;

			nextBlankIndex = ++firstBlankAtEndIndex;

			count++;
		}

		// return the node index that was added, or NullIndex if item was found
		private int AddToHashSetIfNotFound(in T item, int hash)
		{
			// this assmes we are hashing

			int hashIndex = hash % bucketsModSize;

			for (int index = buckets[hashIndex]; index != NullIndex; )
			{
				ref TNode t = ref slots[index];

				if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
				{
					return NullIndex; // item was found, so return NullIndex to indicate it was not added
				}

				index = t.nextIndex;
			}

			if (nextBlankIndex >= slots.Length)
			{
				// there aren't any more blank nodes to add items, so we need to increase capacity
				IncreaseCapacity();
				ResizeBucketsArrayForward(GetNewBucketsArraySize());

				// fix things messed up by buckets array resize
				hashIndex = hash % bucketsModSize;
			}

			int firstIndex = buckets[hashIndex];
			buckets[hashIndex] = nextBlankIndex;

			int addedNodeIndex = nextBlankIndex;
			ref TNode tBlank = ref slots[nextBlankIndex];
			if (nextBlankIndex >= firstBlankAtEndIndex)
			{
				// the blank nodes starting at firstBlankAtEndIndex aren't chained
				nextBlankIndex = ++firstBlankAtEndIndex;
			}
			else
			{
				// the blank nodes before firstBlankAtEndIndex are chained (the hashOrNextIndexForBlanks points to the next blank node)
				nextBlankIndex = tBlank.hashOrNextIndexForBlanks;
			}

			tBlank.hashOrNextIndexForBlanks = hash;
			tBlank.nextIndex = firstIndex;
			tBlank.item = item;

			count++;

			return addedNodeIndex; // item was not found, so return the index of the added item
		}

		// return the node index that was added, or NullIndex if item was found
		private int AddToHashSetIfNotFoundAndMark(in T item, int hash)
		{
			// this assumes we are hashing

			int hashIndex = hash % bucketsModSize;

			for (int index = buckets[hashIndex]; index != NullIndex; )
			{
				ref TNode t = ref slots[index];

				if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
				{
					return NullIndex; // item was found, so return NullIndex to indicate it was not added
				}

				index = t.nextIndex & MarkNextIndexBitMaskInverted;;
			}

			if (nextBlankIndex >= slots.Length)
			{
				// there aren't any more blank nodes to add items, so we need to increase capacity
				IncreaseCapacity();
				ResizeBucketsArrayForwardKeepMarks(GetNewBucketsArraySize());

				// fix things messed up by buckets array resize
				hashIndex = hash % bucketsModSize;
			}

			int firstIndex = buckets[hashIndex];
			buckets[hashIndex] = nextBlankIndex;

			int addedNodeIndex = nextBlankIndex;
			ref TNode tBlank = ref slots[nextBlankIndex];
			if (nextBlankIndex >= firstBlankAtEndIndex)
			{
				// the blank nodes starting at firstBlankAtEndIndex aren't chained
				nextBlankIndex = ++firstBlankAtEndIndex;
			}
			else
			{
				// the blank nodes before firstBlankAtEndIndex are chained (the hashOrNextIndexForBlanks points to the next blank node)
				nextBlankIndex = tBlank.hashOrNextIndexForBlanks;
			}

			tBlank.hashOrNextIndexForBlanks = hash;
			tBlank.nextIndex = firstIndex | MarkNextIndexBitMask;
			tBlank.item = item;

			count++;

			return addedNodeIndex; // item was not found, so return the index of the added item
		}

		// this implements Contains for ICollection<T>
		public bool Contains(T item)
		{
#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
				int hash = (comparer.GetHashCode(item) & HighBitNotSet);
				int hashIndex = hash % bucketsModSize;

				for (int index = buckets[hashIndex]; index != NullIndex; )
				{
					ref TNode t = ref slots[index];

					if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
					{
						return true; // item was found, so return true
					}

					index = t.nextIndex;
				}
				return false;
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				for (int i = 0; i < count; i++)
				{
					if (comparer.Equals(item, noHashArray[i]))
					{
						return true; // item was found, so return true
					}
				}
				return false;
			}
#endif
		}

		// does in make a difference??? - test this with larger structs and compare against the non-in version
		public bool Contains(in T item)
		{
#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif

				//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
				int hash = (comparer.GetHashCode(item) & HighBitNotSet);
				int hashIndex = hash % bucketsModSize;

				for (int index = buckets[hashIndex]; index != NullIndex; )
				{
					ref TNode t = ref slots[index];

					if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
					{
						return true; // item was found, so return true
					}

					index = t.nextIndex;
				}
				return false;
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				for (int i = 0; i < count; i++)
				{
					if (comparer.Equals(item, noHashArray[i]))
					{
						return true; // item was found, so return true
					}
				}
				return false;
			}
#endif
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

#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				int hash = (comparer.GetHashCode(item) & HighBitNotSet);
				int hashIndex = hash % bucketsModSize;

				int priorIndex = NullIndex;

				for (int index = buckets[hashIndex]; index != NullIndex; )
				{
					ref TNode t = ref slots[index];

					if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
					{
						// item was found, so remove it

						if (priorIndex == NullIndex)
						{
							buckets[hashIndex] = t.nextIndex;
						}
						else
						{
							slots[priorIndex].nextIndex = t.nextIndex;
						}

						// add node to blank chain or to the blanks at the end (if possible)
						if (index == firstBlankAtEndIndex - 1)
						{
							if (nextBlankIndex == firstBlankAtEndIndex)
							{
								nextBlankIndex--;
							}
							firstBlankAtEndIndex--;
						}
						else
						{
							t.hashOrNextIndexForBlanks = nextBlankIndex;
							nextBlankIndex = index;
						}

						t.nextIndex = BlankNextIndexIndicator;

						count--;

						return true;
					}

					priorIndex = index;

					index = t.nextIndex;
				}
				return false; // item not found
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				for (int i = 0; i < count; i++)
				{
					if (comparer.Equals(item, noHashArray[i]))
					{
						// remove the item by moving all remaining items to fill over this one - this is probably faster than array.copyto???
						for (int j = i + 1; j < count; j++, i++)
						{
							noHashArray[i] = noHashArray[j];
						}
						count--;
						return true;
					}
				}
				return false;
			}
#endif
		}

		//??? do we need the same FindOrAdd for a reference type? or does this function take care of that?
		//??? don't do isAdded, do isFound because of Find below
		public ref T FindOrAdd(in T item, out bool isFound)
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			isFound = false;
#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				int addedNodeIndex = AddToHashSetIfNotFound(in item, (comparer.GetHashCode(item) & HighBitNotSet));
				isFound = (addedNodeIndex != NullIndex);
				return ref slots[addedNodeIndex].item;
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				int i;
				for (i = 0; i < count; i++)
				{
					if (comparer.Equals(item, noHashArray[i]))
					{
						isFound = true;
						return ref noHashArray[i];
					}
				}

				if (i == noHashArray.Length)
				{
					SwitchToHashing();
					return ref FindOrAdd(in item, out isFound);
				}
				else
				{
					// add to noHashArray and keep isAdded true
					noHashArray[i] = item;
					count++;
					return ref noHashArray[i];
				}
			}
#endif
		}

		// return index into slots array or 0 if not found

		//??? to make things faster, could have a FindInSlotsArray that just returns foundNodeIndex and another version called FindWithPriorInSlotsArray that has the 3 out params
		// first test to make sure this works as is
		private void FindInSlotsArray(in T item, out int foundNodeIndex, out int priorNodeIndex, out int bucketsIndex)
		{
			foundNodeIndex = NullIndex;
			priorNodeIndex = NullIndex;

			//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
			int hash = (comparer.GetHashCode(item) & HighBitNotSet);
			int hashIndex = hash % bucketsModSize;

			bucketsIndex = hashIndex;

			int priorIndex = NullIndex;

			for (int index = buckets[hashIndex]; index != NullIndex; )
			{
				ref TNode t = ref slots[index];

				if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
				{
					foundNodeIndex = index;
					priorNodeIndex = priorIndex;
					return; // item was found
				}

				priorIndex = index;

				index = t.nextIndex & MarkNextIndexBitMaskInverted; // is this function ever called when nextIndex values can be marked???
			}
			return; // item not found
		}

		// is there a way to mark this function so that it will be inlined???
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool FindInSlotsArray(in T item, int hash)
		{
			int hashIndex = hash % bucketsModSize;

			for (int index = buckets[hashIndex]; index != NullIndex; )
			{
				ref TNode t = ref slots[index];

				if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
				{
					return true; // item was found, so return true
				}

				index = t.nextIndex;;
			}
			return false;
		}

#if !Exclude_No_Hash_Array_Implementation
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool FindInNoHashArray(in T item)
		{
			for (int i = 0; i < count; i++)
			{
				if (comparer.Equals(item, noHashArray[i]))
				{
					return true; // item was found, so return true
				}
			}
			return false;
		}
#endif

		private void UnmarkAllNextIndexValues(int maxNodeIndex)
		{
			// must be hashing to be here
			for (int i = 1; i <= maxNodeIndex; i++)
			{
				slots[i].nextIndex &= MarkNextIndexBitMaskInverted;
			}
		}

		// removeMarked = true, means remove the marked items and keep the unmarked items
		// removeMarked = false, means remove the unmarked items and keep the marked items
		private void UnmarkAllNextIndexValuesAndRemoveAnyMarkedOrUnmarked(bool removeMarked)
		{
			// must be hashing to be here

			// must traverse all of the chains instead of just looping through the slots array because going through the chains is the only way to set
			// nodes within a chain to blank and still be able to remove the blank node from the chain

			int index;
			int nextIndex;
			int priorIndex;
			int lastNonBlankIndex = firstBlankAtEndIndex - 1;
			for (int i = 0; i < buckets.Length; i++)
			{
				priorIndex = NullIndex; // 0 means use buckets array
				index = buckets[i];

				while (index != NullIndex)
				{
					ref TNode t = ref slots[index];
					nextIndex = t.nextIndex;
					bool isMarked = (nextIndex & MarkNextIndexBitMask) != 0;
					if (isMarked)
					{
						// this node is marked, so unmark it
						nextIndex &= MarkNextIndexBitMaskInverted;
						t.nextIndex = nextIndex;
					}

					if (removeMarked == isMarked)
					{
						// set this node to blank

						count--;

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

							t.nextIndex = BlankNextIndexIndicator;

							t.hashOrNextIndexForBlanks = nextBlankIndex;
							nextBlankIndex = index;
						}

						if (priorIndex == NullIndex)
						{
							buckets[i] = nextIndex;
						}
						else
						{
							slots[priorIndex].nextIndex = nextIndex;
						}

						// keep priorIndex the same because we removed the node in the chain, so the priorIndex is still the same value
					}
					else
					{
						priorIndex = index; // node was not removed from the chain, so the priorIndex now points to the node that was not removed
					}

					index = nextIndex;
				}
			}
		}

		private FoundType FindInSlotsArrayAndMark(in T item, out int foundNodeIndex)
		{
			int hash = (comparer.GetHashCode(item) & HighBitNotSet);;
			int hashIndex = hash % bucketsModSize;

			int index = buckets[hashIndex];

			if (index == NullIndex)
			{
				foundNodeIndex = NullIndex;
				return FoundType.NotFound;
			}
			else
			{
				// item with same hashIndex already exists, so need to look in the chained list for an equal item (using Equals)

				int nextIndex;
				while (true)
				{
					ref TNode t = ref slots[index];
					nextIndex = t.nextIndex;

					// check if hash codes are equal before calling Equals (which may take longer) items that are Equals must have the same hash code
					if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
					{
						foundNodeIndex = index;
						if ((nextIndex & MarkNextIndexBitMask) == 0)
						{
							// not marked, so mark it
							t.nextIndex |= MarkNextIndexBitMask;

							return FoundType.FoundFirstTime;
						}
						return FoundType.FoundNotFirstTime;
					}

					nextIndex &= MarkNextIndexBitMaskInverted;
					if (nextIndex == NullIndex)
					{
						foundNodeIndex = NullIndex;
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

			return ref slots[0].item;
		}

		// this is similar to HashSet<T>.TryGetValue, except it returns a ref to the value rather than a copy of the value found (TryGetValue uses an out parameter)
		// this way you can modify the actual value in the set if it is a value type (you can always modify the object if it is a reference type - except I think if it is a string)
		// also passing the item by in and the return by ref is faster for larger structs than passing by value
		public ref T Find(in T item, out bool isFound)
		{
			isFound = false;
#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				FindInSlotsArray(item, out int foundNodeIndex, out int priorNodeIndex, out int bucketsIndex);
				if (foundNodeIndex != NullIndex)
				{
					isFound = true;
				}

				return ref slots[foundNodeIndex].item; // if not found, then return a ref to the first node item, which isn't used for anything other than this
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				int i;
				for (i = 0; i < count; i++)
				{
					if (comparer.Equals(item, noHashArray[i]))
					{
						isFound = true;
						return ref noHashArray[i];
					}
				}

				// if item was not found, still need to return a ref to something, so return a ref to the first item in the array
				return ref noHashArray[0];
			}
#endif
		}

		public bool TryGetValue(T equalValue, out T actualValue)
		{
#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				FindInSlotsArray(equalValue, out int foundNodeIndex, out int priorNodeIndex, out int bucketsIndex);
				if (foundNodeIndex > 0)
				{
					actualValue = slots[foundNodeIndex].item;
					return true;
				}

				actualValue = default;
				return false;
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				int i;
				for (i = 0; i < count; i++)
				{
					if (comparer.Equals(equalValue, noHashArray[i]))
					{
						actualValue = noHashArray[i];
						return true;
					}
				}

				actualValue = default;
				return false;
			}
#endif
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

#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				foreach (T item in other)
				{
					//AddToHashSetIfNotFound(in item, item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet));
					AddToHashSetIfNotFound(in item, (comparer.GetHashCode(item) & HighBitNotSet));
				}
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				int i;

				foreach (T item in other)
				{
					//??? if it's easier for the jit compiler or il compiler to remove the array bounds checking then
					// have i < noHashArray.Length and do the check for usedItemsCount within the loop with a break statment
					for (i = 0; i < count; i++)
					{
						if (comparer.Equals(item, noHashArray[i]))
						{
							goto found; // break out of inner for loop
						}
					}

					// if here then item was not found
					if (i == noHashArray.Length)
					{
						SwitchToHashing();
						//AddToHashSetIfNotFound(in item, item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet));
						AddToHashSetIfNotFound(in item, (comparer.GetHashCode(item) & HighBitNotSet));
					}
					else
					{
						// add to noHashArray
						noHashArray[i] = item;
						count++;
					}

			found:;
				}
			}
#endif
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

			// if hashing, find each item in the slots array and mark anything found, but remove from being found again
			// after

			//??? implement the faster methof if other is HashSet or FastHashSet
			//FastHashSet<T> otherSet = other as FastHashSet<T>;
			//if (otherSet != null && Equals(comparer, otherSet.comparer)) //??? also make sure the comparers are equal - how, they are probably references? - maybe they override Equals?
			//{
			//	//???
			//}

#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				int foundItemCount = 0; // the count of found items in the hash - without double counting
				foreach (T item in other)
				{
					FoundType foundType = FindInSlotsArrayAndMark(in item, out int foundIndex);
					if (foundType == FoundType.FoundFirstTime)
					{
						foundItemCount++;

						if (foundItemCount == count)
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
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				// Note: we could actually do this faster by moving any found items to the front and keeping track of the found items
				// with a single int index
				// the problem with this method is it reorders items and even though that shouldn't matter in a set
				// it might cause issues with code that incorrectly assumes order stays the same for operations like this

				// possibly a faster implementation would be to use the method above, but keep track of original order with an int array of the size of usedItemsCount (ex. item at 0 was originally 5, and also item at 5 was originally 0)

				// set the corresponding bit in this int if an item was found
				// using a uint means the no hashing array cannot be more than 32 items
				uint foundItemBits = 0;

				int i;

				int foundItemCount = 0; // the count of found items in the hash - without double counting
				foreach (T item in other)
				{
					for (i = 0; i < count; i++)
					{
						if (comparer.Equals(item, noHashArray[i]))
						{
							uint mask = (1u << i);
							if ((foundItemBits & mask) == 0)
							{
								foundItemBits |= mask;
								foundItemCount++;
							}
							goto found; // break out of inner for loop
						}
					}

			found:
					if (foundItemCount == count)
					{
						// all items in the set were found, so there is nothing to remove - the set isn't changed
						return;
					}
				}

				if (foundItemCount == 0)
				{
					count = 0; // this is the equivalent of calling Clear
				}
				else
				{
					// remove any items that are unmarked (unfound)
					// go backwards because this can be faster
					for (i = count - 1; i >= 0 ; i--)
					{
						uint mask = (1u << i);
						if ((foundItemBits & mask) == 0)
						{
							if (i < count - 1)
							{
								// a faster method if there are multiple unfound items in a row is to find the first used item (make i go backwards until the item is used and then increment i by 1)
								// if there aren't multiple unused in a row, then this is a bit of a waste

								int j = i + 1; // j now points to the next item after the unfound one that we want to keep

								i--;
								while (i >= 0)
								{
									uint mask2 = (1u << i);
									if ((foundItemBits & mask2) != 0)
									{
										break;
									}
									i--;
								}
								i++;

								int k = i;
								for ( ; j < count; j++, k++)
								{
									noHashArray[k] = noHashArray[j];
								}
							}

							count--;
						}
					}
				}
			}
#endif
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
				if (count == 0 && collection.Count > 0)
				{
					return true; // by definition, an empty set is a proper subset of any non-empty collection
				}

				if (count >= collection.Count)
				{
					return false;
				}
			}
			else
			{
				if (count == 0)
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

#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				int foundItemCount = 0; // the count of found items in the hash - without double counting
				int maxFoundIndex = 0;
				bool notFoundAtLeastOne = false;
				foreach (T item in other)
				{
					FoundType foundType = FindInSlotsArrayAndMark(in item, out int foundIndex);
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

					if (notFoundAtLeastOne && foundItemCount == count)
					{
						// true means all of the items in the set were found in other and at least one item in other was not found in the set
						break; // will return true below after unmarking
					}
				}

				UnmarkAllNextIndexValues(maxFoundIndex);

				return notFoundAtLeastOne && foundItemCount == count; // true if all of the items in the set were found in other and at least one item in other was not found in the set
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				uint foundItemBits = 0;

				int foundItemCount = 0; // the count of found items in the hash - without double counting
				bool notFoundAtLeastOne = false;
				foreach (T item in other)
				{
					for (int i = 0; i < count; i++)
					{
						if (comparer.Equals(item, noHashArray[i]))
						{
							uint mask = (1u << i);
							if ((foundItemBits & mask) == 0)
							{
								foundItemBits |= mask;
								foundItemCount++;
							}
							goto found; // break out of inner for loop
						}
					}

					// if here then item was not found
					notFoundAtLeastOne = true;

			found:
					if (notFoundAtLeastOne && foundItemCount == count)
					{
						// true means all of the items in the set were found in other and at least one item in other was not found in the set
						return true;
					}
				}

				return false;
			}
#endif
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

			if (count == 0)
			{
				return true; // by definition, an empty set is a subset of any collection
			}

			ICollection<T> collection = other as ICollection<T>;
			if (collection != null)
			{
				if (count > collection.Count)
				{
					return false;
				}
			}

			//if (collection != null)
			//{

			//	//??? how can this be faster if other is a HashSet or FastHashSet with the same comparer
			//	// don't have to mark because we know all items are unique and with FastHashSet we can re-use the hash
			//}

#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				int foundItemCount = 0; // the count of found items in the hash - without double counting
				int maxFoundIndex = 0;
				foreach (T item in other)
				{
					FoundType foundType = FindInSlotsArrayAndMark(in item, out int foundIndex);
					if (foundType == FoundType.FoundFirstTime)
					{
						foundItemCount++;
						if (maxFoundIndex < foundIndex)
						{
							maxFoundIndex = foundIndex;
						}

						if (foundItemCount == count)
						{
							break;
						}
					}
				}

				UnmarkAllNextIndexValues(maxFoundIndex);

				return foundItemCount == count; // true if all of the items in the set were found in other
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				uint foundItemBits = 0;

				int foundItemCount = 0; // the count of found items in the hash - without double counting
				foreach (T item in other)
				{
					for (int i = 0; i < count; i++)
					{
						if (comparer.Equals(item, noHashArray[i]))
						{
							uint mask = (1u << i);
							if ((foundItemBits & mask) == 0)
							{
								foundItemBits |= mask;
								foundItemCount++;
							}
							goto found; // break out of inner for loop
						}
					}

			found:
					if (foundItemCount == count)
					{
						break;
					}
				}

				return foundItemCount == count; // true if all of the items in the set were found in other
			}
#endif
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

			if (count == 0)
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

#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				int foundItemCount = 0; // the count of found items in the hash - without double counting
				int maxFoundIndex = 0;
				foreach (T item in other)
				{
					FoundType foundType = FindInSlotsArrayAndMark(in item, out int foundIndex);
					if (foundType == FoundType.FoundFirstTime)
					{
						foundItemCount++;
						if (maxFoundIndex < foundIndex)
						{
							maxFoundIndex = foundIndex;
						}

						if (foundItemCount == count)
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

				return foundItemCount < count; // true if all of the items in other were found in set and at least one item in set was not found in other
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				uint foundItemBits = 0;

				int foundItemCount = 0; // the count of found items in the hash - without double counting
				foreach (T item in other)
				{
					for (int i = 0; i < count; i++)
					{
						if (comparer.Equals(item, noHashArray[i]))
						{
							uint mask = (1u << i);
							if ((foundItemBits & mask) == 0)
							{
								foundItemBits |= mask;
								foundItemCount++;
							}
							goto found; // break out of inner for loop
						}
					}

					// if here then item was not found
					return false;

			found:
					if (foundItemCount == count)
					{
						break;
					}
				}

				return foundItemCount < count; // true if all of the items in other were found in set and at least one item in set was not found in other
			}
#endif
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

			if (count == 0)
			{
				return false; // an empty set can never be a proper superset of anything (except an empty collection - but an empty collection returns true above)
			}

			//if (collection != null)
			//{

			//	//??? how can this be faster if other is a HashSet or FastHashSet with the same comparer
			//	// don't have to mark because we know all items are unique and with FastHashSet we can re-use the hash
			//}

#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				foreach (T item in other)
				{
					if (!FindInSlotsArray(in item, (comparer.GetHashCode(item) & HighBitNotSet)))
					{
						return false;
					}
				}

				return true; // true if all of the items in other were found in the set, false if at least one item in other was not found in the set
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				int i;

				foreach (T item in other)
				{
					for (i = 0; i < count; i++)
					{
						if (comparer.Equals(item, noHashArray[i]))
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
#endif
		}

		public bool Overlaps(IEnumerable<T> other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other), "Value cannot be null.");
			}

			if (other == this)
			{
				return count > 0; // return false if there are no items when both sets are the same, otherwise return true when both sets are the same
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
				if (c.Count < count)
				{
					return false;
				}

				HashSet<T> hset = other as HashSet<T>;
				if (hset != null && Equals(hset.Comparer, Comparer))
				{
					if (hset.Count != count)
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
					if (fhset.Count != count)
					{
						return false;
					}

#if !Exclude_No_Hash_Array_Implementation
					if (isHashing)
					{
#endif
						int pastNodeIndex = slots.Length;
						if (firstBlankAtEndIndex < pastNodeIndex)
						{
							pastNodeIndex = firstBlankAtEndIndex;
						}

#if !Exclude_No_Hash_Array_Implementation
						if (fhset.isHashing)
						{
#endif
							for (int i = 1; i < pastNodeIndex; i++)
							{
								//??? could not do the blank check if we know there aren't any blanks - below code and in the loop in the else
								if (slots[i].nextIndex != BlankNextIndexIndicator) // skip any blank nodes
								{
									if (!fhset.FindInSlotsArray(in slots[i].item, slots[i].hashOrNextIndexForBlanks))
									{
										return false;
									}
								}
							}
#if !Exclude_No_Hash_Array_Implementation
						}
						else
						{
							for (int i = 1; i < pastNodeIndex; i++)
							{
								if (slots[i].nextIndex != BlankNextIndexIndicator) // skip any blank nodes
								{
									if (!fhset.FindInNoHashArray(in slots[i].item))
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
							if (!FindInNoHashArray(in item))
							{
								return false;
							}
						}
					}
					return true;
#endif
				}

			}


#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				int foundItemCount = 0; // the count of found items in the hash - without double counting
				int maxFoundIndex = 0;
				foreach (T item in other)
				{
					FoundType foundType = FindInSlotsArrayAndMark(in item, out int foundIndex);
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

				return foundItemCount == count;
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				uint foundItemBits = 0;

				int foundItemCount = 0; // the count of found items in the hash - without double counting
				foreach (T item in other)
				{
					for (int i = 0; i < count; i++)
					{
						if (comparer.Equals(item, noHashArray[i]))
						{
							uint mask = (1u << i);
							if ((foundItemBits & mask) == 0)
							{
								foundItemBits |= mask;
								foundItemCount++;
							}
							goto found; // break out of inner for loop
						}
					}
					// if here then item was not found
					return false;
			found:;
				}

				return foundItemCount == count;
			}
#endif
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

#if !Exclude_No_Hash_Array_Implementation
			if (!isHashing)
			{
				// to make things easier for now, just switch to hashing if calling this function and deal with only one set of code
				SwitchToHashing();
			}
#endif

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
			int maxAddedNodeIndex = NullIndex;
			foreach (T item in other)
			{
				//addedNodeIndex = AddToHashSetIfNotFoundAndMark(in item, item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet));
				addedNodeIndex = AddToHashSetIfNotFoundAndMark(in item, (comparer.GetHashCode(item) & HighBitNotSet));
				if (addedNodeIndex > maxAddedNodeIndex)
				{
					maxAddedNodeIndex = addedNodeIndex;
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
			//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
			int hash = (comparer.GetHashCode(item) & HighBitNotSet);
			int hashIndex = hash % bucketsModSize;

			int priorIndex = NullIndex;

			for (int index = buckets[hashIndex]; index != NullIndex; )
			{
				ref TNode t = ref slots[index];

				if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
				{
					// item was found, so remove it if not marked
					if ((t.nextIndex & MarkNextIndexBitMask) == 0)
					{
						if (priorIndex == NullIndex)
						{
							buckets[hashIndex] = t.nextIndex;
						}
						else
						{
							// if slots[priorIndex].nextIndex was marked, then keep it marked
							// already know that t.nextIndex is not marked
							slots[priorIndex].nextIndex = t.nextIndex | (slots[priorIndex].nextIndex & MarkNextIndexBitMask);
						}

						// add node to blank chain or to the blanks at the end (if possible)
						if (index == firstBlankAtEndIndex - 1)
						{
							if (nextBlankIndex == firstBlankAtEndIndex)
							{
								nextBlankIndex--;
							}
							firstBlankAtEndIndex--;
						}
						else
						{
							t.hashOrNextIndexForBlanks = nextBlankIndex;
							nextBlankIndex = index;
						}

						t.nextIndex = BlankNextIndexIndicator;

						count--;

						return;
					}
				}

				priorIndex = index;

				index = t.nextIndex & MarkNextIndexBitMaskInverted;
			}
			return; // item not found
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

#if !Exclude_No_Hash_Array_Implementation
			if (isHashing)
			{
#endif
				// must traverse all of the chains instead of just looping through the slots array because going through the chains is the only way to set
				// nodes within a chain to blank and still be able to remove the blnak node from the chain

				int priorIndex;
				int nextIndex;
				for (int i = 0; i < buckets.Length; i++)
				{
					priorIndex = NullIndex; // 0 means use buckets array

					for (int index = buckets[i]; index != NullIndex; )
					{
						ref TNode t = ref slots[index];

						nextIndex = t.nextIndex;
						if (match.Invoke(t.item))
						{
							// item was matched, so remove it

							if (priorIndex == NullIndex)
							{
								buckets[i] = nextIndex;
							}
							else
							{
								slots[priorIndex].nextIndex = nextIndex;
							}

							// add node to blank chain or to the blanks at the end (if possible)
							if (index == firstBlankAtEndIndex - 1)
							{
								if (nextBlankIndex == firstBlankAtEndIndex)
								{
									nextBlankIndex--;
								}
								firstBlankAtEndIndex--;
							}
							else
							{
								t.hashOrNextIndexForBlanks = nextBlankIndex;
								nextBlankIndex = index;
							}

							t.nextIndex = BlankNextIndexIndicator;

							count--;
							removeCount++;
						}

						priorIndex = index;

						index = nextIndex;
					}
				}
#if !Exclude_No_Hash_Array_Implementation
			}
			else
			{
				int i;
				for (i = count - 1; i >= 0 ; i--)
				{
					if (match.Invoke(noHashArray[i]))
					{
						removeCount++;

						if (i < count - 1)
						{
							int j = i + 1;
							int k = i;
							for ( ; j < count; j++, k++)
							{
								noHashArray[k] = noHashArray[j];
							}
						}

						count--;
					}
				}
			}
#endif

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

				if (y == null)
				{
					return false;
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
#if !Exclude_No_Hash_Array_Implementation
						if (set.isHashing)
						{
#endif
							int pastNodeIndex = set.slots.Length;
							if (set.firstBlankAtEndIndex < pastNodeIndex)
							{
								pastNodeIndex = set.firstBlankAtEndIndex;
							}

							for (int i = 1; i < pastNodeIndex; i++)
							{
								if (set.slots[i].nextIndex != 0) // nextIndex == 0 indicates a blank/available node
								{
									// maybe do ^= instead of add? - will this produce the same thing regardless of order? - if ^= maybe we don't need unchecked
									// sum up the individual item hash codes - this way it won't matter what order the items are in, the same resulting hash code will be produced
									hashCode += set.slots[i].hashOrNextIndexForBlanks;
								}
							}
#if !Exclude_No_Hash_Array_Implementation
						}
						else
						{
							for (int i = 0; i < set.count; i++)
							{
								// sum up the individual item hash codes - this way it won't matter what order the items are in, the same resulting hash code will be produced
								hashCode += set.noHashArray[i].GetHashCode();
							}
						}
#endif
						return hashCode;
					}
				}
			}
		}

		public static IEqualityComparer<FastHashSet<T>> CreateSetComparer()
		{
			return new FastHashSetEqualityComparer();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return new FastHashSetEnumerator<T>(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
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
#if !Exclude_No_Hash_Array_Implementation
				if (set.isHashing)
				{
#endif
					currentIndex = NullIndex; // 0 is the index before the first possible node (0 is the blank node)
#if !Exclude_No_Hash_Array_Implementation
				}
				else
				{
					currentIndex = -1;
				}
#endif

				#if !Exclude_Check_For_Set_Modifications_In_Enumerator
				incrementForEverySetModification = set.incrementForEverySetModification;
				#endif
			}

			public bool MoveNext()
			{
				#if !Exclude_Check_For_Is_Disposed_In_Enumerator
				if (isDisposed)
				{
					// the only reason this code returns false when Disposed is called is to be compatable with HashSet<T>
					// if this level of compatibility isn't needed, then #define Exclude_Check_For_Is_Disposed_In_Enumerator to remove this check and makes the code slightly faster
					return false;
				}
				#endif

				#if !Exclude_Check_For_Set_Modifications_In_Enumerator
				if (incrementForEverySetModification != set.incrementForEverySetModification)
				{
					throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
				}
				#endif

#if !Exclude_No_Hash_Array_Implementation
				if (set.isHashing)
				{
#endif
					// it's easiest to just loop through the node array and skip any nodes that are blank
					// rather than looping through the buckets array and following the nextIndex to the end of each bucket

					while (true)
					{
						currentIndex++;
						if (currentIndex < set.firstBlankAtEndIndex)
						{
							if (set.slots[currentIndex].nextIndex != BlankNextIndexIndicator)
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
#if !Exclude_No_Hash_Array_Implementation
				}
				else
				{
					currentIndex++;
					if (currentIndex < set.count)
					{
						return true;
					}
					else
					{
						currentIndex--;
						return false;
					}
				}
#endif
			}

			public void Reset()
			{
				#if !Exclude_Check_For_Set_Modifications_In_Enumerator
				if (incrementForEverySetModification != set.incrementForEverySetModification)
				{
					throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
				}
				#endif

#if !Exclude_No_Hash_Array_Implementation
				if (set.isHashing)
				{
#endif
					currentIndex = NullIndex; // 0 is the index before the first possible node (0 is the blank node)
#if !Exclude_No_Hash_Array_Implementation
				}
				else
				{
					currentIndex = -1;
				}
#endif
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
#if !Exclude_No_Hash_Array_Implementation
					if (set.isHashing)
					{
#endif
						// it's easiest to just loop through the node array and skip any nodes with nextIndex = 0
						// rather than looping through the buckets array and following the nextIndex to the end of each bucket

						if (currentIndex > NullIndex && currentIndex < set.firstBlankAtEndIndex)
						{
							return set.slots[currentIndex].item;
						}
#if !Exclude_No_Hash_Array_Implementation
					}
					else
					{
						if (currentIndex >= 0 && currentIndex < set.count)
						{
							return set.noHashArray[currentIndex];
						}
					}
#endif
					return default;
				}
			}

			public ref T2 CurrentRef
			{
				get
				{
#if !Exclude_No_Hash_Array_Implementation
					if (set.isHashing)
					{
#endif
						// it's easiest to just loop through the node array and skip any nodes with nextIndex = 0
						// rather than looping through the buckets array and following the nextIndex to the end of each bucket

						if (currentIndex > NullIndex && currentIndex < set.firstBlankAtEndIndex)
						{
							return ref set.slots[currentIndex].item;
						}
						else
						{
							// we can just return a ref to the 0 node's item instead of throwing an exception? - this should have a default item value
							return ref set.slots[0].item;
						}
#if !Exclude_No_Hash_Array_Implementation
					}
					else
					{
						if (currentIndex >= 0 && currentIndex < set.count)
						{
							return ref set.noHashArray[currentIndex];
						}
						else
						{
							// we can just return a ref to the 0 node's item instead of throwing an exception?
							return ref set.noHashArray[0];
						}
					}
#endif
				}
			}

			public bool IsCurrentValid
			{
				get
				{
					//??? why doesn't Current check if the set was modified and throw an exception - there might not even be a current anymore if something was removed?
#if !Exclude_No_Hash_Array_Implementation
					if (set.isHashing)
					{
#endif
						// it's easiest to just loop through the node array and skip any nodes with nextIndex = 0
						// rather than looping through the buckets array and following the nextIndex to the end of each bucket

						if (currentIndex > NullIndex && currentIndex < set.firstBlankAtEndIndex)
						{
							return true;
						}
#if !Exclude_No_Hash_Array_Implementation
					}
					else
					{
						if (currentIndex >= 0 && currentIndex < set.count)
						{
							return true;
						}
					}
#endif
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
