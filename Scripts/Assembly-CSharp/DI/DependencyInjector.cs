using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DI
{
	public static class DependencyInjector
	{
		private static readonly Dictionary<Type, object> m_instancesMap = new Dictionary<Type, object>();

		private static readonly object m_placeholder = new object();

		public static void InjectObjectsInto(object target)
		{
			foreach (FieldInfo dependencyField in GetDependencyFields(target.GetType()))
			{
				dependencyField.SetValue(target, Resolve(dependencyField.FieldType));
			}
		}

		public static void MakeDependenciesInjected(this MonoBehaviour mb)
		{
			InjectObjectsInto(mb);
		}

		public static T Resolve<T>()
		{
			return (T)Resolve(typeof(T));
		}

		private static IEnumerable<FieldInfo> GetDependencyFields(Type type)
		{
			if (type == null)
			{
				return Enumerable.Empty<FieldInfo>();
			}
			BindingFlags bindFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			return (from f in type.GetCachedFields(bindFlags)
				where HasDependencyAttribute(f)
				select f).Union(GetDependencyFields(type.BaseType));
		}

		private static bool HasDependencyAttribute(MemberInfo member)
		{
			return member.IsDefined(typeof(DependencyAttribute), inherit: true);
		}

		private static object AddCreate(Type type)
		{
			m_instancesMap[type] = m_placeholder;
			m_instancesMap[type] = CreateInstance(type);
			return m_instancesMap[type];
		}

		private static object Resolve(Type type)
		{
			if (!m_instancesMap.TryGetValue(type, out var value))
			{
				return AddCreate(type);
			}
			if (m_placeholder == value)
			{
				throw new Exception("Cyclic dependency detected in " + type);
			}
			return value;
		}

		private static object CreateInstance(Type type)
		{
			if (type.IsSubclassOf(typeof(UnityEngine.Object)))
			{
				UnityEngine.Object obj = UnityEngine.Object.FindObjectOfType(type);
				GameLog.Print(AlertLevel.Notify, "Debug", "DI searches for the instance of the type " + type.Name + ". Result = " + obj);
				return obj;
			}
			GameLog.Print(AlertLevel.Notify, "Debug", "DI creates instance of the type " + type.Name);
			return Activator.CreateInstance(type);
		}

		public static void ClearTransient()
		{
			List<Type> list = new List<Type>();
			foreach (KeyValuePair<Type, object> item in m_instancesMap)
			{
				if (item.Value is ITransientDependency)
				{
					list.Add(item.Key);
				}
			}
			foreach (Type item2 in list)
			{
				m_instancesMap.Remove(item2);
			}
		}
	}
}
