using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace AssetBundleParser
{
	public class TrackAssetBundleParser
	{
		private static TrackAssetBundleParser m_instance;

		public static TrackAssetBundleParser instance
		{
			get
			{
				if (m_instance == null)
				{
					m_instance = new TrackAssetBundleParser();
				}
				return m_instance;
			}
		}

		public string TryGetSceneNameFromAssetBundle(AssetBundle assetBundle)
		{
			string[] allScenePaths = assetBundle.GetAllScenePaths();
			if (allScenePaths.Length == 0)
			{
				return null;
			}
			return Path.GetFileNameWithoutExtension(allScenePaths[0]);
		}

		public static string SceneParseName(string sceneName, ulong externalId)
		{
			try
			{
				string value = externalId.ToString();
				if (sceneName.IndexOf(value, StringComparison.Ordinal) != -1)
				{
					return sceneName.Substring(0, sceneName.IndexOf(value, StringComparison.Ordinal));
				}
				return new string(sceneName.Where(char.IsLetter).ToArray());
			}
			catch (Exception)
			{
				GameLog.Print(AlertLevel.Error, "ModMaps", externalId + " | Error Parse : " + sceneName);
			}
			return null;
		}
	}
}
