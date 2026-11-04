using System;
using System.Collections.Generic;
using System.IO;
using Sentry;
using Sentry.Unity;
using UnityEngine;

namespace Services.GameAnalytics.Sentry.EventHandlers
{
	public class LoadPrefsErrorEventHandler : IEventHandler
	{
		public void Process(string eventName, Dictionary<string, object> parameters)
		{
			Exception exception = (Exception)parameters["error"];
			string prefsFilePath = (string)parameters["prefs_file_path"];
			byte[] prefsFileData = (byte[])parameters["prefs_file_data"];
			SentrySdk.CaptureException(exception, ConfigureScope);
			void ConfigureScope(Scope scope)
			{
				AddEventTag(scope);
				if (!SafeAddAttachmentPath(scope, prefsFilePath))
				{
					SafeAddAttachmentData(scope, prefsFileData, prefsFilePath);
				}
				string persistentDataPath = Application.persistentDataPath;
				SafeAddAttachmentPath(scope, Path.Combine(persistentDataPath, "Player.log"));
				SafeAddAttachmentPath(scope, Path.Combine(persistentDataPath, "Player-prev.log"));
			}
		}

		private void AddEventTag(Scope scope)
		{
			scope.SetTag("event", "load_prefs_error");
		}

		private bool SafeAddAttachmentPath(Scope scope, string path)
		{
			try
			{
				if (IsPathValid(path))
				{
					scope.AddAttachment(path);
					return true;
				}
			}
			catch (Exception arg)
			{
				Debug.LogWarning($"Sentry: Failed to add attachment by path: {arg}");
			}
			return false;
		}

		private bool SafeAddAttachmentData(Scope scope, byte[] data, string fileName)
		{
			try
			{
				scope.AddAttachment(data, fileName);
				return true;
			}
			catch (Exception arg)
			{
				Debug.LogWarning($"Sentry: Failed to add attachment by data: {arg}");
			}
			return false;
		}

		private bool IsPathValid(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return false;
			}
			if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1)
			{
				return false;
			}
			return File.Exists(path);
		}
	}
}
