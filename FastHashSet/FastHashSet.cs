//#define Exclude_Check_For_Set_Modifications_In_Enumerator
//#define Exclude_Check_For_Is_Disposed_In_Enumerator
//#define Exclude_Lazy_Initialization_Of_Internal_Arrays
//#define Exclude_No_Hash_Array_Implementation
#define Cache_Optimize_Resize

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

//??? Add XML comments so that summary, etc. shows in intellisense just like it does for HashSet<T>
namespace FastHashSet
{
	public partial class FastHashSet<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ISet<T> //, ISerializable, IDeserializationCallback - ??? implement this within #define
    {
		// this is the size of the non-hash array used for small
		private const int NoHashArraySize = 8;

		// this is the # of initial nodes for the buckets array after going into hashing after using the noHashArray
		// this is 16 + 1 for the first node (node at index 0) which doesn't get used because 0 is the NullIndex
		private const int InitialBucketsArraySize = 17;

		// this indicates end of chain if the nextIndex of a node has this value and also indicates no chain if an indexArray element has this value
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
		private static readonly int[] slotsSizeArray = { 11, 23, 47, 89, 173, 347, 691, 1367, 2741, 5471, 10_937, 19_841/*16_411/*21_851*/, 40_241/*32_771/*43_711*/, 84_463/*65_537/*87_383*/, /*131_101*/174_767,
			/*262_147*/349_529, 699_053, 1_398_107, 2_796_221, 5_592_407, 11_184_829, 22_369_661, 44_739_259, 89_478_503, 17_8956_983, 35_7913_951, 715_827_947, 143_1655_777, LargestPrimeLessThanMaxInt};
			
		private int currentIndexIntoSlotsSizeArray;

		// when an initial capacity is selected in the constructor or later, allocate the required space for indexArray, but
		// limit the # of used elements to optimize for cpu caches
		private static readonly int[] slotsSizeArrayForCacheOptimization = { 3_371, 62_851, 701_819 };

		// the indexArray can be pre-allocated to a large size, but it's not good to use that entire size for hashing because of cache locality
		// instead do at most 3 size steps (for 3 levels of cache) before using its actual allocated size
		private int slotsModSize;

		#if !Exclude_Check_For_Set_Modifications_In_Enumerator
		private int incrementForEverySetModification;
		#endif

		private double loadFactor = .75; //??? maybe remove this and make it a constant

		// resize the slots array when the count reaches this value
		private int resizeSlotsCountThreshold;

		private int count;

		private int nextBlankIndex;

		// this is needed because if items are removed, they get added into the blank list starting at nextBlankIndex, but we may want to TrimExcess capacity, so this is a quick way to see what the ExcessCapacity is
		private int firstBlankAtEndIndex;

		private IEqualityComparer<T> comparer;

		// make the indexArray size a primary number to make the mod function less predictable - use a constant array of prime numbers
		private int[] slots;

		private TNode[] buckets;

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
			CreateNoHashArray();
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
			CreateNoHashArray();
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


		public FastHashSet(IEnumerable<T> collection, bool areAllCollectionItemsDefinitelyUnique = false, int initialCapacity = -1, IEqualityComparer<T> comparer = null)
		{
			//??? what about an initialArraySize = 0 means go straight into hashing

			this.comparer = comparer;
			CreateNoHashArray();
		}

		//??? this could be public if useful somehow
		// maybe add a param to override the initial capacity???
		private void AddInitialUniqueValuesEnumerable(IEnumerable<T> collection, int countOfItemsInCollection)
		{
			if (isHashing)
			{
				nextBlankIndex = 1;
				foreach (T item in collection)
				{
					//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
					int hash = (comparer.GetHashCode(item) & HighBitNotSet);
					int hashIndex = hash % slotsModSize;

					int index = slots[hashIndex];
					slots[hashIndex] = nextBlankIndex;

					ref TNode t = ref buckets[nextBlankIndex];

					t.hashOrNextIndexForBlanks = hash;
					t.nextIndex = index;
					t.item = item;

					nextBlankIndex++;
				}
			}
			else
			{
				int i = 0;
				foreach (T item in collection)
				{
					noHashArray[i++] = item;
				}
			}
			count = countOfItemsInCollection;
			firstBlankAtEndIndex = nextBlankIndex;
		}

		private void AddInitialEnumerableWithEnoughCapacity(IEnumerable<T> collection)
		{
			// this assumes isHashing = true
			foreach (T item in collection)
			{
				//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
				int hash = (comparer.GetHashCode(item) & HighBitNotSet);
				int hashIndex = hash % slotsModSize;

				for (int index = slots[hashIndex]; index != NullIndex; )
				{
					ref TNode t = ref buckets[index];

					if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
					{
						goto Found; // item was found
					}

					index = t.nextIndex;
				}

				ref TNode tBlank = ref buckets[nextBlankIndex];

				tBlank.hashOrNextIndexForBlanks = hash;
				tBlank.nextIndex = slots[hashIndex];
				tBlank.item = item;

				slots[hashIndex] = nextBlankIndex;

				nextBlankIndex++;

#if Cache_Optimize_Resize
				count++;

				if (count >= resizeSlotsCountThreshold)
				{
					ResizeIndexArrayForward(GetNewIndexArraySize());
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
				// and it has access to the internal nodeArray so we don't have to use the foreach/enumerator

				int count = fhset.Count;
				SetInitialCapacity(count);

				if (isHashing)
				{
					if (fhset.isHashing)
					{
						// this FastHashSet is hashing and collection is a FastHashSet (with equal comparer) and it is also hashing

						nextBlankIndex = 1;
						int maxNodeIndex = fhset.buckets.Length - 1;
						if (fhset.firstBlankAtEndIndex <= maxNodeIndex)
						{
							maxNodeIndex = fhset.firstBlankAtEndIndex - 1;
						}

						for (int i = 1; i <= maxNodeIndex; i++)
						{
							ref TNode t2 = ref fhset.buckets[i];
							if (t2.nextIndex != BlankNextIndexIndicator)
							{
								int hash = t2.hashOrNextIndexForBlanks;
								int hashIndex = hash % slotsModSize;

								ref TNode t = ref buckets[nextBlankIndex];

								t.hashOrNextIndexForBlanks = hash;
								t.nextIndex = slots[hashIndex];;
								t.item = t2.item;

								slots[hashIndex] = nextBlankIndex;

								nextBlankIndex++;
							}
						}
						this.count = count;
						firstBlankAtEndIndex = nextBlankIndex;
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
							int hashIndex = hash % slotsModSize;

							ref TNode t = ref buckets[nextBlankIndex];

							t.hashOrNextIndexForBlanks = hash;
							t.nextIndex = slots[hashIndex];
							t.item = item;

							slots[hashIndex] = nextBlankIndex;

							nextBlankIndex++;
						}
					}
				}
				else
				{
					// this FastHashSet is not hashing

					AddInitialUniqueValuesEnumerable(collection, count);
				}
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

					int count = hset.Count;
					SetInitialCapacity(count);

					AddInitialUniqueValuesEnumerable(collection, count);
				}
				else
				{
					ICollection<T> coll = collection as ICollection<T>;
					if (coll != null)
					{
						SetInitialCapacity(coll.Count);
						if (isHashing)
						{
							// call SetInitialCapacity and then set the capacity back to get rid of the excess?

							AddInitialEnumerableWithEnoughCapacity(collection);

							TrimExcess();
						}
						else
						{
							foreach (T item in collection)
							{
								Add(item);
							}
						}
					}
					else
					{
						CreateNoHashArray();

						foreach (T item in collection)
						{
							//??? use in instead of regular Add? Add(in item);
							Add(item);
						}
					}
				}
			}
		}

		private void SetInitialCapacity(int capacity)
		{
			if (capacity > NoHashArraySize)
			{
				// skip using the array and go right into hashing
				InitHashing(capacity);
			}
			else
			{
				CreateNoHashArray(); // don't set the capacity/size of the noHashArray
			}
		}

		// this function can be called to switch from using initialArray and go into hashing
		// this function can also be called before initialArray is even allocated in order to skip using the array and go right into hashing
		private void SwitchFromArrayToHashing(int capacityIncrease = -1)
		{
			InitHashing(capacityIncrease);

			if (noHashArray != null)
			{
				// i is the index into initialArray
				for (int i = 0; i < count; i++)
				{
					ref T item = ref noHashArray[i];

					//??? if item is a value type, won't comparing to null cause boxing? - or is the jit compiler smart enough to not do that?
					//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
					int hash = (comparer.GetHashCode(item) & HighBitNotSet);
					int hashIndex = hash % slotsModSize;

					ref TNode t = ref buckets[nextBlankIndex];

					t.hashOrNextIndexForBlanks = hash;
					t.nextIndex = slots[hashIndex];
					t.item = item;

					slots[hashIndex] = nextBlankIndex;

					nextBlankIndex++;
				}
				noHashArray = null; // this array can now be garbage collected because it is no longer referenced
			}

			firstBlankAtEndIndex = nextBlankIndex;
		}

		private void InitHashing(int capacity = -1)
		{
			int newNodeArraySize;
			int newIndexArraySize;
			int newIndexArrayModSize;

			bool setThresh = false;
			if (capacity == -1)
			{
				newNodeArraySize = InitialBucketsArraySize;

				newIndexArraySize = slotsSizeArray[0];
				if (newIndexArraySize < newNodeArraySize)
				{
					for (currentIndexIntoSlotsSizeArray = 1; currentIndexIntoSlotsSizeArray < slotsSizeArray.Length; currentIndexIntoSlotsSizeArray++)
					{
						newIndexArraySize = slotsSizeArray[currentIndexIntoSlotsSizeArray];
						if (newIndexArraySize >= newNodeArraySize)
						{
							break;
						}
					}
				}
				newIndexArrayModSize = newIndexArraySize;
			}
			else
			{
				newNodeArraySize = capacity + 1; // add 1 to accomodate blank first node (node at 0 index)

				newIndexArraySize = GetEqualOrClosestHigherPrime((int)(newNodeArraySize / loadFactor));

				#if Cache_Optimize_Resize
				if (newIndexArraySize > slotsSizeArrayForCacheOptimization[0])
				{
					newIndexArrayModSize = slotsSizeArrayForCacheOptimization[0];
					setThresh = true;
				}
				else
				#endif
				{
					newIndexArrayModSize = newIndexArraySize;
				}
			}
			
			if (newNodeArraySize == 0)
			{
				// this is an error, the int.MaxValue has been used for capacity and we require more - throw an Exception for this
				//??? it's not really out of memory, if running 64 bit and you have alot of virtual memory you could possibly get here and still have memory - try this with HashSet<uint> and see what it error it gives
				//throw new OutOfMemoryException();
			}

			buckets = new TNode[newNodeArraySize]; // the nodeArray has an extra item as it's first item (0 index) that is for available items - the memory is wasted, but it simplifies things
			slots = new int[newIndexArraySize]; // these will be initially set to 0, so make 0 the blank(available) value and reduce all indices by one to get to the actual index into the nodeArray
			slotsModSize = newIndexArrayModSize;

			if (setThresh)
			{
				resizeSlotsCountThreshold = (int)(newIndexArrayModSize * loadFactor);
			}
			else
			{
				CalcUsedItemsLoadFactorThreshold();
			}

			nextBlankIndex = 1; // start at 1 because 0 is the blank item

			firstBlankAtEndIndex = nextBlankIndex;

			isHashing = true;
		}
		
		private void CreateNoHashArray()
		{
			noHashArray = new T[NoHashArraySize];
		}

		private void CalcUsedItemsLoadFactorThreshold()
		{
			if (slots != null)
			{
				if (slots.Length == slotsModSize)
				{
					resizeSlotsCountThreshold = buckets.Length; // with this value, the indexArray should always resize after the nodeArray (in the same public function call)
				}
				else
				{
					// when indexArray.Length > indexArrayModSize, this means we want to more slowly increase the indexArrayModSize to keep things in the L1-3 caches
					resizeSlotsCountThreshold = (int)(slotsModSize * loadFactor);
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

			if (isHashing)
			{
				int pastNodeIndex = buckets.Length;
				if (firstBlankAtEndIndex < pastNodeIndex)
				{
					pastNodeIndex = firstBlankAtEndIndex;
				}

				int cnt = 0;
				for (int i = 1; i < pastNodeIndex; i++)
				{
					if (buckets[i].nextIndex != BlankNextIndexIndicator)
					{
						array[arrayIndex++] = buckets[i].item;
						if (++cnt == count)
						{
							break;
						}
					}
				}
			}
			else
			{
				int cnt = this.count;
				if (cnt > count)
				{
					cnt = count;
				}

				// for small arrays, I think the for loop below will actually be faster than Array.Copy - could test this???
				//Array.Copy(initialArray, 0, array, arrayIndex, cnt);

				for (int i = 0; i < cnt; i++)
				{
					array[arrayIndex++] = noHashArray[i];
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
				return count;
			}
		}

		//??? maybe we don't need a public prop. for this
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
					excessCapacity = buckets.Length - firstBlankAtEndIndex;
				}
				else
				{
					excessCapacity = noHashArray.Length - count;
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
					return buckets.Length - 1; // subtract 1 for blank node at 0 index
				}
				else
				{
					return noHashArray.Length;
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
				currentCapacity = buckets.Length - count;
			}
			else
			{
				currentCapacity = noHashArray.Length - count;
			}

			if (currentCapacity < capacity)
			{
				IncreaseCapacity(capacity - currentCapacity);
			}

			//??? is this correct - this should be the number where the next lowest number would force a resize of indexArray with the current loadfactor and the entire nodeArray is full
			int calcedNewIndexArraySize = (int)(buckets.Length / loadFactor) + 1;

			if (calcedNewIndexArraySize < 0 && calcedNewIndexArraySize > LargestPrimeLessThanMaxInt)
			{
				calcedNewIndexArraySize = LargestPrimeLessThanMaxInt;
			}
			else
			{
				calcedNewIndexArraySize = GetEqualOrClosestHigherPrime(calcedNewIndexArraySize);
			}

			if (slots.Length < calcedNewIndexArraySize)
			{
				// -1 means stop trying to increase the size based on the array of primes
				// instead calc based on 2 * existing length and then get the next higher prime
				currentIndexIntoSlotsSizeArray = -1;

				ResizeIndexArrayForward(calcedNewIndexArraySize);
			}

			return buckets.Length - count;
		}

		// return true if indexArrayModSize was set, false otherwise
		private bool CheckForModSizeIncrease()
		{
			if (slotsModSize < slots.Length)
			{
				// instead of array, just have 3 constants
				int partLength = (int)(slots.Length * .75);

				int size0 = slotsSizeArrayForCacheOptimization[0];
				int size1 = slotsSizeArrayForCacheOptimization[1];
				if (slotsModSize == size0)
				{
					if (size1 <= partLength)
					{
						slotsModSize = size1;
						return true;
					}
					else
					{
						slotsModSize = slots.Length;
						return true;
					}
				}
				else
				{
					int size2 = slotsSizeArrayForCacheOptimization[2];
					if (slotsModSize == size1)
					{
						if (size2 <= partLength)
						{
							slotsModSize = size2;
							return true;
						}
						else
						{
							slotsModSize = slots.Length;
							return true;
						}
					}
					else if (slotsModSize == size2)
					{
						slotsModSize = slots.Length;
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

		private int GetNewNodeArraySizeIncrease(out int oldArraySize, bool getOnlyDefaultSize = false)
		{
			if (buckets != null)
			{
				oldArraySize = buckets.Length;
			}
			else
			{
				//??? should probably start at 8 if NOT doing the initial non-hashing array - we start at 32 if doing the initial non-hashing array
				oldArraySize = 17; // this isn't the old node array or the initialArray, but it is the initial size we should start at - this will create a new node array of Length = 33
			}
			//else if (initialArray != null)
			//{
			//	oldArraySize = initialArray.Length; // this isn't the old node array, but it is the old # of items that could be stored without resizing
			//}
			//else
			//{
			//	oldArraySize = InitialItemsTableSize; // this isn't the old node array or the initialArray, but it is the initial size we should start at
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

		// if the value returned gets used and that value is different than the current indexArray.Length, then the calling code should increment currentIndexIntoSizeArray because this would now be the current
		private int GetNewIndexArraySize()
		{
			//??? to avoid to many allocations of this array, setting the initialCapacity in the constructor or MaxCapacity or NextCapacityIncreaseOverride should determine
			// where the currentIndexIntoSizeArray is pointing to and also the capacity of this indexArray (which should be a prime)		public int MaxCapacity { get; set; } = -1;

			int newArraySize;

			if (currentIndexIntoSlotsSizeArray >= 0)
			{
				if (currentIndexIntoSlotsSizeArray + 1 < slotsSizeArray.Length)
				{
					newArraySize = slotsSizeArray[currentIndexIntoSlotsSizeArray + 1];
				}
				else
				{
					newArraySize = slots.Length;
				}
			}
			else
			{
				// -1 means stop trying to increase the size based on the array of primes
				// instead calc based on 2 * existing length and then get the next higher prime
				newArraySize = slots.Length;
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

		// if hashing, increase the size of the nodeArray
		// if not yet hashing, switch to hashing
		private void IncreaseCapacity(int capacityIncrease = -1)
		{
			//??? this function might be a fair bit over overhead for resizing at small sizes (like 33 and 65)
			//- could try to reduce the overhead - there could just be a nextNodeArraySize (don't need increase?), or nextNodeArraySizeIncrease?
			// then we don't have to call GetNewNodeArraySizeIncrease at all?
			// could test the overhead by just replacing all of the code with 
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
					oldNodeArraySize = buckets.Length;
				}

				if (newNodeArraySizeIncrease <= 0)
				{
					//??? throw an error
				}

				int newNodeArraySize = oldNodeArraySize + newNodeArraySizeIncrease;

				//#if false
				TNode[] newNodeArray = new TNode[newNodeArraySize];
				Array.Copy(buckets, 0, newNodeArray, 0, buckets.Length); // check the IL, I think Array.Resize and Array.Copy without the start param calls this, so avoid the overhead by calling directly
				buckets = newNodeArray;
				//#endif
				//Array.Resize(ref nodeArray, newNodeArraySize);

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
					oldNodeArraySize = buckets.Length;
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
			if (newIndexArraySize == slots.Length)
			{
				// this will still work if no increase in size - it just might be slower than if you could increase the indexArray size
			}
			else
			{
				//??? what if there is a high percent of blank/unused items in the nodeArray before the firstBlankAtEndIndex (mabye because of lots of removes)?
				// It would probably be faster to loop through the indexArray and then do chaining to find the used nodes - one problem with this is that you would have to find blank nodes - but they would be chained
				// this probably isn't a very likely scenario

				if (!CheckForModSizeIncrease()) //??? clean this up, it isn't really good to do it this way - no need to call GetNewIndexArraySize before calling this function
				{
					slots = new int[newIndexArraySize];
					slotsModSize = newIndexArraySize;

					if (currentIndexIntoSlotsSizeArray >= 0)
					{
						currentIndexIntoSlotsSizeArray++; // when the newIndexArraySize gets used in the above code, point to the next avaialble size - ??? not sure this is the best place to increment this
					}
				}
				else
				{
					Array.Clear(slots, 0, slotsModSize);
				}

				CalcUsedItemsLoadFactorThreshold();

				int indexArrayLength = slots.Length;

				int pastNodeIndex = buckets.Length;
				if (firstBlankAtEndIndex < pastNodeIndex)
				{
					pastNodeIndex = firstBlankAtEndIndex;
				}

				//??? for a loop where the end is array.Length, the compiler can skip any array bounds checking - can it do it for this code - it should be able to because pastIndex is no more than indexArray.Length
				if (firstBlankAtEndIndex == count + 1)
				{
					// this means there aren't any blank nodes
					for (int i = 1; i < pastNodeIndex; i++)
					{
						ref TNode t = ref buckets[i];

						int hashIndex = t.hashOrNextIndexForBlanks % indexArrayLength;
						t.nextIndex = slots[hashIndex];

						slots[hashIndex] = i;
					}
				}
				else
				{
					// this means there are some blank nodes
					for (int i = 1; i < pastNodeIndex; i++)
					{
						ref TNode t = ref buckets[i];
						if (t.nextIndex != BlankNextIndexIndicator) // skip blank nodes
						{
							int hashIndex = t.hashOrNextIndexForBlanks % indexArrayLength;
							t.nextIndex = slots[hashIndex];

							slots[hashIndex] = i;
						}
					}
				}
			}
		}

		private void ResizeIndexArrayForwardKeepMarks(int newIndexArraySize)
		{
			if (newIndexArraySize == slots.Length)
			{
				// this will still work if no increase in size - it just might be slower than if you could increase the indexArray size
			}
			else
			{
				//??? what if there is a high percent of blank/unused items in the nodeArray before the firstBlankAtEndIndex (mabye because of lots of removes)?
				// It would probably be faster to loop through the indexArray and then do chaining to find the used nodes - one problem with this is that you would have to find blank nodes - but they would be chained
				// this probably isn't a very likely scenario

				if (!CheckForModSizeIncrease()) //??? clean this up, it isn't really good to do it this way - no need to call GetNewIndexArraySize before calling this function
				{
					slots = new int[newIndexArraySize];
					slotsModSize = newIndexArraySize;

					if (currentIndexIntoSlotsSizeArray >= 0)
					{
						currentIndexIntoSlotsSizeArray++; // when the newIndexArraySize gets used in the above code, point to the next avaialble size - ??? not sure this is the best place to increment this
					}
				}

				CalcUsedItemsLoadFactorThreshold();

				int indexArrayLength = slots.Length;

				int pastNodeIndex = buckets.Length;
				if (firstBlankAtEndIndex < pastNodeIndex)
				{
					pastNodeIndex = firstBlankAtEndIndex;
				}

				//??? for a loop where the end is array.Length, the compiler can skip any array bounds checking - can it do it for this code - it should be able to because pastIndex is no more than indexArray.Length
				if (firstBlankAtEndIndex == count + 1)
				{
					// this means there aren't any blank nodes
					for (int i = 1; i < pastNodeIndex; i++)
					{
						ref TNode t = ref buckets[i];

						int hashIndex = t.hashOrNextIndexForBlanks % indexArrayLength;
						t.nextIndex = slots[hashIndex] | (t.nextIndex & MarkNextIndexBitMask);

						slots[hashIndex] = i;
					}
				}
				else
				{
					// this means there are some blank nodes
					for (int i = 1; i < pastNodeIndex; i++)
					{
						ref TNode t = ref buckets[i];
						if (t.nextIndex != BlankNextIndexIndicator) // skip blank nodes
						{
							int hashIndex = t.hashOrNextIndexForBlanks % indexArrayLength;
							t.nextIndex = slots[hashIndex] | (t.nextIndex & MarkNextIndexBitMask);

							slots[hashIndex] = i;
						}
					}
				}
			}
		}

		//??? remove if not used
		private void ResizeIndexArrayReverse(int newIndexArraySize)
		{
			if (newIndexArraySize == slots.Length)
			{
				// this will still work if no increase in size - it just might be slower than if you could increase the indexArray size
			}
			else
			{
				//??? what if there is a high percent of blank/unused items in the nodeArray before the firstBlankAtEndIndex (mabye because of lots of removes)?
				// It would probably be faster to loop through the indexArray and then do chaining to find the used nodes - one problem with this is that you would have to find blank nodes - but they would be chained
				// this probably isn't a very likely scenario
				slots = new int[newIndexArraySize];
				slotsModSize = newIndexArraySize;

				if (currentIndexIntoSlotsSizeArray >= 0)
				{
					currentIndexIntoSlotsSizeArray++; // when the newIndexArraySize gets used in the above code, point to the next avaialble size - ??? not sure this is the best place to increment this
				}

				CalcUsedItemsLoadFactorThreshold();

				int indexArrayLength = slots.Length;

				int lastNodeIndex = buckets.Length - 1;
				if (firstBlankAtEndIndex < lastNodeIndex)
				{
					lastNodeIndex = firstBlankAtEndIndex - 1;
				}

				if (nextBlankIndex >= firstBlankAtEndIndex) // we know there aren't any blanks within the nodes, so skip the if (<not blank node>) check
				{
					for (int i = lastNodeIndex; i >= 1 ; i--)
					{
						ref TNode t = ref buckets[i];

						int hashIndex = t.hashOrNextIndexForBlanks % indexArrayLength;
						t.nextIndex = slots[hashIndex];

						slots[hashIndex] = i;
					}				
				}
				else
				{
					for (int i = lastNodeIndex; i >= 1 ; i--)
					{
						ref TNode t = ref buckets[i];
						if (t.nextIndex != BlankNextIndexIndicator)
						{
							int hashIndex = t.hashOrNextIndexForBlanks % indexArrayLength;
							t.nextIndex = slots[hashIndex];

							slots[hashIndex] = i;
						}
					}
				}
			}
		}

		//??? rewrite this to be correct if used
		private void ResizeIndexArrayForwardAndCopy(int newIndexArraySize, TNode[] oldNodeArray)
		{
			if (newIndexArraySize == slots.Length)
			{
				// this will still work if no increase in size - it just might be slower than if you could increase the indexArray size
			}
			else
			{
				//??? what if there is a high percent of blank/unused items in the nodeArray before the firstBlankAtEndIndex (mabye because of lots of removes)?
				// It would probably be faster to loop through the indexArray and then do chaining to find the used nodes - one problem with this is that you would have to find blank nodes - but they would be chained
				// this probably isn't a very likely scenario
				slots = new int[newIndexArraySize];
				slotsModSize = newIndexArraySize;

				if (currentIndexIntoSlotsSizeArray >= 0)
				{
					currentIndexIntoSlotsSizeArray++; // when the newIndexArraySize gets used in the above code, point to the next avaialble size - ??? not sure this is the best place to increment this
				}

				CalcUsedItemsLoadFactorThreshold();

				int indexArrayLength = slots.Length;

				int lastNodeIndex = buckets.Length - 1;
				if (firstBlankAtEndIndex < lastNodeIndex)
				{
					lastNodeIndex = firstBlankAtEndIndex - 1;
				}

				if (nextBlankIndex >= firstBlankAtEndIndex) // we know there aren't any blanks within the nodes, so skip the if (<not blank node>) check
				{
					for (int i = 1; i <= lastNodeIndex; i++)
					{
						ref TNode t = ref buckets[i];
						t = oldNodeArray[i]; // copy node from old to new

						int hashIndex = t.hashOrNextIndexForBlanks % slotsModSize;

						slots[hashIndex] = i;
						t.nextIndex = slots[hashIndex];
					}				
				}
				else
				{
					for (int i = 1; i <= lastNodeIndex; i++)
					{
						ref TNode t = ref buckets[i];
						t = oldNodeArray[i]; // copy node from old to new
						if (t.nextIndex != BlankNextIndexIndicator)
						{
							int hashIndex = t.hashOrNextIndexForBlanks % slotsModSize;

							slots[hashIndex] = i;
							t.nextIndex = slots[hashIndex];
						}
					}
				}
			}
		}

		//??? why is this not used
		private void ResizeIndexArrayWithMarks(int newIndexArraySize)
		{
			if (newIndexArraySize == slots.Length)
			{
				// this will still work if no increase in size - it just might be slower than if you could increase the indexArray size
			}
			else
			{
				//??? what if there is a high percent of blank/unused items in the nodeArray before the firstBlankAtEndIndex (mabye because of lots of removes)?
				// It would probably be faster to loop through the indexArray and then do chaining to find the used nodes - one problem with this is that you would have to find blank nodes - but they would be chained
				// this probably isn't a very likely scenario
				slots = new int[newIndexArraySize];
				slotsModSize = newIndexArraySize;

				if (currentIndexIntoSlotsSizeArray >= 0)
				{
					currentIndexIntoSlotsSizeArray++; // when the newIndexArraySize gets used in the above code, point to the next avaialble size - ??? not sure this is the best place to increment this
				}

				CalcUsedItemsLoadFactorThreshold();

				int indexArrayLength = slots.Length; //??? look at the IL code to see if it helps to put this often used property into a local variable or not

				int pastNodeIndex = buckets.Length;
				if (firstBlankAtEndIndex < pastNodeIndex)
				{
					pastNodeIndex = firstBlankAtEndIndex;
				}

				//??? for a loop where the end is array.Length, the compiler can skip any array bounds checking - can it do it for this code - it should be able to because pastIndex is no more than indexArray.Length
				if (firstBlankAtEndIndex == count + 1)
				{
					// this means there aren't any blank nodes
					for (int i = 1; i < pastNodeIndex; i++)
					{
						ref TNode t = ref buckets[i];

						int hashIndex = t.hashOrNextIndexForBlanks % indexArrayLength;
						t.nextIndex = slots[hashIndex] | (t.nextIndex & MarkNextIndexBitMask); // if marked, keep the mark

						slots[hashIndex] = i;
					}
				}
				else
				{
					// this means there are some blank nodes
					for (int i = 1; i < pastNodeIndex; i++)
					{
						ref TNode t = ref buckets[i];
						if (t.nextIndex != BlankNextIndexIndicator) // skip blank nodes
						{
							int hashIndex = t.hashOrNextIndexForBlanks % indexArrayLength;
							t.nextIndex = slots[hashIndex] | (t.nextIndex & MarkNextIndexBitMask); // if marked, keep the mark

							slots[hashIndex] = i;
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
				Array.Clear(slots, 0, slots.Length);
			}

			count = 0;
		}

		//
		public void ClearAndTrimAll()
		{
			#if !Exclude_Check_For_Set_Modifications_In_Enumerator
			incrementForEverySetModification++;
			#endif

			// this would deallocate the arrays - would need to lazy allocate the arrays if allowing this (if (nodeArray == null) .. InitForHashing() if (initialArray == null) InitForInitialArray()
			//??? I don't think the time to always check for the arrays would make this worth it (but HashSet does this - with branch prediction being correct most of the time I think it wouldn't add that much time) - could just set the BasicHashSet variable to null in this case and reset it with constructor when needed?
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
				if (buckets != null && buckets.Length > firstBlankAtEndIndex && firstBlankAtEndIndex > 0)
				{
					Array.Resize(ref buckets, firstBlankAtEndIndex);
					// when firstBlankAtEndIndex == nodeArray.Length, that means there are no blank at end items
				}
			}
			else
			{
				if (noHashArray != null && noHashArray.Length > count && count > 0)
				{
					Array.Resize(ref noHashArray, count);
				}
			}
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

			if (isHashing)
			{
				//bool increasedCapacity = false;

				//??? consider doing the  &  & HighBitNotSet on the comparer.GetHashCode(item); - this could mess with comparing equals (maybe -2,000,000,000 would compare equals to 1...) by just their hash codes, but this should be rare
				//int hash = item == null ? 0 : comparer.GetHashCode(item);
				//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
				int hash = (comparer.GetHashCode(item) & HighBitNotSet);
				int hashIndex = hash % slotsModSize;

				for (int index = slots[hashIndex]; index != NullIndex; )
				{
					ref TNode t = ref buckets[index];

					if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
					{
						return false; // item was found, so return false to indicate it was not added
					}

					index = t.nextIndex;
				}

				if (nextBlankIndex >= buckets.Length)
				{
					// there aren't any more blank nodes to add items, so we need to increase capacity
					IncreaseCapacity();
					//increasedCapacity = true;
				}

				int firstIndex = slots[hashIndex];
				slots[hashIndex] = nextBlankIndex;

				ref TNode tBlank = ref buckets[nextBlankIndex];
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

				if (count >= resizeSlotsCountThreshold)
				//if (increasedCapacity)
				{
					ResizeIndexArrayForward(GetNewIndexArraySize());
				}

				return true;
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
					SwitchFromArrayToHashing();

					//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
					int hash = (comparer.GetHashCode(item) & HighBitNotSet);
					int hashIndex = hash % slotsModSize;

					ref TNode tBlank = ref buckets[nextBlankIndex];

					tBlank.hashOrNextIndexForBlanks = hash;
					tBlank.nextIndex = slots[hashIndex];
					tBlank.item = item;

					slots[hashIndex] = nextBlankIndex;

					nextBlankIndex = ++firstBlankAtEndIndex;

					count++;

					return true;
				}
				else
				{
					// add to initialArray
					noHashArray[i] = item;
					count++;
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
				//bool increasedCapacity = false;

				//??? consider doing the  &  & HighBitNotSet on the comparer.GetHashCode(item); - this could mess with comparing equals (maybe -2,000,000,000 would compare equals to 1...) by just their hash codes, but this should be rare
				//int hash = item == null ? 0 : comparer.GetHashCode(item);
				//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
				int hash = (comparer.GetHashCode(item) & HighBitNotSet);
				int hashIndex = hash % slotsModSize;

				for (int index = slots[hashIndex]; index != NullIndex; )
				{
					ref TNode t = ref buckets[index];

					if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
					{
						return false; // item was found, so return false to indicate it was not added
					}

					index = t.nextIndex;
				}

				if (nextBlankIndex >= buckets.Length)
				{
					// there aren't any more blank nodes to add items, so we need to increase capacity
					IncreaseCapacity();
					//increasedCapacity = true;
				}

				int firstIndex = slots[hashIndex];
				slots[hashIndex] = nextBlankIndex;

				ref TNode tBlank = ref buckets[nextBlankIndex];
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

				if (count >= resizeSlotsCountThreshold)
				//if (increasedCapacity)
				{
					ResizeIndexArrayForward(GetNewIndexArraySize());
				}

				return true;
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
					SwitchFromArrayToHashing();

					//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
					int hash = (comparer.GetHashCode(item) & HighBitNotSet);
					int hashIndex = hash % slotsModSize;

					ref TNode tBlank = ref buckets[nextBlankIndex];

					tBlank.hashOrNextIndexForBlanks = hash;
					tBlank.nextIndex = slots[hashIndex];
					tBlank.item = item;

					slots[hashIndex] = nextBlankIndex;

					nextBlankIndex = ++firstBlankAtEndIndex;

					count++;

					return true;
				}
				else
				{
					// add to initialArray
					noHashArray[i] = item;
					count++;
					return true;
				}
			}
		}

		//??? remove if not used - this was inlined in both Add methods
		private void AddToHashSet(in T item)
		{
			// this assumes we are hashing and there is enough capacity and the item is already not found and there are no chained blanks

			//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
			int hash = (comparer.GetHashCode(item) & HighBitNotSet);
			int hashIndex = hash % slotsModSize;

			ref TNode tBlank = ref buckets[nextBlankIndex];

			tBlank.hashOrNextIndexForBlanks = hash;
			tBlank.nextIndex = slots[hashIndex];
			tBlank.item = item;

			slots[hashIndex] = nextBlankIndex;

			nextBlankIndex = ++firstBlankAtEndIndex;

			count++;
		}

		// return the node index that was added, or NullIndex if item was found
		private int AddToHashSetIfNotFound(in T item, int hash)
		{
			int hashIndex = hash % slotsModSize;

			for (int index = slots[hashIndex]; index != NullIndex; )
			{
				ref TNode t = ref buckets[index];

				if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
				{
					return NullIndex; // item was found, so return NullIndex to indicate it was not added
				}

				index = t.nextIndex;
			}

			if (nextBlankIndex >= buckets.Length)
			{
				// there aren't any more blank nodes to add items, so we need to increase capacity
				IncreaseCapacity();
				ResizeIndexArrayForward(GetNewIndexArraySize());

				// fix things messed up by indexArray resize
				hashIndex = hash % slotsModSize;
			}

			int firstIndex = slots[hashIndex];
			slots[hashIndex] = nextBlankIndex;

			int addedNodeIndex = nextBlankIndex;
			ref TNode tBlank = ref buckets[nextBlankIndex];
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
			int hashIndex = hash % slotsModSize;

			for (int index = slots[hashIndex]; index != NullIndex; )
			{
				ref TNode t = ref buckets[index];

				if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
				{
					return NullIndex; // item was found, so return NullIndex to indicate it was not added
				}

				index = t.nextIndex & MarkNextIndexBitMaskInverted;;
			}

			if (nextBlankIndex >= buckets.Length)
			{
				// there aren't any more blank nodes to add items, so we need to increase capacity
				IncreaseCapacity();
				ResizeIndexArrayForwardKeepMarks(GetNewIndexArraySize());

				// fix things messed up by indexArray resize
				hashIndex = hash % slotsModSize;
			}

			int firstIndex = slots[hashIndex];
			slots[hashIndex] = nextBlankIndex;

			int addedNodeIndex = nextBlankIndex;
			ref TNode tBlank = ref buckets[nextBlankIndex];
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
			if (isHashing)
			{
				//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
				int hash = (comparer.GetHashCode(item) & HighBitNotSet);
				int hashIndex = hash % slotsModSize;

				for (int index = slots[hashIndex]; index != NullIndex; )
				{
					ref TNode t = ref buckets[index];

					if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
					{
						return true; // item was found, so return true
					}

					index = t.nextIndex;
				}
				return false;
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
		}

		// does in make a difference??? - test this with larger structs and compare against the non-in version
		public bool Contains(in T item)
		{
			if (isHashing)
			{
				//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
				int hash = (comparer.GetHashCode(item) & HighBitNotSet);
				int hashIndex = hash % slotsModSize;

				for (int index = slots[hashIndex]; index != NullIndex; )
				{
					ref TNode t = ref buckets[index];

					if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
					{
						return true; // item was found, so return true
					}

					index = t.nextIndex;
				}
				return false;
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

			if (isHashing)
			{
				//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
				int hash = (comparer.GetHashCode(item) & HighBitNotSet);
				int hashIndex = hash % slotsModSize;

				int priorIndex = NullIndex;

				for (int index = slots[hashIndex]; index != NullIndex; )
				{
					ref TNode t = ref buckets[index];

					if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
					{
						// item was found, so remove it

						if (priorIndex == NullIndex)
						{
							slots[hashIndex] = t.nextIndex;
						}
						else
						{
							buckets[priorIndex].nextIndex = t.nextIndex;
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
			}
			return false;
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
				//int addedNodeIndex = AddToHashSetIfNotFound(in item, item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet));
				int addedNodeIndex = AddToHashSetIfNotFound(in item, (comparer.GetHashCode(item) & HighBitNotSet));
				isFound = (addedNodeIndex != NullIndex);
				return ref buckets[addedNodeIndex].item;
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
					SwitchFromArrayToHashing();
					return ref FindOrAdd(in item, out isFound);
				}
				else
				{
					// add to initialArray and keep isAdded true
					noHashArray[i] = item;
					count++;
					return ref noHashArray[i];
				}
			}
		}

		// return index into nodeArray or 0 if not found

		//??? to make things faster, could have a FindInNodeArray that just returns foundNodeIndex and another version called FindWithPriorInNodeArray that has the 3 out params
		// first test to make sure this works as is
		private void FindInNodeArray(in T item, out int foundNodeIndex, out int priorNodeIndex, out int indexArrayIndex)
		{
			foundNodeIndex = NullIndex;
			priorNodeIndex = NullIndex;

			//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);
			int hash = (comparer.GetHashCode(item) & HighBitNotSet);
			int hashIndex = hash % slotsModSize;

			indexArrayIndex = hashIndex;

			int priorIndex = NullIndex;

			for (int index = slots[hashIndex]; index != NullIndex; )
			{
				ref TNode t = ref buckets[index];

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
		private bool FindInNodeArray(in T item, int hash)
		{
			int hashIndex = hash % slotsModSize;

			for (int index = slots[hashIndex]; index != NullIndex; )
			{
				ref TNode t = ref buckets[index];

				if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
				{
					return true; // item was found, so return true
				}

				index = t.nextIndex;;
			}
			return false;
		}

		private bool FindInInitialArray(in T item)
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

		private void UnmarkAllNextIndexValues(int maxNodeIndex)
		{
			// must be hashing to be here
			for (int i = 1; i <= maxNodeIndex; i++)
			{
				buckets[i].nextIndex &= MarkNextIndexBitMaskInverted;
			}
		}

		// removeMarked = true, means remove the marked items and keep the unmarked items
		// removeMarked = false, means remove the unmarked items and keep the marked items
		private void UnmarkAllNextIndexValuesAndRemoveAnyMarkedOrUnmarked(bool removeMarked)
		{
			// must be hashing to be here

			// must traverse all of the chains instead of just looping through the nodeArray because going through the chains is the only way to set
			// nodes within a chain to blank and still be able to remove the blank node from the chain

			int index;
			int nextIndex;
			int priorIndex;
			int lastNonBlankIndex = firstBlankAtEndIndex - 1;
			for (int i = 0; i < slots.Length; i++)
			{
				priorIndex = NullIndex; // 0 means use indexArray
				index = slots[i];

				while (index != NullIndex)
				{
					ref TNode t = ref buckets[index];
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
							slots[i] = nextIndex;
						}
						else
						{
							buckets[priorIndex].nextIndex = nextIndex;
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

		private FoundType FindInNodeArrayAndMark(in T item, out int foundNodeIndex)
		{
			//int hash = item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet);;
			int hash = (comparer.GetHashCode(item) & HighBitNotSet);;
			int hashIndex = hash % slotsModSize;

			int index = slots[hashIndex];

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
					ref TNode t = ref buckets[index];
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

			return ref noHashArray[0];
		}

		// this is similar to HashSet<T>.TryGetValue, except it returns a ref to the value rather than a copy of the value found (TryGetValue uses an out parameter)
		// this way you can modify the actual value in the set if it is a value type (you can always modify the object if it is a reference type - except I think if it is a string)
		// also passing the item by in and the return by ref is faster for larger structs than passing by value
		public ref T Find(in T item, out bool isFound)
		{
			isFound = false;
			if (isHashing)
			{
				FindInNodeArray(item, out int foundNodeIndex, out int priorNodeIndex, out int indexArrayIndex);
				if (foundNodeIndex != NullIndex)
				{
					isFound = true;
				}

				return ref buckets[foundNodeIndex].item; // if not found, then return a ref to the first node item, which isn't used for anything other than this
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
		}

		public bool TryGetValue(T equalValue, out T actualValue)
		{
			if (isHashing)
			{
				FindInNodeArray(equalValue, out int foundNodeIndex, out int priorNodeIndex, out int indexArrayIndex);
				if (foundNodeIndex > 0)
				{
					actualValue = buckets[foundNodeIndex].item;
					return true;
				}

				actualValue = default;
				return false;
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

			if (isHashing)
			{
				foreach (T item in other)
				{
					//AddToHashSetIfNotFound(in item, item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet));
					AddToHashSetIfNotFound(in item, (comparer.GetHashCode(item) & HighBitNotSet));
				}
			}
			else
			{
				int i;

				foreach (T item in other)
				{
					//??? if it's easier for the jit compiler or il compiler to remove the array bounds checking then
					// have i < initialArray.Length and do the check for usedItemsCount within the loop with a break statment
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
						SwitchFromArrayToHashing();
						//AddToHashSetIfNotFound(in item, item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet));
						AddToHashSetIfNotFound(in item, (comparer.GetHashCode(item) & HighBitNotSet));
					}
					else
					{
						// add to initialArray
						noHashArray[i] = item;
						count++;
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
			}
			else
			{
				// Note: we could actually do this faster by moving any found items to the front and keeping track of the found items
				// with a single int index
				// the problem with this method is it reorders items and even though that shouldn't matter in a set
				// it might cause issues with code that incorrectly assumes order stays the same for operations like this

				// possibly a faster implementation would be to use the method above, but keep track of original order with an int array of the size of usedItemsCount (ex. item at 0 was originally 5, and also item at 5 was originally 0)

				//??? should stackalloc be used - this requires unsafe - could also use an int and bit array if <=32/64 used items - might be better than allocating an array on the heap
				byte[] foundItemArray = new byte[count]; // 0 means corresponding item was not yet found, 1 means it was

				int i;

				int foundItemCount = 0; // the count of found items in the hash - without double counting
				foreach (T item in other)
				{
					for (i = 0; i < count; i++)
					{
						if (comparer.Equals(item, noHashArray[i]))
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
						if (foundItemArray[i] == 0)
						{
							if (i < count - 1)
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

					if (notFoundAtLeastOne && foundItemCount == count)
					{
						// true means all of the items in the set were found in other and at least one item in other was not found in the set
						break; // will return true below after unmarking
					}
				}

				UnmarkAllNextIndexValues(maxFoundIndex);

				return notFoundAtLeastOne && foundItemCount == count; // true if all of the items in the set were found in other and at least one item in other was not found in the set
			}
			else
			{
				// just use bool instead of byte because bool is always just 1 byte anyway - also use ulong if itemCount <= 64???
				byte[] foundItemArray = new byte[count]; // 0 means corresponding item was not yet found, 1 means it was

				int i;

				int foundItemCount = 0; // the count of found items in the hash - without double counting
				bool notFoundAtLeastOne = false;
				foreach (T item in other)
				{
					for (i = 0; i < count; i++)
					{
						if (comparer.Equals(item, noHashArray[i]))
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
					if (notFoundAtLeastOne && foundItemCount == count)
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

						if (foundItemCount == count)
						{
							break;
						}
					}
				}

				UnmarkAllNextIndexValues(maxFoundIndex);

				return foundItemCount == count; // true if all of the items in the set were found in other
			}
			else
			{
				byte[] foundItemArray = new byte[count]; // 0 means corresponding item was not yet found, 1 means it was

				int i;

				int foundItemCount = 0; // the count of found items in the hash - without double counting
				foreach (T item in other)
				{
					for (i = 0; i < count; i++)
					{
						if (comparer.Equals(item, noHashArray[i]))
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
					if (foundItemCount == count)
					{
						break;
					}
				}

				return foundItemCount == count; // true if all of the items in the set were found in other
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
			}
			else
			{
				byte[] foundItemArray = new byte[count]; // 0 means corresponding item was not yet found, 1 means it was

				int i;

				int foundItemCount = 0; // the count of found items in the hash - without double counting
				foreach (T item in other)
				{
					for (i = 0; i < count; i++)
					{
						if (comparer.Equals(item, noHashArray[i]))
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
					if (foundItemCount == count)
					{
						break;
					}
				}

				return foundItemCount < count; // true if all of the items in other were found in set and at least one item in set was not found in other
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

			if (count == 0)
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
					//if (!FindInNodeArray(in item, item == null ? 0 : (comparer.GetHashCode(item) & HighBitNotSet)))
					if (!FindInNodeArray(in item, (comparer.GetHashCode(item) & HighBitNotSet)))
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

					if (isHashing)
					{
						int pastNodeIndex = buckets.Length;
						if (firstBlankAtEndIndex < pastNodeIndex)
						{
							pastNodeIndex = firstBlankAtEndIndex;
						}

						if (fhset.isHashing)
						{
							for (int i = 1; i < pastNodeIndex; i++)
							{
								//??? could not do the blank check if we know there aren't any blanks - below code and in the loop in the else
								if (buckets[i].nextIndex != BlankNextIndexIndicator) // skip any blank nodes
								{
									if (!fhset.FindInNodeArray(in buckets[i].item, buckets[i].hashOrNextIndexForBlanks))
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
								if (buckets[i].nextIndex != BlankNextIndexIndicator) // skip any blank nodes
								{
									if (!fhset.FindInInitialArray(in buckets[i].item))
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

				return foundItemCount == count;
			}
			else
			{
				// if we need to create an array for this, maybe create bitarray - it will be smaller
				//??? maybe there could a max size of initial array of 64, then we don't need 2 sets of code - test to see how fast 64 is
				byte[] foundItemArray = new byte[count]; // 0 means corresponding item was not yet found, 1 means it was

				int i;

				int foundItemCount = 0; // the count of found items in the hash - without double counting
				foreach (T item in other)
				{
					for (i = 0; i < count; i++)
					{
						if (comparer.Equals(item, noHashArray[i]))
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

				return foundItemCount == count;
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
			int hashIndex = hash % slotsModSize;

			int priorIndex = NullIndex;

			for (int index = slots[hashIndex]; index != NullIndex; )
			{
				ref TNode t = ref buckets[index];

				if (t.hashOrNextIndexForBlanks == hash && comparer.Equals(t.item, item))
				{
					// item was found, so remove it if not marked
					if ((t.nextIndex & MarkNextIndexBitMask) == 0)
					{
						if (priorIndex == NullIndex)
						{
							slots[hashIndex] = t.nextIndex;
						}
						else
						{
							// if nodeArray[priorIndex].nextIndex was marked, then keep it marked
							// already know that t.nextIndex is not marked
							buckets[priorIndex].nextIndex = t.nextIndex | (buckets[priorIndex].nextIndex & MarkNextIndexBitMask);
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

			if (isHashing)
			{
				// must traverse all of the chains instead of just looping through the nodeArray because going through the chains is the only way to set
				// nodes within a chain to blank and still be able to remove the blnak node from the chain

				int priorIndex;
				int nextIndex;
				for (int i = 0; i < slots.Length; i++)
				{
					priorIndex = NullIndex; // 0 means use indexArray

					for (int index = slots[i]; index != NullIndex; )
					{
						ref TNode t = ref buckets[index];

						nextIndex = t.nextIndex;
						if (match.Invoke(t.item))
						{
							// item was matched, so remove it

							if (priorIndex == NullIndex)
							{
								slots[i] = nextIndex;
							}
							else
							{
								buckets[priorIndex].nextIndex = nextIndex;
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
							int pastNodeIndex = set.buckets.Length;
							if (set.firstBlankAtEndIndex < pastNodeIndex)
							{
								pastNodeIndex = set.firstBlankAtEndIndex;
							}

							for (int i = 1; i < pastNodeIndex; i++)
							{
								if (set.buckets[i].nextIndex != 0) // nextIndex == 0 indicates a blank/available node
								{
									// maybe do ^= instead of add? - will this produce the same thing regardless of order? - if ^= maybe we don't need unchecked
									// sum up the individual item hash codes - this way it won't matter what order the items are in, the same resulting hash code will be produced
									hashCode += set.buckets[i].hashOrNextIndexForBlanks;
								}
							}
						}
						else
						{
							for (int i = 0; i < set.count; i++)
							{
								// sum up the individual item hash codes - this way it won't matter what order the items are in, the same resulting hash code will be produced
								hashCode += set.noHashArray[i].GetHashCode();
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
					currentIndex = NullIndex; // 0 is the index before the first possible node (0 is the blank node)
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
					// it's easiest to just loop through the node array and skip any nodes that are blank
					// rather than looping through the indexArray and following the nextIndex to the end of each bucket

					while (true)
					{
						currentIndex++;
						if (currentIndex < set.firstBlankAtEndIndex)
						{
							if (set.buckets[currentIndex].nextIndex != BlankNextIndexIndicator)
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
					currentIndex = NullIndex; // 0 is the index before the first possible node (0 is the blank node)
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

						if (currentIndex > NullIndex && currentIndex < set.firstBlankAtEndIndex)
						{
							return set.buckets[currentIndex].item;
						}
					}
					else
					{
						if (currentIndex >= 0 && currentIndex < set.count)
						{
							return set.noHashArray[currentIndex];
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

						if (currentIndex > NullIndex && currentIndex < set.firstBlankAtEndIndex)
						{
							return ref set.buckets[currentIndex].item;
						}
						else
						{
							// we can just return a ref to the 0 node's item instead of throwing an exception? - this should have a default item value
							return ref set.buckets[0].item;
						}
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

						if (currentIndex > NullIndex && currentIndex < set.firstBlankAtEndIndex)
						{
							return true;
						}
					}
					else
					{
						if (currentIndex >= 0 && currentIndex < set.count)
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
