using System.Globalization;
using System.Threading;
using UnityEngine;

public static class FixCultureRuntime
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void FixCulture()
	{
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
	}
}
