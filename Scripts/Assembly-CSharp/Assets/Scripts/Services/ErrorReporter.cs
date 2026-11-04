using System;
using UnityEngine;

namespace Assets.Scripts.Services
{
	public static class ErrorReporter
	{
		public static void ReportPrefsError(Exception exc)
		{
			try
			{
				Debug.LogException(exc);
			}
			catch (Exception)
			{
			}
		}
	}
}
