//#define RunLongTests
//#define RunVeryLongTests

using Xunit;
using Motvin.Collections;
using System.Text;
using System.Collections.Generic;
using System;

namespace FastHashSetTestsExtra
{
    public class ExtraTests
    {

		public static bool IsPrime(int number)
		{
			if (number <= 2) return false;
			if (number % 2 == 0) return (number == 2);
			int root = (int)System.Math.Sqrt((double)number);
			for (int i = 3; i <= root; i += 2)
			{
				if (number % i == 0) return false;
			}
			return true;
		}

		[Fact]
		public void Prime()
		{
			int p = FastHashSet<int>.FastHashSetUtil.GetEqualOrClosestHigherPrime(2);

			Assert.True(p == 3);
		}

		[Fact]
		public void Prime2()
		{
			for (int i = 2; i < 100_000; i++)
			{
				int p = FastHashSet<int>.FastHashSetUtil.GetEqualOrClosestHigherPrime(i);

				for (int j = i; j < p; j++)
				{
					//if (IsPrime(j))
					//{
					//	int adf = 1;
					//}
					Assert.False(IsPrime(j));

				}
				Assert.True(IsPrime(p));
			}
		}

		#if RunLongTests
		[Fact]
		public void Prime3()
		{
			for (int i = 100_000; i < 10_000_000; i++)
			{
				int p = FastHashSet<int>.FastHashSetUtil.GetEqualOrClosestHigherPrime(i);

				for (int j = i; j < p; j++)
				{
					//if (IsPrime(j))
					//{
					//	int adf = 1;
					//}
					Assert.False(IsPrime(j));

				}
				Assert.True(IsPrime(p));
			}
		}
		#endif

		#if RunVeryLongTests
		[Fact]
		public void Prime4()
		{
			for (int i = 10_000_000; i < 100_000_000; i++)
			{
				int p = FastHashSet<int>.FastHashSetUtil.GetEqualOrClosestHigherPrime(i);

				for (int j = i; j < p; j++)
				{
					//if (IsPrime(j))
					//{
					//	int adf = 1;
					//}
					Assert.False(IsPrime(j));

				}
				Assert.True(IsPrime(p));
			}
		}
		#endif

		[Fact]
		public void AddIn()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			int x = 1;
			int x2 = 2;
			int x3 = 3;
			int x4 = 4;
			int x5 = 5;
			int x6 = 6;
			int x7 = 7;
			int x8 = 8;
			int x9 = 9;
			int x10 = 10;
			int x11 = 11;
			int x12 = 12;
			set.Add(in x);
			set.Add(in x2);
			set.Add(in x3);
			set.Add(in x4);
			set.Add(in x5);
			set.Add(in x6);
			set.Add(in x7);
			set.Add(in x8);
			set.Add(in x9);
			set.Add(in x10);
			set.Add(in x11);
			set.Add(in x12);

			Assert.Contains(1, set);
			Assert.True(set.Count == 12);
		}

		[Fact]
		public void AddIn2()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			int x = 1;
			set.Add(in x);

			Assert.Contains(1, set);
		}

		[Fact]
		public void ContainsIn()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			int x = 1;
			int x2 = 2;
			int x3 = 3;
			int x4 = 4;
			int x5 = 5;
			int x6 = 6;
			int x7 = 7;
			int x8 = 8;
			int x9 = 9;
			int x10 = 10;
			int x11 = 11;
			int x12 = 12;
			set.Add(in x);
			set.Add(in x2);
			set.Add(in x3);
			set.Add(in x4);
			set.Add(in x5);
			set.Add(in x6);
			set.Add(in x7);
			set.Add(in x8);
			set.Add(in x9);
			set.Add(in x10);
			set.Add(in x11);
			set.Add(in x12);
			set.Remove(x12);

			Assert.True(set.Contains(in x));
			Assert.True(set.Contains(in x2));
			Assert.True(set.Contains(in x3));
			Assert.True(set.Contains(in x4));
			Assert.True(set.Contains(in x5));
			Assert.True(set.Contains(in x6));
			Assert.True(set.Contains(in x7));
			Assert.True(set.Contains(in x8));
			Assert.True(set.Contains(in x9));
			Assert.True(set.Contains(in x10));
			Assert.True(set.Contains(in x11));
			Assert.False(set.Contains(in x12));
		}

		[Fact]
		public void ContainsIn2()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			int x = 1;
			set.Add(in x);
			Assert.True(set.Contains(in x));
		}

		[Fact]
		public void RemoveIf()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			int x = 1;
			int x2 = 2;
			int x3 = 3;
			int x4 = 4;
			int x5 = 5;
			int x6 = 6;
			int x7 = 7;
			int x8 = 8;
			int x9 = 9;
			int x10 = 10;
			int x11 = 11;
			int x12 = 12;
			set.Add(in x);
			set.Add(in x2);
			set.Add(in x3);
			set.Add(in x4);
			set.Add(in x5);
			set.Add(in x6);
			set.Add(in x7);
			set.Add(in x8);
			set.Add(in x9);
			set.Add(in x10);
			set.Add(in x11);
			set.Add(in x12);

			bool isRemoved = set.RemoveIf(in x, n => n < 2);

			Assert.True(isRemoved);
			Assert.DoesNotContain(1, set);

			isRemoved = set.RemoveIf(x2, n => n < 2);

			Assert.False(isRemoved);
			Assert.Contains(x2, set);

			set.Add(in x);
			bool isRemoved2 = set.RemoveIf(in x, n => n > 2);

			Assert.False(isRemoved2);
			Assert.Contains(1, set);
		}

		[Fact]
		public void RemoveIf2()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			int x = 1;
			set.Add(in x);

			bool isRemoved = set.RemoveIf(in x, n => n < 2);

			Assert.True(isRemoved);
			Assert.DoesNotContain(1, set);

			set.Add(in x);
			bool isRemoved2 = set.RemoveIf(in x, n => n > 2);

			Assert.False(isRemoved2);
			Assert.Contains(1, set);
		}

		public struct IDCount
		{
			public int id;
			public int count;

			public IDCount(int id, int count)
			{
				this.id = id;
				this.count = count;
			}

			public override int GetHashCode()
			{
				return id;
			}

			public override bool Equals(object obj)
			{
				return this.id == ((IDCount)obj).id;
			}
		}

		[Fact]
		public void FindOrAdd()
		{
			FastHashSet<IDCount> set = new FastHashSet<IDCount>();
			IDCount x = new IDCount(1, 1);
			bool isFound;
			ref IDCount xRef = ref set.FindOrAdd(in x, out isFound);
			if (isFound)
			{
				xRef.count++;
			}

			bool added = set.Add(new IDCount(1, 1)); // alrady added
			Assert.False(added);

			set.Add(new IDCount(2, 1));
			set.Add(new IDCount(3, 1));
			set.Add(new IDCount(4, 1));
			set.Add(new IDCount(6, 1));
			set.Add(new IDCount(7, 1));
			set.Add(new IDCount(8, 1));
			set.Add(new IDCount(9, 1));
			set.Add(new IDCount(10, 1));
			set.Add(new IDCount(11, 1));
			
			Assert.False(isFound);

			xRef = ref set.FindOrAdd(in x, out isFound);
			if (isFound)
			{
				xRef.count++;
			}
			Assert.True(isFound);
			Assert.True(xRef.count == 2);

			Assert.Contains(x, set);
		}

		[Fact]
		public void FindOrAdd2()
		{
			FastHashSet<IDCount> set = new FastHashSet<IDCount>();
			IDCount x = new IDCount(1, 1);
			bool isFound;
			ref IDCount xRef = ref set.FindOrAdd(in x, out isFound);
			if (isFound)
			{
				xRef.count++;
			}

			Assert.False(isFound);

			xRef = ref set.FindOrAdd(in x, out isFound);
			if (isFound)
			{
				xRef.count++;
			}
			Assert.True(isFound);
			Assert.True(xRef.count == 2);

			Assert.Contains(x, set);
		}

		[Fact]
		public void Find()
		{
			FastHashSet<IDCount> set = new FastHashSet<IDCount>();
			IDCount x = new IDCount(1, 123);
			set.Add(in x);
			set.Add(new IDCount(1, 1)); // already added with 123 as count
			set.Add(new IDCount(2, 1));
			set.Add(new IDCount(3, 1));
			set.Add(new IDCount(4, 1));
			set.Add(new IDCount(6, 1));
			set.Add(new IDCount(7, 1));
			set.Add(new IDCount(8, 1));
			set.Add(new IDCount(9, 1));
			set.Add(new IDCount(10, 1));
			set.Add(new IDCount(11, 1));
			bool isFound;
			ref IDCount xRef = ref set.Find(in x, out isFound);
			if (isFound)
			{
				xRef.count++;
			}

			Assert.True(isFound);

			xRef = ref set.Find(in x, out isFound);

			Assert.True(isFound);
			Assert.True(xRef.count == 124);

			Assert.Contains(x, set);
		}

		[Fact]
		public void ReorderChainedNodesToBeAdjacent()
		{
			FastHashSet<int> set = new FastHashSet<int>();
			int x = 1;
			set.Add(in x);

			Random rand = new Random(17);
			int itemCnt = 10_000;
			int maxVal = itemCnt * 4;
			for (int i = 0; i < itemCnt; i++)
			{
				set.Add(rand.Next(0, maxVal));
			}

			int cnt = set.Count;
			List<ChainLevelAndCount> lst = set.GetChainLevelsCounts(out double avgNodeVisitsPerChain);
			set.ReorderChainedNodesToBeAdjacent();
			List<ChainLevelAndCount> lst2 = set.GetChainLevelsCounts(out double avgNodeVisitsPerChain2);

			Assert.Contains(x, set);
			Assert.True(set.Count == cnt);
		}

		[Fact]
		public void Find2()
		{
			FastHashSet<IDCount> set = new FastHashSet<IDCount>();
			IDCount x = new IDCount(1, 123);
			set.Add(in x);
			bool isFound;
			ref IDCount xRef = ref set.Find(in x, out isFound);
			if (isFound)
			{
				xRef.count++;
			}

			Assert.True(isFound);

			xRef = ref set.Find(in x, out isFound);

			Assert.True(isFound);
			Assert.True(xRef.count == 124);

			Assert.Contains(x, set);
		}

		[Fact]
		public void FindAndRemoveIf()
		{
			FastHashSet<IDCount> set = new FastHashSet<IDCount>();
			IDCount x = new IDCount(5, 1);

			set.Add(new IDCount(1, 1));
			set.Add(new IDCount(2, 1));
			set.Add(new IDCount(3, 1));
			set.Add(new IDCount(4, 1));
			set.Add(new IDCount(6, 1));
			set.Add(new IDCount(7, 1));
			set.Add(new IDCount(8, 1));
			set.Add(new IDCount(9, 1));
			set.Add(new IDCount(10, 1));
			set.Add(new IDCount(11, 1));

			bool isFound;
			bool isRemoved;

			// below is how you can increment a ref counted item and add if the current count = 1
			ref IDCount xRef = ref set.FindOrAdd(in x, out isFound);
			if (isFound)
			{
				xRef.count++;
			}
			Assert.False(isFound);

			xRef = ref set.FindOrAdd(in x, out isFound);
			if (isFound)
			{
				xRef.count++;
			}
			Assert.True(isFound);

			// below is how you can decrement a ref counted item and remove if the current count = 1
			xRef = ref set.FindAndRemoveIf(in x, n => n.count == 1, out isFound, out isRemoved);
			if (!isRemoved && isFound)
			{
				xRef.count--;
			}

			Assert.True(isFound);
			Assert.False(isRemoved);

			xRef = ref set.FindAndRemoveIf(in x, n => n.count == 1, out isFound, out isRemoved);
			if (!isRemoved && isFound)
			{
				xRef.count--;
			}

			Assert.True(isFound);
			Assert.True(isRemoved);

			Assert.DoesNotContain(x, set);
		}

		[Fact]
		public void FindAndRemoveIf2()
		{
			FastHashSet<IDCount> set = new FastHashSet<IDCount>();
			IDCount x = new IDCount(5, 1);

			set.Add(new IDCount(1, 1));
			set.Add(new IDCount(2, 1));
			set.Add(new IDCount(3, 1));

			bool isFound;
			bool isRemoved;

			// below is how you can increment a ref counted item and add if the current count = 1
			ref IDCount xRef = ref set.FindOrAdd(in x, out isFound);
			if (isFound)
			{
				xRef.count++;
			}
			Assert.False(isFound);

			xRef = ref set.FindOrAdd(in x, out isFound);
			if (isFound)
			{
				xRef.count++;
			}
			Assert.True(isFound);

			// below is how you can decrement a ref counted item and remove if the current count = 1
			xRef = ref set.FindAndRemoveIf(in x, n => n.count == 1, out isFound, out isRemoved);
			if (!isRemoved && isFound)
			{
				xRef.count--;
			}

			Assert.True(isFound);
			Assert.False(isRemoved);

			xRef = ref set.FindAndRemoveIf(in x, n => n.count == 1, out isFound, out isRemoved);
			if (!isRemoved && isFound)
			{
				xRef.count--;
			}

			Assert.True(isFound);
			Assert.True(isRemoved);

			Assert.DoesNotContain(x, set);
		}
	}
}
