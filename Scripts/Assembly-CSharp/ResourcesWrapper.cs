#define PROD_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;

public static class ResourcesWrapper
{
	public const string GR_GLOBAL = "global";

	public const string GR_TEMP = "temp";

	public const string GR_CUSTOMIZATION = "customization";

	public const string GR_RACESTATE = "race_state";

	public const string GR_VINYL_EDITOR = "vinyl_editor";

	private static bool m_canLog = false;

	private static Dictionary<string, List<AsyncOperationHandle>> m_groups = new Dictionary<string, List<AsyncOperationHandle>>();

	[Conditional("PROD_BUILD")]
	private static void EnableLoggingOnProd()
	{
		m_canLog = true;
	}

	public static void Initialize()
	{
		EnableLoggingOnProd();
		if (m_canLog)
		{
			GameLog.Print(AlertLevel.Notify, "Addressables", "ResourcesWrapper > Initialize");
		}
		Addressables.InitializeAsync().WaitForCompletion();
	}

	public static IEnumerator InitializeAsync()
	{
		if (m_canLog)
		{
			GameLog.Print(AlertLevel.Notify, "Addressables", "ResourcesWrapper > InitializeAsync");
		}
		yield return Addressables.InitializeAsync();
	}

	public static T LoadLegacy<T>(string path) where T : UnityEngine.Object
	{
		T val = Resources.LoadAsync<T>(path).asset as T;
		if (m_canLog)
		{
			if (val == null)
			{
				GameLog.Print(AlertLevel.Error, "Addressables", "ResourcesWrapper> LoadLegacy<" + typeof(T).Name + "> - " + path + " - not found!");
			}
			else
			{
				GameLog.Print(AlertLevel.Notify, "Addressables", "ResourcesWrapper> LoadLegacy<" + typeof(T).Name + "> - " + path, val);
			}
		}
		return val;
	}

	public static T Load<T>(string path, string group = "") where T : UnityEngine.Object
	{
		if (m_canLog)
		{
			GameLog.Print(AlertLevel.Notify, "Addressables", "ResourcesWrapper> Load<" + typeof(T).Name + "> - " + path);
		}
		AsyncOperationHandle<T> asyncOperationHandle = LoadAssetAsync<T>(path, group);
		T result = null;
		if (asyncOperationHandle.Status != AsyncOperationStatus.Failed)
		{
			return asyncOperationHandle.WaitForCompletion();
		}
		if (m_canLog)
		{
			UnityEngine.Debug.LogException(asyncOperationHandle.OperationException);
		}
		return result;
	}

	public static IEnumerator LoadOut<T>(string path, Action<AsyncOperationHandle<T>> onDone, string group = "") where T : UnityEngine.Object
	{
		if (m_canLog)
		{
			GameLog.Print(AlertLevel.Notify, "Addressables", "ResourcesWrapper> Load<" + typeof(T).Name + "> - " + path);
		}
		AsyncOperationHandle<T> aoh = LoadAssetAsync<T>(path, group);
		yield return aoh;
		if (aoh.Status == AsyncOperationStatus.Failed)
		{
			if (m_canLog)
			{
				UnityEngine.Debug.LogException(aoh.OperationException);
			}
			onDone?.Invoke(default(AsyncOperationHandle<T>));
		}
		else
		{
			onDone?.Invoke(aoh);
		}
	}

	public static bool ContainsAsset(string path)
	{
		return GetResourceLocation(path) != null;
	}

	public static IResourceLocation GetResourceLocation(string path)
	{
		return Addressables.LoadAssetAsync<IResourceLocation>(path).WaitForCompletion();
	}

	public static AsyncOperationHandle<GameObject> InstantiateAsync(string path, Vector3 position, Quaternion rotation, Transform parent = null)
	{
		return Addressables.InstantiateAsync(path, position, rotation, parent);
	}

	public static GameObject Instantiate(string path, Vector3 position, Quaternion rotation, Transform parent = null)
	{
		return Addressables.InstantiateAsync(path, position, rotation, parent).WaitForCompletion();
	}

	public static T[] LoadAllLegacy<T>(string path) where T : UnityEngine.Object
	{
		if (m_canLog)
		{
			GameLog.Print(AlertLevel.Notify, "Addressables", "ResourcesWrapper> LoadAllLegasy<" + typeof(T).Name + "> - " + path);
		}
		return Resources.LoadAll<T>(path);
	}

	public static IList<T> LoadAll<T>(string path) where T : UnityEngine.Object
	{
		if (m_canLog)
		{
			GameLog.Print(AlertLevel.Notify, "Addressables", "ResourcesWrapper> LoadAll<" + typeof(T).Name + "> - " + path);
		}
		return Addressables.LoadAssetsAsync<T>(path, null).WaitForCompletion();
	}

	public static AsyncOperationHandle<T> LoadAsync<T>(string path, Action<T> callback = null, string group = "") where T : UnityEngine.Object
	{
		if (m_canLog)
		{
			GameLog.Print(AlertLevel.Notify, "Addressables", "ResourcesWrapper> LoadAsync<" + typeof(T).Name + "> - " + path);
		}
		AsyncOperationHandle<T> result = LoadAssetAsync<T>(path, group);
		if (callback != null)
		{
			result.Completed += delegate(AsyncOperationHandle<T> asyncOperationHandle)
			{
				callback?.Invoke(asyncOperationHandle.Result);
			};
		}
		return result;
	}

	public static async UniTask<T> LoadUniTaskAsync<T>(string path, CancellationToken cancellationToken) where T : UnityEngine.Object
	{
		return await Addressables.LoadAssetAsync<T>(path).WithCancellation(cancellationToken);
	}

	private static AsyncOperationHandle<T> LoadAssetAsync<T>(string path, string group) where T : UnityEngine.Object
	{
		AsyncOperationHandle<T> asyncOperationHandle = Addressables.LoadAssetAsync<T>(path);
		if (!string.IsNullOrEmpty(group))
		{
			if (!m_groups.TryGetValue(group, out var value))
			{
				value = new List<AsyncOperationHandle>();
				m_groups.Add(group, value);
			}
			value.Add(asyncOperationHandle);
		}
		return asyncOperationHandle;
	}

	public static void UnloadAsset(UnityEngine.Object asset)
	{
		if (!(asset == null))
		{
			if (m_canLog)
			{
				GameLog.Print(AlertLevel.Notify, "Addressables", $"ResourcesWrapper> UnloadAsset - {asset}");
			}
			if (asset is GameObject instance)
			{
				Addressables.ReleaseInstance(instance);
			}
			else
			{
				Addressables.Release(asset);
			}
		}
	}

	public static void UnloadAsset(AsyncOperationHandle handle)
	{
		if (m_canLog)
		{
			GameLog.Print(AlertLevel.Notify, "Addressables", $"ResourcesWrapper> UnloadAsset - {handle}");
		}
		Release(handle);
	}

	private static void Release(AsyncOperationHandle handle)
	{
		if (handle.IsValid())
		{
			Addressables.Release(handle);
		}
	}

	public static void UnloadResources(string groupName)
	{
		if (m_groups.TryGetValue(groupName, out var value))
		{
			value.ForEach(Release);
			value.Clear();
		}
	}

	public static IEnumerator UnloadUnusedAssets()
	{
		if (m_canLog)
		{
			GameLog.Print(AlertLevel.Notify, "Addressables", "UnloadUnusedAssets");
		}
		UnloadResources("temp");
		UnloadResources("race_state");
		UnloadResources("vinyl_editor");
		yield return Resources.UnloadUnusedAssets();
	}

	public static void LoadScene(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single)
	{
		if (m_canLog)
		{
			GameLog.Print(AlertLevel.Notify, "Addressables", $"ResourcesWrapper> LoadScene - {sceneName} - {loadMode}");
		}
		Addressables.LoadSceneAsync(sceneName + ".unity", loadMode).WaitForCompletion();
	}

	public static IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single)
	{
		if (m_canLog)
		{
			GameLog.Print(AlertLevel.Notify, "Addressables", $"ResourcesWrapper> LoadSceneAsync - {sceneName} - {loadMode}");
		}
		yield return Addressables.LoadSceneAsync(sceneName + ".unity", loadMode);
	}
}
