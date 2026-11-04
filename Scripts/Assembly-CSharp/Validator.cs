using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class Validator
{
	public abstract class MethodStringInvoker<T>
	{
		protected void Invoke(string method)
		{
			try
			{
				typeof(T).GetMethod(method, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public)?.Invoke(this, null);
			}
			catch (Exception ex)
			{
				GameLog.Print(AlertLevel.Error, "ModMaps", ex?.ToString() + "| Method : " + method);
			}
		}

		public abstract void InvokeCommand(GameObject data);
	}

	private static void Command<T, U>(this MethodStringInvoker<T> invoker, Transform root) where U : Component
	{
		for (int i = 0; i < root.childCount; i++)
		{
			Transform child = root.GetChild(i);
			U component = child.GetComponent<U>();
			if (component != null)
			{
				invoker.InvokeCommand(component.gameObject);
			}
			invoker.Command<T, U>(child);
		}
	}

	public static void CommandNoRecursive<T, U>(this MethodStringInvoker<T> invoker, ICollection<U> roots) where U : Component
	{
		foreach (U root in roots)
		{
			U component = root.GetComponent<U>();
			if (component != null)
			{
				invoker.InvokeCommand(component.gameObject);
			}
		}
	}

	public static void Command<T, U>(this MethodStringInvoker<T> invoker, ICollection<U> roots) where U : Component
	{
		foreach (U root in roots)
		{
			invoker.Command<T, U>(root.transform);
		}
	}
}
