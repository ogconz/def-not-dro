using System.Diagnostics;
using UnityEngine;

public class DebugUtils
{
	[Conditional("UNITY_EDITOR")]
	public static void Assert(bool condition)
	{
	}

	[Conditional("UNITY_EDITOR")]
	public static void Assert(bool condition, string assertString)
	{
	}

	[Conditional("UNITY_EDITOR")]
	public static void Assert(bool condition, string assertString, bool pauseOnFail)
	{
		if (!condition)
		{
			UnityEngine.Debug.LogError("assert failed! " + assertString);
			if (pauseOnFail)
			{
				UnityEngine.Debug.Break();
			}
		}
	}
}
