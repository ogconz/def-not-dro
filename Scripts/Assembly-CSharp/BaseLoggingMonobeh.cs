using System;
using UnityEngine;

public abstract class BaseLoggingMonobeh : MonoBehaviour
{
	protected virtual void AwakeHandler()
	{
	}

	protected virtual void StartHandler()
	{
	}

	private void Awake()
	{
		CallMethodWithLogging("awake", AwakeHandler);
	}

	private void Start()
	{
		CallMethodWithLogging("start", StartHandler);
	}

	protected void CallMethodWithLogging(string methodName, Action method)
	{
		GameLog.Check(AlertLevel.Notify, "Debug", GetType().Name + " invoke " + methodName, method.TrySafeCall());
	}
}
