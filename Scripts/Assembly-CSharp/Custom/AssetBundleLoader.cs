using System;
using System.IO;
using UnityEngine;

namespace Custom
{
	public class AssetBundleLoader
	{
		public GameObject LoadFromFile(string path, GameObject wrapper)
		{
			AssetBundle bundle = SafeLoadBundle(path);
			GameObject result = null;
			if (IsBundleValid(bundle) && TryGetAssetNameFromBundle(bundle, out var result2))
			{
				GameObject reference = SafeLoadAssetFromBundle(bundle, result2);
				result = SafeInstantiateObject(reference, wrapper);
			}
			SafeUnloadBundle(bundle);
			return result;
		}

		public void LoadFromFileAsync(string path, GameObject wrapper, Action<GameObject> callback)
		{
			SafeLoadBundleAsync(path, LoadAssetFromBundleAsync);
			void LoadAssetFromBundleAsync(AssetBundle bundle)
			{
				if (IsBundleValid(bundle) && TryGetAssetNameFromBundle(bundle, out var result))
				{
					SafeLoadAssetFromBundleAsync(bundle, result, InstantiateObjectFromBundle);
				}
				else
				{
					InvokeCallbackAndReleaseBundle(null);
				}
				void InstantiateObjectFromBundle(GameObject reference)
				{
					GameObject instance = SafeInstantiateObject(reference, wrapper);
					InvokeCallbackAndReleaseBundle(instance);
				}
				void InvokeCallbackAndReleaseBundle(GameObject instance)
				{
					callback.SafeCall(instance);
					SafeUnloadBundle(bundle);
				}
			}
		}

		private AssetBundle SafeLoadBundle(string path)
		{
			if (!IsPathValid(path))
			{
				GameLog.Print(AlertLevel.Error, "AssetBundleLoader", "Path to asset bundle is invalid: " + path);
				return null;
			}
			try
			{
				return AssetBundle.LoadFromFile(path);
			}
			catch (Exception arg)
			{
				GameLog.Print(AlertLevel.Error, "AssetBundleLoader", $"Loading asset bundle failed: {arg}");
				return null;
			}
		}

		private void SafeLoadBundleAsync(string path, Action<AssetBundle> callback)
		{
			if (!IsPathValid(path))
			{
				GameLog.Print(AlertLevel.Error, "AssetBundleLoader", "Path to asset bundle is invalid: " + path);
				callback.SafeCall(null);
				return;
			}
			AssetBundleCreateRequest request;
			try
			{
				request = AssetBundle.LoadFromFileAsync(path);
				request.completed += OnBundleLoadedHandler;
			}
			catch (Exception arg)
			{
				GameLog.Print(AlertLevel.Error, "AssetBundleLoader", $"Loading asset bundle failed: {arg}");
				callback.SafeCall(null);
			}
			void OnBundleLoadedHandler(AsyncOperation asyncOperation)
			{
				request.completed -= OnBundleLoadedHandler;
				callback.SafeCall(request.assetBundle);
			}
		}

		private void SafeUnloadBundle(AssetBundle bundle)
		{
			if (IsBundleValid(bundle))
			{
				bundle.Unload(unloadAllLoadedObjects: false);
			}
		}

		private GameObject SafeLoadAssetFromBundle(AssetBundle bundle, string assetName)
		{
			try
			{
				return bundle.LoadAsset<GameObject>(assetName);
			}
			catch (Exception arg)
			{
				GameLog.Print(AlertLevel.Error, "AssetBundleLoader", $"Loading asset bundle failed: {arg}");
				return null;
			}
		}

		private void SafeLoadAssetFromBundleAsync(AssetBundle bundle, string assetName, Action<GameObject> callback)
		{
			AssetBundleRequest request;
			try
			{
				request = bundle.LoadAssetAsync<GameObject>(assetName);
				request.completed += OnAssetLoadedHandler;
			}
			catch (Exception arg)
			{
				GameLog.Print(AlertLevel.Error, "AssetBundleLoader", $"Loading asset from bundle failed: {arg}");
				callback.SafeCall(null);
			}
			void OnAssetLoadedHandler(AsyncOperation asyncOperation)
			{
				request.completed -= OnAssetLoadedHandler;
				callback.SafeCall(request.asset as GameObject);
			}
		}

		private GameObject SafeInstantiateObject(GameObject reference, GameObject wrapper)
		{
			if (reference == null)
			{
				GameLog.Print(AlertLevel.Error, "AssetBundleLoader", "Instantiate failed: Reference is null");
				return null;
			}
			try
			{
				return UnityEngine.Object.Instantiate(reference, wrapper.transform);
			}
			catch (Exception arg)
			{
				GameLog.Print(AlertLevel.Error, "AssetBundleLoader", $"Loading asset bundle failed: {arg}");
				return null;
			}
		}

		private bool TryGetAssetNameFromBundle(AssetBundle bundle, out string result)
		{
			string[] allAssetNames = bundle.GetAllAssetNames();
			if (allAssetNames.Length != 0)
			{
				result = allAssetNames[0];
				return true;
			}
			GameLog.Print(AlertLevel.Error, "AssetBundleLoader", "Asset bundle doesn't contain any assets");
			result = null;
			return false;
		}

		private bool IsPathValid(string path)
		{
			if (path != null)
			{
				return File.Exists(path);
			}
			return false;
		}

		private bool IsBundleValid(AssetBundle bundle)
		{
			return bundle != null;
		}
	}
}
