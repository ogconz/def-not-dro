using System;
using System.Collections.Generic;
using System.Reflection;
using SyncMultiplayer;
using UnityEngine;

public static class DynamicTypesManager
{
	private static readonly Dictionary<char, List<KeyValuePair<string, Type>>> m_cachedTypesDictionary;

	private static Dictionary<string, List<Type>> m_dynamicTypesByName;

	private static Dictionary<Type, List<Type>> m_typesByInterface;

	private static List<Type> m_cachedTypesResult;

	private static Dictionary<BindingFlags, Dictionary<int, FieldInfo[]>> m_fieldsCache;

	private static Dictionary<BindingFlags, Dictionary<int, PropertyInfo[]>> m_propertiesCache;

	private static Dictionary<BindingFlags, Dictionary<int, MethodInfo[]>> m_methodsCache;

	private static Dictionary<Module, Dictionary<int, int>> m_propertyModulesCache;

	private static Dictionary<long, bool> m_isDefinedAttrCache;

	static DynamicTypesManager()
	{
		m_cachedTypesDictionary = new Dictionary<char, List<KeyValuePair<string, Type>>>(64);
		m_dynamicTypesByName = new Dictionary<string, List<Type>>(64);
		m_typesByInterface = new Dictionary<Type, List<Type>>(64);
		m_cachedTypesResult = new List<Type>(64);
		m_fieldsCache = new Dictionary<BindingFlags, Dictionary<int, FieldInfo[]>>(64);
		m_propertiesCache = new Dictionary<BindingFlags, Dictionary<int, PropertyInfo[]>>(64);
		m_methodsCache = new Dictionary<BindingFlags, Dictionary<int, MethodInfo[]>>(64);
		m_propertyModulesCache = new Dictionary<Module, Dictionary<int, int>>(64);
		m_isDefinedAttrCache = new Dictionary<long, bool>(256);
		InitiateDictionary();
	}

	public static Type GetFirstDerivedType<T>(string name)
	{
		if (m_dynamicTypesByName.TryGetValue(name, out var value))
		{
			foreach (Type item in value)
			{
				if (item.IsSubclassOf(typeof(T)))
				{
					return item;
				}
			}
		}
		return null;
	}

	public static List<Type> GetAssemblyTypes(string name)
	{
		m_cachedTypesResult.Clear();
		List<KeyValuePair<string, Type>> list = m_cachedTypesDictionary[name[0]];
		for (int i = 0; i < list.Count; i++)
		{
			KeyValuePair<string, Type> keyValuePair = list[i];
			if (FastStringCompare(keyValuePair.Key, name))
			{
				m_cachedTypesResult.Add(keyValuePair.Value);
			}
		}
		return m_cachedTypesResult;
	}

	private static bool FastStringCompare(string a, string b)
	{
		int length = a.Length;
		int length2 = b.Length;
		if (length != length2)
		{
			return false;
		}
		for (int i = 0; i < length; i++)
		{
			if (a[i] != b[i])
			{
				return false;
			}
		}
		return true;
	}

	public static List<Type> GetInterfacedTypes<T>()
	{
		if (m_typesByInterface.TryGetValue(typeof(T), out var value))
		{
			return value;
		}
		return new List<Type>();
	}

	private static void InitiateDictionary()
	{
		Type[] types = typeof(DynamicTypesManager).Assembly.GetTypes();
		List<Type> set = new List<Type> { typeof(BaseContext) };
		HashSet<Type> hashSet = new HashSet<Type> { typeof(ISingleMasterPacketAutoHandler) };
		Type[] array = types;
		foreach (Type type in array)
		{
			if (type.IsAbstract)
			{
				continue;
			}
			string name = type.Name;
			m_cachedTypesDictionary.GetOrSetDefault(name[0]).Add(new KeyValuePair<string, Type>(name, type));
			if (set.CheckBaseType(type))
			{
				m_dynamicTypesByName.GetOrSetDefault(name).Add(type);
			}
			Type[] interfaces = type.GetInterfaces();
			foreach (Type type2 in interfaces)
			{
				if (hashSet.Contains(type2))
				{
					m_typesByInterface.GetOrSetDefault(type2).Add(type);
				}
			}
		}
		array = typeof(Vector3).Assembly.GetTypes();
		foreach (Type type3 in array)
		{
			if (!type3.IsAbstract)
			{
				string name2 = type3.Name;
				m_cachedTypesDictionary.GetOrSetDefault(name2[0]).Add(new KeyValuePair<string, Type>(name2, type3));
			}
		}
	}

	private static V GetOrSetDefault<K, V>(this Dictionary<K, V> dictionary, K key)
	{
		if (dictionary.TryGetValue(key, out var value))
		{
			return value;
		}
		value = Activator.CreateInstance<V>();
		dictionary.Add(key, value);
		return value;
	}

	private static bool CheckBaseType(this List<Type> set, Type type)
	{
		foreach (Type item in set)
		{
			if (type.IsSubclassOf(item))
			{
				return true;
			}
		}
		return false;
	}

	public static FieldInfo[] GetCachedFields(this Type type, BindingFlags bindFlags)
	{
		Dictionary<int, FieldInfo[]> orSetDefault = m_fieldsCache.GetOrSetDefault(bindFlags);
		int hashCode = type.GetHashCode();
		if (orSetDefault.TryGetValue(hashCode, out var value))
		{
			return value;
		}
		return orSetDefault[hashCode] = type.GetFields(bindFlags);
	}

	public static PropertyInfo[] GetCachedProperties(this Type type, BindingFlags bindFlags)
	{
		Dictionary<int, PropertyInfo[]> orSetDefault = m_propertiesCache.GetOrSetDefault(bindFlags);
		int hashCode = type.GetHashCode();
		if (orSetDefault.TryGetValue(hashCode, out var value))
		{
			return value;
		}
		return orSetDefault[hashCode] = type.GetProperties(bindFlags);
	}

	public static bool GetCachedIsDefinedInherit(this MemberInfo type, Type attributeType)
	{
		int hashCode = attributeType.GetHashCode();
		int hashCode2 = type.GetHashCode();
		long key = (long)(((ulong)(uint)hashCode << 32) | (uint)hashCode2);
		if (!m_isDefinedAttrCache.TryGetValue(key, out var value))
		{
			value = type.IsDefined(attributeType, inherit: true);
			m_isDefinedAttrCache[key] = value;
			return value;
		}
		return value;
	}

	public static int GetCachedPropertyIndexParamsCount(PropertyInfo property)
	{
		Module module = property.Module;
		if (m_propertyModulesCache.TryGetValue(module, out var value))
		{
			int metadataToken = property.MetadataToken;
			if (value.TryGetValue(metadataToken, out var value2))
			{
				return value2;
			}
			value2 = property.GetIndexParameters().Length;
			m_propertyModulesCache[module][metadataToken] = value2;
			return value2;
		}
		m_propertyModulesCache[module] = new Dictionary<int, int>();
		int num = property.GetIndexParameters().Length;
		m_propertyModulesCache[module][property.MetadataToken] = num;
		return num;
	}

	public static MethodInfo[] GetCachedMethods(this Type type, BindingFlags bindFlags)
	{
		Dictionary<int, MethodInfo[]> orSetDefault = m_methodsCache.GetOrSetDefault(bindFlags);
		int hashCode = type.GetHashCode();
		if (orSetDefault.TryGetValue(hashCode, out var value))
		{
			return value;
		}
		return orSetDefault[hashCode] = type.GetMethods(bindFlags);
	}
}
