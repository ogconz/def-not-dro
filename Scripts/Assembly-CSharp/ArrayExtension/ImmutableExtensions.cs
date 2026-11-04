using System;
using UnityEngine;

namespace ArrayExtension
{
	public static class ImmutableExtensions
	{
		public static T[] Add<T>(this T[] lhs, T[] rhs)
		{
			if (rhs.IsEmpty())
			{
				if (lhs != null)
				{
					return lhs;
				}
				return new T[0];
			}
			if (lhs.IsEmpty())
			{
				if (rhs != null)
				{
					return rhs;
				}
				return new T[0];
			}
			lhs = lhs.Resize(lhs.Length + rhs.Length);
			rhs.CopyTo(lhs, lhs.Length - rhs.Length);
			return lhs;
		}

		public static T[] Insert<T>(this T[] lhs, int index, T value)
		{
			if (lhs.IsEmpty())
			{
				return new T[1] { value };
			}
			if (index < 0)
			{
				index = 0;
			}
			if (index > lhs.Length)
			{
				index = lhs.Length;
			}
			T[] array = new T[lhs.Length + 1];
			if (index > 0)
			{
				Array.Copy(lhs, 0, array, 0, index);
			}
			array[index] = value;
			if (index < lhs.Length)
			{
				Array.Copy(lhs, index, array, index + 1, lhs.Length - index);
			}
			return array;
		}

		public static T[] Add<T>(this T[] lhs, T rhs)
		{
			if (lhs.IsEmpty())
			{
				return new T[1] { rhs };
			}
			lhs = lhs.Resize(lhs.Length + 1);
			lhs[^1] = rhs;
			return lhs;
		}

		public static int Count<T>(this T[] array, Predicate<T> predicate)
		{
			if (array.IsEmpty())
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < array.Length; i++)
			{
				if (predicate(array[i]))
				{
					num++;
				}
			}
			return num;
		}

		private static int CountInternal<T>(this T[] array, Predicate<T> predicate)
		{
			if (array.IsEmpty())
			{
				return 0;
			}
			Tools.MakeSureSize(ref Tools.PreCache, array.Length);
			int num = 0;
			for (int i = 0; i < array.Length; i++)
			{
				Tools.PreCache[i] = predicate(array[i]);
				if (Tools.PreCache[i])
				{
					num++;
				}
			}
			return num;
		}

		private static int CountInternal<T>(this T[] array, Func<T, int, bool> predicate)
		{
			if (array.IsEmpty())
			{
				return 0;
			}
			Tools.MakeSureSize(ref Tools.PreCache, array.Length);
			int num = 0;
			for (int i = 0; i < array.Length; i++)
			{
				Tools.PreCache[i] = predicate(array[i], i);
				if (Tools.PreCache[i])
				{
					num++;
				}
			}
			return num;
		}

		public static int CountNotNull<T>(this T[] array, Predicate<T> predicate)
		{
			if (array.IsEmpty())
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != null && predicate(array[i]))
				{
					num++;
				}
			}
			return num;
		}

		public static int Count<T>(this T[] array, Func<T, int, bool> predicate)
		{
			if (array.IsEmpty())
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < array.Length; i++)
			{
				if (predicate(array[i], i))
				{
					num++;
				}
			}
			return num;
		}

		public static bool Contains<T>(this T[] array, T value)
		{
			if (array.IsEmpty())
			{
				return false;
			}
			return Array.IndexOf(array, value) != -1;
		}

		public static E[] Convert<T, E>(this T[] array, Converter<T, E> convertor)
		{
			if (array.IsEmpty())
			{
				return new E[0];
			}
			return Array.ConvertAll(array, convertor);
		}

		public static E[] Convert<T, E>(this T[] array, Predicate<T> predicate, Converter<T, E> convertor)
		{
			if (array.IsEmpty())
			{
				return new E[0];
			}
			int num = array.CountInternal(predicate);
			E[] array2 = new E[num];
			int num2 = 0;
			if (num > 0)
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (Tools.PreCache[i])
					{
						array2[num2] = convertor(array[i]);
						num2++;
					}
				}
			}
			return array2;
		}

		public static T[] Copy<T>(this T[] array)
		{
			if (array == null)
			{
				return null;
			}
			if (array.Length == 0)
			{
				return new T[0];
			}
			T[] array2 = new T[array.Length];
			Array.Copy(array, array2, array.Length);
			return array2;
		}

		public static T[] Drop<T>(this T[] array, int count)
		{
			if (count <= 0)
			{
				if (array != null)
				{
					return array;
				}
				return new T[0];
			}
			if (array.IsEmpty() || count >= array.Length)
			{
				return new T[0];
			}
			T[] array2 = new T[array.Length - count];
			Array.Copy(array, count, array2, 0, array.Length - count);
			return array2;
		}

		public static T[] DropRight<T>(this T[] array, int count)
		{
			if (count <= 0)
			{
				if (array != null)
				{
					return array;
				}
				return new T[0];
			}
			if (array.IsEmpty() || count >= array.Length)
			{
				return new T[0];
			}
			T[] array2 = new T[array.Length - count];
			Array.Copy(array, 0, array2, 0, array.Length - count);
			return array2;
		}

		public static bool Exists<T>(this T[] array, Predicate<T> predicate)
		{
			if (array.IsEmpty())
			{
				return false;
			}
			return Array.Exists(array, predicate);
		}

		public static T[] Filter<T>(this T[] array, Predicate<T> predicate)
		{
			if (array.IsEmpty())
			{
				return new T[0];
			}
			return Array.FindAll(array, predicate);
		}

		public static T[] FilterNotNull<T>(this T[] array, Predicate<T> predicate)
		{
			if (array.IsEmpty())
			{
				return new T[0];
			}
			return Array.FindAll(array, (T el) => el != null && predicate(el));
		}

		public static T[] Filter<T>(this T[] array, Func<T, int, bool> predicate)
		{
			if (array.IsEmpty())
			{
				return new T[0];
			}
			int num = array.CountInternal(predicate);
			T[] array2 = new T[num];
			int num2 = 0;
			if (num > 0)
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (Tools.PreCache[i])
					{
						array2[num2] = array[i];
						num2++;
					}
				}
			}
			return array2;
		}

		public static T Find<T>(this T[] array, Predicate<T> predicate)
		{
			if (array.IsEmpty())
			{
				return default(T);
			}
			return Array.Find(array, predicate);
		}

		public static T[] FindAll<T>(this T[] array, Predicate<T> predicate)
		{
			if (array.IsEmpty())
			{
				return new T[0];
			}
			return Array.FindAll(array, predicate);
		}

		public static int FindFirst<T>(this T[] array, T value)
		{
			if (array.IsEmpty())
			{
				return -1;
			}
			return Array.IndexOf(array, value);
		}

		public static int FindLast<T>(this T[] array, T value)
		{
			if (array.IsEmpty())
			{
				return -1;
			}
			return Array.LastIndexOf(array, value);
		}

		public static int IndexOf<T>(this T[] array, T value)
		{
			if (array.IsEmpty())
			{
				return -1;
			}
			return Array.IndexOf(array, value);
		}

		public static bool ForAll<T>(this T[] array, Predicate<T> predicate)
		{
			if (array.IsEmpty())
			{
				return false;
			}
			return Array.TrueForAll(array, predicate);
		}

		public static void ForEach<T>(this T[] array, Action<T> action)
		{
			if (!array.IsEmpty())
			{
				for (int i = 0; i < array.Length; i++)
				{
					action(array[i]);
				}
			}
		}

		public static void ForEach<T>(this T[] array, Action<T, int> action)
		{
			if (!array.IsEmpty())
			{
				for (int i = 0; i < array.Length; i++)
				{
					action(array[i], i);
				}
			}
		}

		public static void ForEachNotNull<T>(this T[] array, Action<T> action)
		{
			if (array.IsEmpty())
			{
				return;
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != null)
				{
					action(array[i]);
				}
			}
		}

		public static int GetLength<T>(this T[] array)
		{
			if (array != null)
			{
				return array.Length;
			}
			return 0;
		}

		public static bool IsEmpty<T>(this T[] array)
		{
			if (array != null)
			{
				return array.Length == 0;
			}
			return true;
		}

		public static bool IsNonEmpty<T>(this T[] array)
		{
			if (array != null)
			{
				return array.Length != 0;
			}
			return false;
		}

		public static T[] MapImut<T>(this T[] array, Converter<T, T> mutator)
		{
			if (array.IsEmpty())
			{
				return new T[0];
			}
			return Array.ConvertAll(array, mutator);
		}

		public static T[] RemoveAt<T>(this T[] array, int index)
		{
			if (array == null || array.IsEmpty() || index < 0 || index >= array.Length)
			{
				return array ?? new T[0];
			}
			T[] array2 = new T[array.Length - 1];
			if (index > 0)
			{
				Array.Copy(array, 0, array2, 0, index);
			}
			if (index < array.Length - 1)
			{
				Array.Copy(array, index + 1, array2, index, array.Length - index - 1);
			}
			return array2;
		}

		public static T[] RemoveAll<T>(this T[] array, T value) where T : class
		{
			if (array.IsEmpty())
			{
				if (array != null)
				{
					return array;
				}
				return new T[0];
			}
			return Array.FindAll(array, (T x) => x != value);
		}

		public static T[] RemoveAllS<T>(this T[] array, T value) where T : struct
		{
			if (array.IsEmpty())
			{
				if (array != null)
				{
					return array;
				}
				return new T[0];
			}
			return Array.FindAll(array, (T x) => !x.Equals(value));
		}

		public static T[] Resize<T>(this T[] array, int newSize)
		{
			if (!array.IsEmpty() && newSize == array.Length)
			{
				return array;
			}
			if (newSize <= 0)
			{
				return new T[0];
			}
			T[] array2 = new T[newSize];
			if (array.IsNonEmpty())
			{
				Array.Copy(array, 0, array2, 0, Math.Min(newSize, array.Length));
			}
			return array2;
		}

		public static T SelectRandom<T>(this T[] array, Predicate<T> predicate)
		{
			if (array.IsEmpty())
			{
				return default(T);
			}
			int num = array.CountInternal(predicate);
			if (num > 0)
			{
				Tools.MakeSureSize(ref Tools.PreCacheInt, array.Length);
				int i = 0;
				int num2 = 0;
				for (; i < array.Length; i++)
				{
					if (Tools.PreCache[i])
					{
						Tools.PreCacheInt[num2] = i;
						num2++;
					}
				}
				return array[Tools.PreCacheInt[UnityEngine.Random.Range(0, num)]];
			}
			return default(T);
		}

		public static int SelectRandomIndex<T>(this T[] array, Predicate<T> predicate)
		{
			if (array.IsEmpty())
			{
				return -1;
			}
			int num = array.CountInternal(predicate);
			if (num > 0)
			{
				Tools.MakeSureSize(ref Tools.PreCacheInt, array.Length);
				int i = 0;
				int num2 = 0;
				for (; i < array.Length; i++)
				{
					if (Tools.PreCache[i])
					{
						Tools.PreCacheInt[num2] = i;
						num2++;
					}
				}
				return Tools.PreCacheInt[UnityEngine.Random.Range(0, num)];
			}
			return -1;
		}

		public static T[][] Split<T>(this T[] array, Predicate<T> predicate)
		{
			if (array.IsEmpty())
			{
				return new T[2][]
				{
					new T[0],
					new T[0]
				};
			}
			int num = array.CountInternal(predicate);
			T[][] array2 = new T[2][]
			{
				new T[num],
				new T[array.Length - num]
			};
			if (num > 0)
			{
				int i = 0;
				int num2 = 0;
				int num3 = 0;
				for (; i < array.Length; i++)
				{
					if (Tools.PreCache[i])
					{
						array2[0][num2] = array[i];
						num2++;
					}
					else
					{
						array2[1][num3] = array[i];
						num3++;
					}
				}
			}
			return array2;
		}

		public static T[] SyncLength<T, E>(this T[] src, E[] dest)
		{
			int length = src.GetLength();
			int length2 = dest.GetLength();
			if (length != length2)
			{
				src = ((length != 0 && length2 != 0) ? src.Resize(length2) : new T[length2]);
			}
			if (src != null)
			{
				return src;
			}
			return new T[0];
		}

		public static bool IsValid(this Array array)
		{
			if (array != null)
			{
				return array.Length > 0;
			}
			return false;
		}
	}
}
