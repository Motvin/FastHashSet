using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashSetBench
{
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
			if (obj.GetType() != typeof(SmallStruct))
			{
				return false;
			}

			return Equals((SmallStruct)obj);
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
			return !c1.Equals(c2);
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

	public struct SmallStructIntVal : IEquatable<SmallStructIntVal>
	{
		public int myInt;
		public int refCountOrSum;

		public SmallStructIntVal(int i, int refCountOrSum)
		{
			myInt = i;
			this.refCountOrSum = refCountOrSum;
		}

		public override int GetHashCode()
		{
			return myInt;
		}

		public override bool Equals(object obj)
		{
			if (obj.GetType() != typeof(SmallStructIntVal))
			{
				return false;
			}

			return Equals((SmallStruct)obj);
		}

		public bool Equals(SmallStructIntVal other)
		{
			return (myInt == other.myInt);
		}

		public static bool operator ==(SmallStructIntVal c1, SmallStructIntVal c2)
		{
			return c1.Equals(c2);
		}

		public static bool operator !=(SmallStructIntVal c1, SmallStructIntVal c2)
		{
			return !c1.Equals(c2);
		}
	}

	public sealed class SmallClassIntVal : IEquatable<SmallClassIntVal>
	{
		public int myInt;
		public int refCountOrSum;

		public SmallClassIntVal(int i, int refCountOrSum)
		{
			myInt = i;
			this.refCountOrSum = refCountOrSum;
		}

		public override int GetHashCode()
		{
			return myInt;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as SmallClassIntVal);
		}

		public bool Equals(SmallClassIntVal other)
		{
			if (other == null)
			{
				return false;
			}

			return (myInt == other.myInt);
		}
	}

	// this should be 8 bytes in size
	public struct SmallStructBasic
	{
		public int myInt;
		public int myInt2;

		public SmallStructBasic(int i, int i2)
		{
			myInt = i;
			myInt2 = i2;
		}
	}

	public class SmallClassBasic
	{
		public int myInt;
		public int myInt2;

		public SmallClassBasic(int i, int i2)
		{
			myInt = i;
			myInt2 = i2;
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
			if (obj.GetType() != typeof(MediumStruct))
			{
				return false;
			}

			return Equals((MediumStruct)obj);
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
            return !c1.Equals(c2);
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

			return Equals(obj as MediumClass);
		}

		public bool Equals(MediumClass other)
		{
			if (other == null)
			{
				return false;
			}

			return (myInt == other.myInt && myDouble == other.myDouble && myDate == other.myDate);
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
				StringRandUtil.uppercaseChars,
				StringRandUtil.uppercaseChars,
				StringRandUtil.lowercaseChars,
				StringRandUtil.lowercaseChars,
				StringRandUtil.digits,
				StringRandUtil.space,
				StringRandUtil.symbols,
				};
			string s = StringRandUtil.CreateRandomString(rand, 10, 12, strFreq);

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
			if (obj.GetType() != typeof(LargeStruct))
			{
				return false;
			}

			return Equals((LargeStruct)obj);
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
            return !c1.Equals(c2);
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
				StringRandUtil.uppercaseChars,
				StringRandUtil.uppercaseChars,
				StringRandUtil.lowercaseChars,
				StringRandUtil.lowercaseChars,
				StringRandUtil.digits,
				StringRandUtil.space,
				StringRandUtil.symbols,
				};
			string s = StringRandUtil.CreateRandomString(rand, 10, 12, strFreq);

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

			return Equals(obj as LargeClass);
		}

		public bool Equals(LargeClass other)
		{
			if (other == null)
			{
				return false;
			}

			return (myInt == other.myInt && myInt2 == other.myInt2 && myInt3 == other.myInt3 && myDouble == other.myDouble && myDouble2 == other.myDouble2 && myDate == other.myDate && myString == other.myString);
		}
	}

	// this should be about 64 bytes, not including the space for the actual string bytes
	public struct VeryLargeStruct : IEquatable<VeryLargeStruct>
	{
		public DateTime myDate;
		public double myDouble;
		public double myDouble2;
		public int myInt;
		public int myInt2;
		public int myInt3;
		public string myString;

		public VeryLargeStruct(DateTime dt, double d, double d2, int i, int i2, int i3, string s)
		{
			myDate = dt;
			myDouble = d;
			myDouble2 = d2;
			myInt = i;
			myInt2 = i2;
			myInt3 = i3;
			myString = s;
		}

		public static VeryLargeStruct CreateRand(Random rand)
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
				StringRandUtil.uppercaseChars,
				StringRandUtil.uppercaseChars,
				StringRandUtil.lowercaseChars,
				StringRandUtil.lowercaseChars,
				StringRandUtil.digits,
				StringRandUtil.space,
				StringRandUtil.symbols,
				};
			string s = StringRandUtil.CreateRandomString(rand, 10, 12, strFreq);

			return new VeryLargeStruct(new DateTime(year, month, day), d, d2, i, i2, i3, s);
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
			if (obj.GetType() != typeof(VeryLargeStruct))
			{
				return false;
			}

			return Equals((VeryLargeStruct)obj);
		}

		public bool Equals(VeryLargeStruct other)
		{
			return (myInt == other.myInt && myInt2 == other.myInt2 && myInt3 == other.myInt3 && myDouble == other.myDouble && myDouble2 == other.myDouble2 && myDate == other.myDate && myString == other.myString);
		}

        public static bool operator ==(VeryLargeStruct c1, VeryLargeStruct c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(VeryLargeStruct c1, VeryLargeStruct c2)
        {
            return !c1.Equals(c2);
        }
	}

	// this should be about 64 bytes, not including the space for the actual string bytes
	public sealed class VeryLargeClass : IEquatable<VeryLargeClass>
	{
		public DateTime myDate;
		public double myDouble;
		public double myDouble2;
		public int myInt;
		public int myInt2;
		public int myInt3;
		public string myString;

		public VeryLargeClass(DateTime dt, double d, double d2, int i, int i2, int i3, string s)
		{
			myDate = dt;
			myDouble = d;
			myDouble2 = d2;
			myInt = i;
			myInt2 = i2;
			myInt3 = i3;
			myString = s;
		}

		public static VeryLargeClass CreateRand(Random rand)
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
				StringRandUtil.uppercaseChars,
				StringRandUtil.uppercaseChars,
				StringRandUtil.lowercaseChars,
				StringRandUtil.lowercaseChars,
				StringRandUtil.digits,
				StringRandUtil.space,
				StringRandUtil.symbols,
				};
			string s = StringRandUtil.CreateRandomString(rand, 10, 12, strFreq);

			return new VeryLargeClass(new DateTime(year, month, day), d, d2, i, i2, i3, s);
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

			return Equals(obj as VeryLargeClass);
		}

		public bool Equals(VeryLargeClass other)
		{
			if (other == null)
			{
				return false;
			}

			return (myInt == other.myInt && myInt2 == other.myInt2 && myInt3 == other.myInt3 && myDouble == other.myDouble && myDouble2 == other.myDouble2 && myDate == other.myDate && myString == other.myString);
		}
	}
}
