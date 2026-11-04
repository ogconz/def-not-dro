using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArrayExtension
{
	public static class MutableExtensions
	{
		public static void Shuffle<T>(this IList<T> array)
		{
			if (array.Count != 0)
			{
				for (int i = 0; i < array.Count; i++)
				{
					int index = UnityEngine.Random.Range(i, array.Count);
					T value = array[i];
					array[i] = array[index];
					array[index] = value;
				}
			}
		}

		public static void Shuffle<T>(this T[] array)
		{
			if (!array.IsEmpty())
			{
				for (int i = 0; i < array.Length; i++)
				{
					int num = UnityEngine.Random.Range(i, array.Length);
					T val = array[i];
					array[i] = array[num];
					array[num] = val;
				}
			}
		}

		public static void MapMut<T>(this T[] array, Func<T, T> mutator)
		{
			if (!array.IsEmpty())
			{
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = mutator(array[i]);
				}
			}
		}

		public static bool ReplaceFirst<T>(this T[] array, T source, T dest)
		{
			if (array.IsEmpty())
			{
				return false;
			}
			int num = Array.IndexOf(array, source);
			if (num != -1)
			{
				array[num] = dest;
				return true;
			}
			return false;
		}

		public static bool ReplaceFirst<T>(this T[] array, T source, T dest, out int index)
		{
			index = -1;
			if (array.IsEmpty())
			{
				return false;
			}
			index = Array.IndexOf(array, source);
			if (index != -1)
			{
				array[index] = dest;
				return true;
			}
			return false;
		}

		public static void Swap<T>(this T[] array, int i, int j)
		{
			if (!array.IsEmpty())
			{
				T val = array[i];
				array[i] = array[j];
				array[j] = val;
			}
		}
	}
}
