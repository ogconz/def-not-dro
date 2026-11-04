using System;
using System.Collections;
using UnityEngine;

namespace Xbox
{
	public class XboxConnectedStorage : IXboxConnectedStorage
	{
		public string prefix { get; private set; }

		public XboxConnectedStorage(string storageName)
		{
		}

		public (bool, byte[] data) Read(string modelName)
		{
			byte[] array = null;
			if (!modelName.StartsWith(prefix))
			{
				modelName = prefix + modelName;
			}
			if (PlayerPrefs.HasKey(modelName))
			{
				array = Convert.FromBase64String(PlayerPrefs.GetString(modelName, string.Empty));
			}
			return (array != null, data: array);
		}

		public void Dispose()
		{
		}

		public IEnumerator Submit()
		{
			PlayerPrefs.Save();
			yield break;
		}

		public IEnumerator Clear(string key)
		{
			PlayerPrefs.DeleteAll();
			yield return Submit();
		}

		public bool Write(string modelName, byte[] data)
		{
			if (data != null)
			{
				if (!modelName.StartsWith(prefix))
				{
					modelName = prefix + modelName;
				}
				string value = Convert.ToBase64String(data);
				PlayerPrefs.SetString(modelName, value);
				return true;
			}
			return false;
		}
	}
}
