using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class GeneralExtensions
{
	private static Type m_objectType = typeof(object);

	private static Dictionary<Type, Type> m_cacheBaseType = new Dictionary<Type, Type>();

	public static bool IsOneOf<T>(this T valueToFind, params T[] valuesToCheck)
	{
		foreach (T val in valuesToCheck)
		{
			if (valueToFind.Equals(val))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsNoneOf<T>(this T valueToFind, params T[] valuesToCheck)
	{
		return !valueToFind.IsOneOf(valuesToCheck);
	}

	public static bool IsVectorHaveNANs(this Vector3 data)
	{
		if (!float.IsNaN(data.x) && !float.IsNaN(data.y))
		{
			return float.IsNaN(data.z);
		}
		return true;
	}

	public static bool IsRotationHaveNANs(this Quaternion data)
	{
		if (!float.IsNaN(data.x) && !float.IsNaN(data.y) && !float.IsNaN(data.z))
		{
			return float.IsNaN(data.w);
		}
		return true;
	}

	public static T TryParseEnum<T>(string fromValue, T defaultValue)
	{
		Type typeFromHandle = typeof(T);
		if (!typeFromHandle.IsEnum)
		{
			Debug.LogError("Trying to parse enum to NOT a enum type " + typeFromHandle.Name);
			return defaultValue;
		}
		if (!Enum.IsDefined(typeFromHandle, fromValue))
		{
			return defaultValue;
		}
		return (T)Enum.Parse(typeFromHandle, fromValue);
	}

	public static DateTime MakeTime(int fromSecondsFromNow)
	{
		return DateTime.Now + new TimeSpan(0, 0, fromSecondsFromNow);
	}

	public static bool TryParseInvariant(string value, out float result)
	{
		return float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
	}

	public static int GetMultiplayerDayNumber(this DateTime dateTime)
	{
		DateTime dateTime2 = new DateTime(2015, 1, 1, 5, 0, 0);
		return (int)Math.Truncate((dateTime - dateTime2).TotalDays);
	}

	public static T1 With<T, T1>(this T callSite, Func<T, T1> selector) where T : class
	{
		if (callSite == null)
		{
			return default(T1);
		}
		return selector(callSite);
	}

	public static void Call(this Action action)
	{
		action?.Invoke();
	}

	public static void Call<T1, T2>(this Action<T1, T2> action, T1 param1, T2 param2)
	{
		action?.Invoke(param1, param2);
	}

	public static void Call<T1, T2, T3>(this Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3)
	{
		action?.Invoke(param1, param2, param3);
	}

	public static void SafeCall(this Action action)
	{
		try
		{
			action?.Invoke();
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "Debug", "Exception in callback: " + ex);
		}
	}

	public static bool TrySafeCall(this Action action)
	{
		try
		{
			if (action != null)
			{
				action();
				return true;
			}
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "Debug", "Exception in callback: " + ex);
		}
		return false;
	}

	public static void Call<T>(this Action<T> action, T param)
	{
		action?.Invoke(param);
	}

	public static void SafeCall<T>(this Action<T> action, T param)
	{
		try
		{
			action.Call(param);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "Debug", "Exception in callback: " + ex);
		}
	}

	public static void SafeCall<T, R>(this Action<T, R> action, T param1, R param2)
	{
		try
		{
			action?.Invoke(param1, param2);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "Debug", "Exception in callback: " + ex);
		}
	}

	public static Type GetInitialType(this Type type)
	{
		if (!m_cacheBaseType.TryGetValue(type, out var value))
		{
			value = type;
			if (value.BaseType != null)
			{
				while (value.BaseType != m_objectType)
				{
					value = value.BaseType;
				}
			}
			m_cacheBaseType[type] = value;
		}
		return value;
	}

	public static void UntilInitialType(this Type type, Action<Type> action)
	{
		Type type2 = type;
		if (type2.BaseType != null)
		{
			while (type2 != m_objectType)
			{
				action(type2);
				type2 = type2.BaseType;
			}
		}
	}

	public static void UntilType<T>(this Type type, Action<Type> action)
	{
		Type type2 = type;
		Type typeFromHandle = typeof(T);
		if (type2.BaseType != null)
		{
			while (type2 != typeFromHandle && type2 != m_objectType)
			{
				action(type2);
				type2 = type2.BaseType;
			}
		}
	}

	public static List<R> Map<T, R>(this ICollection<T> collection, Func<T, R> provider)
	{
		List<R> list = new List<R>(collection.Count);
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		foreach (T item in collection)
		{
			list.Add(provider(item));
		}
		return list;
	}

	public static R Reduce<T, R>(this ICollection<T> collection, R initialValue, Func<T, R, R> func)
	{
		if (func == null)
		{
			throw new ArgumentNullException("func");
		}
		R val = initialValue;
		foreach (T item in collection)
		{
			val = func(item, val);
		}
		return val;
	}

	public static bool HasAny<T>(this ICollection<T> collection, Func<T, bool> func)
	{
		if (func == null)
		{
			throw new ArgumentNullException("func");
		}
		foreach (T item in collection)
		{
			if (func(item))
			{
				return true;
			}
		}
		return false;
	}

	public static T PopLast<T>(this IList<T> list)
	{
		int num = list.Count - 1;
		if (num < 0)
		{
			return default(T);
		}
		T result = list[num];
		list.RemoveAt(num);
		return result;
	}

	public static void RemoveAtSwapBack<T>(this IList<T> list, int index)
	{
		if (index > list.Count - 1 || index < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (list.Count < 2)
		{
			list.RemoveAt(index);
			return;
		}
		list[index] = list.Last();
		list.RemoveAt(list.Count - 1);
	}

	public static T Last<T>(this IList<T> list)
	{
		int num = list.Count - 1;
		if (num < 0)
		{
			return default(T);
		}
		return list[num];
	}

	public static List<T> GetRange<T>(this IList<T> list, int index, int count)
	{
		int num = Math.Min(count, Math.Max(0, list.Count - index));
		List<T> list2 = new List<T>(num);
		for (int i = 0; i < num; i++)
		{
			list2.Add(list[index + i]);
		}
		return list2;
	}

	public static void InsertionSort<T>(this List<T> list, Comparison<T> comparison)
	{
		if (list == null)
		{
			throw new ArgumentNullException("List is null!");
		}
		if (comparison == null)
		{
			throw new ArgumentNullException("Comparison is null!");
		}
		if (list.Count < 2)
		{
			return;
		}
		for (int i = 1; i < list.Count; i++)
		{
			T val = list[i];
			int num = i - 1;
			while (num >= 0 && comparison(list[num], val) > 0)
			{
				list[num + 1] = list[num];
				num--;
			}
			list[num + 1] = val;
		}
	}

	public static object MemberwiseClone(this object obj)
	{
		if (obj == null || obj.GetType().IsValueType)
		{
			return obj;
		}
		Type type = obj.GetType();
		object obj2 = Activator.CreateInstance(type);
		FieldInfo[] cachedFields = type.GetCachedFields(BindingFlags.Instance | BindingFlags.Public);
		foreach (FieldInfo fieldInfo in cachedFields)
		{
			fieldInfo.SetValue(obj2, fieldInfo.GetValue(obj));
		}
		PropertyInfo[] cachedProperties = type.GetCachedProperties(BindingFlags.Instance | BindingFlags.Public);
		foreach (PropertyInfo propertyInfo in cachedProperties)
		{
			if (propertyInfo.CanRead && propertyInfo.CanWrite && propertyInfo.GetIndexParameters().Length == 0)
			{
				propertyInfo.SetValue(obj2, propertyInfo.GetValue(obj, null), null);
			}
		}
		return obj2;
	}

	public static byte[] ReadFully(this Stream stream)
	{
		if (stream == null)
		{
			return new byte[0];
		}
		if (stream is MemoryStream)
		{
			return ((MemoryStream)stream).ToArray();
		}
		throw new Exception("You can remove this exception, but BEWARE: this code branch was not tested");
	}

	public static byte[] ForcedReadBytes(this Stream stream, int count)
	{
		if (count <= 0)
		{
			return new byte[0];
		}
		if (stream == null)
		{
			throw new Exception("Null stream to read " + count + " bytes from");
		}
		using MemoryStream memoryStream = new MemoryStream(count);
		int bufferLength = 4096;
		if (count < bufferLength)
		{
			bufferLength = count;
		}
		byte[] buffer = new byte[bufferLength];
		int totalReadCount = 0;
		Func<int> func = delegate
		{
			int num2 = count - totalReadCount;
			return (num2 >= bufferLength) ? bufferLength : num2;
		};
		int num;
		while ((num = stream.Read(buffer, 0, func())) > 0)
		{
			memoryStream.Write(buffer, 0, num);
			totalReadCount += num;
		}
		if (totalReadCount < count)
		{
			throw new Exception("Not enough data in stream to read. Required " + count + ", available " + totalReadCount);
		}
		return memoryStream.ToArray();
	}

	public static bool IncludesIndex<T>(this List<T> list, int index) where T : class
	{
		if (list.Count == 0)
		{
			return false;
		}
		if (index >= 0 && index < list.Count)
		{
			return true;
		}
		return false;
	}

	public static bool HasPrefix(this string s, string prefix)
	{
		if (s.Length < prefix.Length)
		{
			return false;
		}
		for (int i = 0; i < prefix.Length; i++)
		{
			if (s[i] != prefix[i])
			{
				return false;
			}
		}
		return true;
	}

	public static bool FastSequenceEqual<T>(this IList<T> a, IList<T> b)
	{
		if ((a == null && b != null) || (a != null && b == null))
		{
			return false;
		}
		if (a == null && b == null)
		{
			return true;
		}
		if (a.Count != b.Count)
		{
			return false;
		}
		for (int i = 0; i < a.Count; i++)
		{
			if (!a[i].Equals(b[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static bool HasParameter(this Animator animator, int nameHash)
	{
		AnimatorControllerParameter[] parameters = animator.parameters;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].nameHash == nameHash)
			{
				return true;
			}
		}
		return false;
	}

	public static bool HasParameter(this Animator animator, string name)
	{
		AnimatorControllerParameter[] parameters = animator.parameters;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].name == name)
			{
				return true;
			}
		}
		return false;
	}
}
