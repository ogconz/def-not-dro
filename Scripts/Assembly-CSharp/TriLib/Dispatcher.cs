using System;
using System.Collections.Generic;
using UnityEngine;

namespace TriLib
{
	public class Dispatcher : MonoBehaviour
	{
		private static Dispatcher _instance;

		private static bool _instanceExists;

		private static readonly object LockObject = new object();

		private static readonly Queue<Action> Actions = new Queue<Action>();

		public static void CheckInstance()
		{
			if (!_instanceExists)
			{
				new GameObject("Dispatcher").AddComponent<Dispatcher>();
				_instanceExists = true;
			}
		}

		public static void InvokeAsync(Action action)
		{
			if (!_instanceExists)
			{
				Debug.LogError("No Dispatcher exists in the scene. Actions will not be invoked!");
				return;
			}
			lock (LockObject)
			{
				Actions.Enqueue(action);
			}
		}

		private void Awake()
		{
			if ((bool)_instance)
			{
				UnityEngine.Object.DestroyImmediate(this);
				return;
			}
			_instance = this;
			_instanceExists = true;
		}

		private void OnDestroy()
		{
			if (_instance == this)
			{
				_instance = null;
				_instanceExists = false;
			}
		}

		private void Update()
		{
			lock (LockObject)
			{
				while (Actions.Count > 0)
				{
					Actions.Dequeue()();
				}
			}
		}
	}
}
