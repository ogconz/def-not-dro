using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CarModelSystem;
using CarModelSystem.Data;
using Custom;
using TriLib;
using UnityEngine;

public static class CarPartExternalLoader
{
	public class ExternalAssetRequest : IExternalAssetRequest
	{
		internal bool internalIsDone;

		internal GameObject internalReference;

		public bool isDone => internalIsDone;

		public GameObject reference => internalReference;
	}

	public const string ExternalPartRootNodeName = "external_part_root_node";

	private const string BundleExtension = "bundle";

	private static Dictionary<string, GameObject> m_objectPool = new Dictionary<string, GameObject>();

	private static HashSet<string> m_activeRequests = new HashSet<string>();

	private static HashSet<string> m_stopList = new HashSet<string>();

	private static HashSet<string> m_errorList = new HashSet<string>();

	private static AssetLoader m_trilibLoader = new AssetLoader();

	private static AssetLoaderAsync m_trilibAsyncLoader = new AssetLoaderAsync();

	private static AssetBundleLoader m_assetBundleLoader = new AssetBundleLoader();

	private static GameObject m_holder;

	private static GameObject m_loadPlace;

	private static GameObject m_parent;

	private static AssetLoaderOptions m_defaultOptions;

	public const string BodyColorMaterialNamePart = " - Paint";

	public const string GlassColorMaterialNamePart = " - Glass";

	public const string TailbrakeMaterialNamePart = " - tailbrake";

	private static readonly List<MeshRenderer> s_rendererList = new List<MeshRenderer>();

	private static readonly List<Material> s_materialList = new List<Material>();

	public static void Initialize()
	{
		m_defaultOptions = ScriptableObject.CreateInstance<AssetLoaderOptions>();
		m_defaultOptions.UseCutoutMaterials = false;
		m_parent = KeepAliveManager.instance.gameObject;
		ConstructHolder();
		CarModelConfig.ExternalLoader = LoadFromFile;
		CarModelConfig.ExternalLoaderAsync = AsyncLoadFromFile;
		SubscribeToChangeCarForCleanPool();
	}

	public static GameObject LoadFromFile(string fileName)
	{
		if (m_stopList.Contains(fileName))
		{
			m_stopList.Remove(fileName);
			m_activeRequests.Add(fileName);
		}
		if (m_activeRequests.Contains(fileName))
		{
			while (!m_objectPool.ContainsKey(fileName))
			{
			}
		}
		if (!m_objectPool.TryGetValue(fileName, out var value))
		{
			value = InternalLoadObject(fileName);
			value.name = "external_part_root_node" + GetFormatFromFileName(fileName);
			if (value.transform.childCount > 0)
			{
				value.transform.SetParent(m_holder.transform, worldPositionStays: false);
			}
			value.GetComponentsInChildren(s_rendererList);
			for (int i = 0; i < s_rendererList.Count; i++)
			{
				MeshRendererPostprocessing(s_rendererList[i]);
			}
			m_objectPool.Add(fileName, value);
		}
		return value;
	}

	public static void MeshRendererPostprocessing(MeshRenderer renderer)
	{
		GameObject gameObject = renderer.gameObject;
		string name = renderer.name;
		renderer.GetSharedMaterials(s_materialList);
		for (int i = 0; i < s_materialList.Count; i++)
		{
			string name2 = s_materialList[i].name;
			TypeID typeID = 0u;
			if (name2.IndexOf(" - Paint", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				typeID = new TypeID("body_nolivery");
			}
			else if (name2.IndexOf(" - Glass", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				typeID = new TypeID("glass");
			}
			else if (name.Equals("brakelight", StringComparison.OrdinalIgnoreCase) && name2.IndexOf(" - tailbrake", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				typeID = new TypeID("lights");
			}
			else if (name2.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
			{
				typeID = new TypeID("lights");
			}
			if (typeID != 0u)
			{
				MaterialPropertyBlockRecivier materialPropertyBlockRecivier = gameObject.AddComponent<MaterialPropertyBlockRecivier>();
				materialPropertyBlockRecivier.renderer = renderer;
				materialPropertyBlockRecivier.masterMaterialType = typeID;
				materialPropertyBlockRecivier.materialIndex = i;
			}
		}
	}

	public static IExternalAssetRequest AsyncLoadFromFile(string filename)
	{
		ExternalAssetRequest externalAssetRequest = new ExternalAssetRequest();
		TimeController.RunCoroutine(PreloadFromFile(filename, externalAssetRequest));
		return externalAssetRequest;
	}

	public static IEnumerator PreloadFromFile(string filename, ExternalAssetRequest request)
	{
		yield return PreloadFromFile(filename);
		request.internalReference = LoadFromFile(filename);
		request.internalIsDone = true;
	}

	public static IEnumerator PreloadFromFile(string fileName)
	{
		if (m_stopList.Contains(fileName))
		{
			m_stopList.Remove(fileName);
			m_activeRequests.Add(fileName);
		}
		if (m_activeRequests.Contains(fileName))
		{
			while (!m_objectPool.ContainsKey(fileName) && !m_errorList.Contains(fileName))
			{
				yield return null;
			}
		}
		else
		{
			if (m_objectPool.TryGetValue(fileName, out var obj))
			{
				yield break;
			}
			m_activeRequests.Add(fileName);
			yield return InternalLoadObjectAsync(fileName, delegate(GameObject result)
			{
				obj = result;
			});
			m_activeRequests.Remove(fileName);
			if (m_stopList.Contains(fileName))
			{
				m_stopList.Remove(fileName);
				UnityEngine.Object.DestroyImmediate(obj);
				yield break;
			}
			if (obj == null || obj.transform == null)
			{
				GameLog.Print(AlertLevel.FatalError, "MOD", "Failed to load from " + fileName);
				m_errorList.Add(fileName);
				yield break;
			}
			if (m_holder == null || m_holder.transform == null)
			{
				GameLog.Print(AlertLevel.FatalError, "MOD", "Holder lost for " + fileName);
				m_errorList.Add(fileName);
				yield break;
			}
			obj.name = "external_part_root_node" + GetFormatFromFileName(fileName);
			if (obj.transform.childCount > 0)
			{
				obj.transform.SetParent(m_holder.transform, worldPositionStays: false);
			}
			obj.GetComponentsInChildren(s_rendererList);
			for (int num = 0; num < s_rendererList.Count; num++)
			{
				MeshRendererPostprocessing(s_rendererList[num]);
			}
			m_objectPool.Add(fileName, obj);
		}
	}

	private static GameObject InternalLoadObject(string fileName)
	{
		if (GetFormatFromFileName(fileName).Contains("bundle"))
		{
			return m_assetBundleLoader.LoadFromFile(fileName, m_loadPlace);
		}
		return m_trilibLoader.LoadFromFile(fileName, m_defaultOptions, m_loadPlace);
	}

	private static IEnumerator InternalLoadObjectAsync(string fileName, Action<GameObject> callback)
	{
		bool completed;
		if (GetFormatFromFileName(fileName).Contains("bundle"))
		{
			completed = false;
			m_assetBundleLoader.LoadFromFileAsync(fileName, m_loadPlace, OnFileLoadedHandler);
			while (!completed)
			{
				yield return null;
			}
		}
		else
		{
			GameObject instance = m_trilibLoader.LoadFromFile(fileName, m_defaultOptions, m_loadPlace);
			SetupResult(instance);
		}
		void OnFileLoadedHandler(GameObject gameObject)
		{
			SetRotationForBakedAxis(gameObject);
			SetupResult(gameObject);
			completed = true;
		}
		void SetupResult(GameObject param)
		{
			callback.SafeCall(param);
		}
	}

	private static string GetFormatFromFileName(string fileName)
	{
		return Path.GetExtension(fileName).ToLower();
	}

	private static void SubscribeToChangeCarForCleanPool()
	{
		GameEvents.OnLastCarIdChanged.Subscribe(OnLastCarChangedHandler);
	}

	private static void OnLastCarChangedHandler(GameEvents.EventInfo eventInfo)
	{
		CleanUp();
	}

	public static void CleanUp()
	{
		foreach (string activeRequest in m_activeRequests)
		{
			m_stopList.Add(activeRequest);
		}
		m_activeRequests.Clear();
		m_objectPool.Clear();
		ConstructHolder();
		Resources.UnloadUnusedAssets();
	}

	private static void ConstructHolder()
	{
		if (m_holder != null)
		{
			UnityEngine.Object.DestroyImmediate(m_holder);
		}
		m_holder = new GameObject("CarPool");
		m_holder.transform.SetParent(m_parent.transform, worldPositionStays: false);
		m_holder.SetActive(value: false);
		m_loadPlace = new GameObject("Temp");
		m_loadPlace.transform.SetParent(m_holder.transform, worldPositionStays: false);
	}

	private static void SetRotationForBakedAxis(GameObject obj)
	{
		if (obj != null)
		{
			obj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
		}
	}
}
