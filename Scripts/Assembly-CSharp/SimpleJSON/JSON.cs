using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GameOverlay;
using UnityEngine;

namespace SimpleJSON
{
	public static class JSON
	{
		private const string DATETIME_FORMAT_STRING = "yyyy-MM-dd HH:mm:ss.ffff";

		public const string NULL = "null";

		private static Dictionary<Type, object> m_cachedDefaultValues = new Dictionary<Type, object>(128);

		private static List<JSONCache> m_cachePool = new List<JSONCache>();

		public static object TryParseNodeObject(string aJSON, string nodeName, Type type, bool logError)
		{
			object result = null;
			JSONCache jSONCache = GetJSONCache();
			JSONNode jSONNode = TryParse(aJSON, logError, jSONCache);
			if (jSONNode != null && jSONNode.HasKey(nodeName))
			{
				result = TryParseFromNode(jSONNode[nodeName], type, logError: true);
			}
			FreeJSONCache(jSONCache);
			return result;
		}

		public static string TryParseNodeValue(string aJSON, string nodeName, bool logError)
		{
			string result = null;
			JSONCache jSONCache = GetJSONCache();
			JSONNode jSONNode = TryParse(aJSON, logError, jSONCache);
			if (jSONNode != null && jSONNode.HasKey(nodeName))
			{
				result = jSONNode[nodeName].Value;
			}
			FreeJSONCache(jSONCache);
			return result;
		}

		private static JSONNode TryParse(string aJSON, bool logError, JSONCache cache)
		{
			try
			{
				return JSONNode.Parse(aJSON, cache);
			}
			catch (Exception ex)
			{
				if (logError)
				{
					GameLog.Print(AlertLevel.Error, "JSON", "Could not parse json: " + ex.ToString());
				}
				return null;
			}
		}

		public static T Parse<T>(string aJSON, bool skipErrors = false)
		{
			if (aJSON == null)
			{
				return default(T);
			}
			JSONCache jSONCache = GetJSONCache();
			object valueFromNode = GetValueFromNode(JSONNode.Parse(aJSON, jSONCache), typeof(T), skipErrors);
			FreeJSONCache(jSONCache);
			if (!(valueFromNode is T))
			{
				return default(T);
			}
			return (T)valueFromNode;
		}

		public static IEnumerator PrewarmCache(int classCount, int arrayCount, int dataCount, int yieldStep = 500)
		{
			JSONCache cache = GetJSONCache();
			int toYield = 0;
			for (int i = 0; i < classCount; i++)
			{
				cache.AllocateJSONClass();
				toYield++;
				if (toYield >= yieldStep)
				{
					toYield = 0;
					yield return null;
				}
			}
			for (int i = 0; i < arrayCount; i++)
			{
				cache.AllocateJSONArray();
				toYield++;
				if (toYield >= yieldStep)
				{
					toYield = 0;
					yield return null;
				}
			}
			for (int i = 0; i < dataCount; i++)
			{
				cache.AllocateJSONData(0);
				toYield++;
				if (toYield >= yieldStep)
				{
					toYield = 0;
					yield return null;
				}
			}
			FreeJSONCache(cache);
		}

		private static JSONCache GetJSONCache()
		{
			lock (m_cachePool)
			{
				JSONCache jSONCache = null;
				for (int i = 0; i < m_cachePool.Count; i++)
				{
					JSONCache jSONCache2 = m_cachePool[i];
					if (!jSONCache2.inUse)
					{
						jSONCache = jSONCache2;
						break;
					}
				}
				if (jSONCache == null)
				{
					jSONCache = new JSONCache();
					m_cachePool.Add(jSONCache);
				}
				jSONCache.Init();
				return jSONCache;
			}
		}

		private static void FreeJSONCache(JSONCache cache)
		{
			lock (m_cachePool)
			{
				cache.Free();
				if (m_cachePool.Count > 1)
				{
					bool flag = false;
					int num = m_cachePool[0].poolSize;
					for (int i = 1; i < m_cachePool.Count; i++)
					{
						int poolSize = m_cachePool[i].poolSize;
						if (poolSize > num)
						{
							flag = true;
							break;
						}
						num = poolSize;
					}
					if (flag)
					{
						m_cachePool.Sort((JSONCache a, JSONCache b) => b.poolSize.CompareTo(a.poolSize));
					}
				}
				cache = null;
			}
		}

		public static void Parse<T>(string aJSON, T obj, bool skipErrors = false) where T : class
		{
			if (obj != null)
			{
				JSONCache jSONCache = GetJSONCache();
				GetObjectFromNode(JSONNode.Parse(aJSON, jSONCache), obj, skipErrors);
				FreeJSONCache(jSONCache);
			}
		}

		private static object TryParseFromNode(JSONNode json, Type toType, bool logError = false)
		{
			if (json == null)
			{
				return null;
			}
			try
			{
				return GetValueFromNode(json, toType);
			}
			catch (Exception ex)
			{
				if (logError)
				{
					GameLog.Print(AlertLevel.Error, "JSON", "Could not parse json: " + ex.ToString());
				}
				return null;
			}
		}

		private static bool TryParse(string json, Type toType, out object result, bool logError = false)
		{
			if (json == null)
			{
				result = null;
				return false;
			}
			try
			{
				JSONCache jSONCache = GetJSONCache();
				JSONNode fromNode = JSONNode.Parse(json, jSONCache);
				result = GetValueFromNode(fromNode, toType);
				FreeJSONCache(jSONCache);
				return true;
			}
			catch (Exception ex)
			{
				if (logError)
				{
					GameLog.Print(AlertLevel.Error, "JSON", "Could not parse json: " + ex.ToString());
				}
				result = null;
				return false;
			}
		}

		public static bool TryParse<T>(string aJSON, out T result, bool logError = false)
		{
			object result2 = null;
			bool result3 = TryParse(aJSON, typeof(T), out result2, logError);
			result = (T)result2;
			return result3;
		}

		public static T ParseAndSkipErrors<T>(string aJSON)
		{
			try
			{
				JSONCache jSONCache = GetJSONCache();
				T result = (T)GetValueFromNode(JSONNode.Parse(aJSON, jSONCache), typeof(T), skipErrors: true);
				FreeJSONCache(jSONCache);
				return result;
			}
			catch
			{
				return default(T);
			}
		}

		public static string Serialize(object obj)
		{
			JSONCache jSONCache = GetJSONCache();
			JSONNode nodeFromValue = GetNodeFromValue(obj, jSONCache);
			string result = ((nodeFromValue == null) ? string.Empty : nodeFromValue.ToString());
			FreeJSONCache(jSONCache);
			return result;
		}

		public static string Format(string json)
		{
			if (!string.IsNullOrEmpty(json))
			{
				return new JsonFormatter(json).Format();
			}
			return json;
		}

		private static bool TryGetValueFromNode(JSONNode fromNode, Type withType, bool skipErrors, ref object result)
		{
			try
			{
				result = GetValueFromNode(fromNode, withType, skipErrors);
				return true;
			}
			catch (Exception ex)
			{
				if (skipErrors)
				{
					Debug.LogWarning("[JSON error (ignored)] " + ex.Message);
					Exception ex2 = null;
					return false;
				}
				throw;
			}
		}

		private static void GetObjectFromNode(JSONNode fromNode, object result, bool skipErrors)
		{
			if (fromNode == null)
			{
				return;
			}
			bool flag = fromNode.IsNullValue();
			Type type = result.GetType();
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				if (flag)
				{
					return;
				}
				type = type.GetGenericArguments()[0];
			}
			if (flag)
			{
				return;
			}
			JSONClass jSONClass = fromNode as JSONClass;
			if (jSONClass == null)
			{
				throw new Exception("Type mismatch - expected a class node but found " + fromNode.ToString());
			}
			FieldInfo[] cachedFields = type.GetCachedFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in cachedFields)
			{
				if (jSONClass.HasKey(fieldInfo.Name))
				{
					object result2 = null;
					if (TryGetValueFromNode(jSONClass[fieldInfo.Name], fieldInfo.FieldType, skipErrors, ref result2))
					{
						fieldInfo.SetValue(result, result2);
					}
				}
			}
			PropertyInfo[] cachedProperties = type.GetCachedProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (PropertyInfo propertyInfo in cachedProperties)
			{
				if (propertyInfo.CanRead && propertyInfo.CanWrite && jSONClass.HasKey(propertyInfo.Name) && DynamicTypesManager.GetCachedPropertyIndexParamsCount(propertyInfo) <= 0)
				{
					object result3 = null;
					if (TryGetValueFromNode(jSONClass[propertyInfo.Name], propertyInfo.PropertyType, skipErrors, ref result3))
					{
						propertyInfo.SetValue(result, result3, null);
					}
				}
			}
		}

		private static object GetValueFromNode(JSONNode fromNode, Type withType, bool skipErrors = false)
		{
			if (fromNode == null)
			{
				return null;
			}
			bool flag = fromNode.IsNullValue();
			Type type = (withType.IsGenericType ? withType.GetGenericTypeDefinition() : null);
			if (type == typeof(Nullable<>))
			{
				if (flag)
				{
					return null;
				}
				withType = withType.GetGenericArguments()[0];
			}
			bool flag2 = withType == typeof(bool);
			bool flag3 = withType == typeof(byte);
			bool flag4 = withType == typeof(int);
			bool flag5 = withType == typeof(long);
			bool flag6 = withType == typeof(ulong);
			bool flag7 = withType == typeof(float);
			bool flag8 = withType == typeof(double);
			bool flag9 = withType == typeof(string);
			bool flag10 = withType == typeof(DateTime);
			bool isArray = withType.IsArray;
			bool flag11 = type == typeof(List<>);
			bool flag12 = type == typeof(HashSet<>);
			bool isEnum = withType.IsEnum;
			if (!(fromNode is JSONData) && (flag2 || flag4 || flag3 || flag7 || flag8 || flag9 || flag10))
			{
				throw new Exception("Type mismatch - expected '" + withType.Name + "' but found " + fromNode.ToString());
			}
			if (!(fromNode is JSONArray) && (isArray || flag11 || flag12))
			{
				throw new Exception("Type mismatch - expected an array but found " + fromNode.ToString());
			}
			if (flag2)
			{
				return fromNode.AsBool;
			}
			if (flag3)
			{
				return (byte)fromNode.AsInt;
			}
			if (flag4)
			{
				return fromNode.AsInt;
			}
			if (flag5)
			{
				return long.Parse(fromNode.Value);
			}
			if (flag6)
			{
				return ulong.Parse(fromNode.Value);
			}
			if (flag7)
			{
				return fromNode.AsFloat;
			}
			if (flag8)
			{
				return fromNode.AsDouble;
			}
			if (flag9)
			{
				return fromNode.Value;
			}
			if (flag10)
			{
				return ParseDateTime(fromNode.Value);
			}
			if (isEnum && fromNode is JSONData)
			{
				if (!Enum.IsDefined(withType, fromNode.Value))
				{
					if (int.TryParse(fromNode.Value, out var result))
					{
						return Enum.ToObject(withType, result);
					}
					try
					{
						return Enum.Parse(withType, fromNode.Value);
					}
					catch
					{
						Array values = Enum.GetValues(withType);
						if (values.Length == 0)
						{
							throw new Exception("Enum '" + withType.Name + "' doesn't contain any constants");
						}
						return values.GetValue(0);
					}
				}
				return Enum.Parse(withType, fromNode.Value);
			}
			bool flag13 = false;
			if (withType == typeof(CarVisualManager.VisualSettings))
			{
				foreach (string key in fromNode.AsObject.Keys)
				{
					if (key == "bodyColor")
					{
						flag13 = true;
						break;
					}
				}
			}
			if (isArray)
			{
				JSONArray asArray = fromNode.AsArray;
				int num = 0;
				Array array = Array.CreateInstance(withType.GetElementType(), asArray.Count);
				{
					foreach (JSONNode child in asArray.Children)
					{
						object result2 = null;
						if (TryGetValueFromNode(child, withType.GetElementType(), skipErrors, ref result2))
						{
							array.SetValue(result2, num++);
						}
					}
					return array;
				}
			}
			if (flag11 || flag12)
			{
				JSONArray asArray2 = fromNode.AsArray;
				Type withType2 = withType.GetGenericArguments()[0];
				MethodInfo method = withType.GetMethod("Add");
				object obj2 = Activator.CreateInstance(withType);
				{
					foreach (JSONNode child2 in asArray2.Children)
					{
						object result3 = null;
						if (TryGetValueFromNode(child2, withType2, skipErrors, ref result3))
						{
							method.Invoke(obj2, new object[1] { result3 });
						}
					}
					return obj2;
				}
			}
			if (flag)
			{
				return null;
			}
			if (withType == typeof(PlayerId) && PlayerId.TryParse(fromNode.Value, out var playerId))
			{
				return playerId;
			}
			JSONClass jSONClass = fromNode as JSONClass;
			if (jSONClass == null)
			{
				if (!string.IsNullOrEmpty(fromNode.Value))
				{
					ConstructorInfo[] constructors = withType.GetConstructors();
					foreach (ConstructorInfo constructorInfo in constructors)
					{
						ParameterInfo[] parameters = constructorInfo.GetParameters();
						if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
						{
							return constructorInfo.Invoke(new object[1] { fromNode.Value });
						}
					}
				}
				throw new Exception("Type mismatch - expected a class node but found " + fromNode.ToString());
			}
			if (type == typeof(Dictionary<, >))
			{
				Type withType3 = withType.GetGenericArguments()[0];
				Type withType4 = withType.GetGenericArguments()[1];
				MethodInfo method2 = withType.GetMethod("Add");
				MethodInfo method3 = withType.GetMethod("ContainsKey");
				object obj3 = Activator.CreateInstance(withType);
				{
					foreach (string key2 in jSONClass.Keys)
					{
						JSONData jSONData = new JSONData();
						jSONData.Set(key2);
						JSONNode fromNode2 = jSONClass[key2];
						object result4 = null;
						if (!TryGetValueFromNode(jSONData, withType3, skipErrors, ref result4))
						{
							continue;
						}
						object result5 = null;
						if (TryGetValueFromNode(fromNode2, withType4, skipErrors, ref result5))
						{
							object obj4 = method3.Invoke(obj3, new object[1] { result4 });
							if (!(obj4 is bool) || !(bool)obj4)
							{
								method2.Invoke(obj3, new object[2] { result4, result5 });
							}
						}
					}
					return obj3;
				}
			}
			object obj5 = Activator.CreateInstance(withType);
			FieldInfo[] cachedFields = withType.GetCachedFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in cachedFields)
			{
				if (jSONClass.HasKey(fieldInfo.Name))
				{
					object result6 = null;
					if (TryGetValueFromNode(jSONClass[fieldInfo.Name], fieldInfo.FieldType, skipErrors, ref result6))
					{
						fieldInfo.SetValue(obj5, result6);
					}
				}
			}
			if (flag13 && withType == typeof(CarVisualManager.VisualSettings) && obj5 is CarVisualManager.VisualSettings visualSettings)
			{
				visualSettings.oldColorsFormat = true;
			}
			PropertyInfo[] cachedProperties = withType.GetCachedProperties(BindingFlags.Instance | BindingFlags.Public);
			int j = 0;
			for (int num2 = cachedProperties.Length; j < num2; j++)
			{
				PropertyInfo propertyInfo = cachedProperties[j];
				if (propertyInfo.CanRead && propertyInfo.CanWrite && jSONClass.HasKey(propertyInfo.Name) && DynamicTypesManager.GetCachedPropertyIndexParamsCount(propertyInfo) <= 0)
				{
					object result7 = null;
					if (TryGetValueFromNode(jSONClass[propertyInfo.Name], propertyInfo.PropertyType, skipErrors, ref result7))
					{
						propertyInfo.SetValue(obj5, result7, null);
					}
				}
			}
			return obj5;
		}

		private static JSONNode GetNodeFromValue(object obj, JSONCache cache)
		{
			if (obj == null)
			{
				return cache.AllocateJSONData("null");
			}
			Type type = obj.GetType();
			if (type.GetCachedIsDefinedInherit(typeof(JsonIgnoreAttribute)))
			{
				return cache.AllocateJSONData("null");
			}
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				PropertyInfo property = type.GetProperty("Value");
				MethodInfo methodInfo = ((property == null) ? null : property.GetGetMethod());
				obj = ((methodInfo == null) ? null : methodInfo.Invoke(obj, null));
				if (obj == null)
				{
					return cache.AllocateJSONData("null");
				}
				type = obj.GetType();
			}
			if (obj is int)
			{
				return cache.AllocateJSONData(JSONNode.FastIntToString((int)obj));
			}
			if (obj is float num)
			{
				if (num == 0f)
				{
					return cache.AllocateJSONData("0");
				}
				if (num == 1f)
				{
					return cache.AllocateJSONData("1");
				}
				return cache.AllocateJSONData(num.ToString());
			}
			if (obj is double num2)
			{
				if (num2 == 0.0)
				{
					return cache.AllocateJSONData("0");
				}
				if (num2 == 1.0)
				{
					return cache.AllocateJSONData("1");
				}
				return cache.AllocateJSONData(num2.ToString());
			}
			if (obj is bool || obj is byte || obj is long || obj is ulong || obj is string || type.IsEnum)
			{
				return cache.AllocateJSONData(obj.ToString());
			}
			if (obj is DateTime)
			{
				return cache.AllocateJSONData(((DateTime)obj).ToString("yyyy-MM-dd HH:mm:ss.ffff"));
			}
			if (obj is Guid)
			{
				return cache.AllocateJSONData(obj.ToString());
			}
			if (obj is Array)
			{
				Array array = (Array)obj;
				JSONArray jSONArray = cache.AllocateJSONArray();
				for (int i = 0; i < array.Length; i++)
				{
					jSONArray.Add(GetNodeFromValue(array.GetValue(i), cache));
				}
				return jSONArray;
			}
			Type type2 = (type.IsGenericType ? type.GetGenericTypeDefinition() : null);
			if (type2 == typeof(List<>))
			{
				IList obj2 = (IList)obj;
				JSONArray jSONArray2 = cache.AllocateJSONArray();
				{
					foreach (object item in obj2)
					{
						jSONArray2.Add(GetNodeFromValue(item, cache));
					}
					return jSONArray2;
				}
			}
			if (type2 == typeof(HashSet<>))
			{
				IEnumerable obj3 = (IEnumerable)obj;
				JSONArray jSONArray3 = cache.AllocateJSONArray();
				{
					foreach (object item2 in obj3)
					{
						jSONArray3.Add(GetNodeFromValue(item2, cache));
					}
					return jSONArray3;
				}
			}
			if (type2 == typeof(Dictionary<, >))
			{
				JSONClass jSONClass = cache.AllocateJSONClass();
				IDictionary dictionary = (IDictionary)obj;
				{
					foreach (object key in dictionary.Keys)
					{
						string aKey = key.ToString();
						JSONNode nodeFromValue = GetNodeFromValue(dictionary[key], cache);
						jSONClass.Add(aKey, nodeFromValue);
					}
					return jSONClass;
				}
			}
			JSONClass jSONClass2 = cache.AllocateJSONClass();
			FieldInfo[] cachedFields = type.GetCachedFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in cachedFields)
			{
				if (fieldInfo.IsPrivate)
				{
					if (!fieldInfo.GetCachedIsDefinedInherit(typeof(JsonSerializeAttribute)))
					{
						continue;
					}
				}
				else if (fieldInfo.GetCachedIsDefinedInherit(typeof(JsonIgnoreAttribute)))
				{
					continue;
				}
				object value = fieldInfo.GetValue(obj);
				if (value != GetDefaultValue(fieldInfo.FieldType))
				{
					jSONClass2.Add(fieldInfo.Name, GetNodeFromValue(value, cache));
				}
			}
			PropertyInfo[] cachedProperties = type.GetCachedProperties(BindingFlags.Instance | BindingFlags.Public);
			int k = 0;
			for (int num3 = cachedProperties.Length; k < num3; k++)
			{
				PropertyInfo propertyInfo = cachedProperties[k];
				if (propertyInfo.CanRead && propertyInfo.CanWrite && DynamicTypesManager.GetCachedPropertyIndexParamsCount(propertyInfo) <= 0 && !propertyInfo.GetCachedIsDefinedInherit(typeof(JsonIgnoreAttribute)))
				{
					PropertyAction(cache, jSONClass2, obj, propertyInfo);
				}
			}
			cachedProperties = type.GetCachedProperties(BindingFlags.Instance | BindingFlags.NonPublic);
			foreach (PropertyInfo propertyInfo2 in cachedProperties)
			{
				if (propertyInfo2.GetCachedIsDefinedInherit(typeof(JsonSerializeAttribute)))
				{
					PropertyAction(cache, jSONClass2, obj, propertyInfo2);
				}
			}
			return jSONClass2;
		}

		private static void PropertyAction(JSONCache cache, JSONClass result, object obj, PropertyInfo property)
		{
			if (property.CanRead && property.CanWrite && DynamicTypesManager.GetCachedPropertyIndexParamsCount(property) <= 0)
			{
				object value = property.GetValue(obj, null);
				if (value != GetDefaultValue(property.PropertyType))
				{
					result.Add(property.Name, GetNodeFromValue(value, cache));
				}
			}
		}

		private static object GetDefaultValue(Type type)
		{
			object value = null;
			if (type.IsValueType)
			{
				if (m_cachedDefaultValues.TryGetValue(type, out value))
				{
					return value;
				}
				value = Activator.CreateInstance(type);
				m_cachedDefaultValues[type] = value;
			}
			return value;
		}

		private static List<Type> GetAllTypesByName(string className)
		{
			return DynamicTypesManager.GetAssemblyTypes(className);
		}

		private static DateTime ParseDateTime(string s)
		{
			try
			{
				return DateTime.ParseExact(s, "yyyy-MM-dd HH:mm:ss.ffff", null);
			}
			catch
			{
				return DateTime.Parse(s);
			}
		}
	}
}
