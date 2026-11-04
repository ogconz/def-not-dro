using System;
using System.Collections;
using ArrayExtension;
using DI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class LightVariantController : MonoBehaviour
{
	public static LightVariantController instance;

	private const string LIGHT_PROFILES_PATH = "Assets/Art/Internal/EnvironmentProfiles/";

	private const string PREFAB_PATTERN = "{0}.prefab";

	private const string LIGHT_CUBEMAP_PATH = "Assets/Scenes/Tracks/AllBakedReflections/";

	private const string CUBEMAP_EXT = ".exr";

	private const string LIGHT_SCENE_PREFIX = "vp_";

	private const string BACK_SLASH = "\\";

	private const string SLASH = "/";

	public bool skipLightChange;

	private GamePrefs m_prefs;

	private Coroutine m_changeLightCoroutine;

	private AsyncOperationHandle m_lightHandle;

	public Action onProbeUpdated;

	public HDProbe reflectionProbe;

	private AsyncOperationHandle m_cubeHandle;

	private void Awake()
	{
		instance = this;
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		if (m_lightHandle.IsValid())
		{
			ResourcesWrapper.UnloadAsset(m_lightHandle);
		}
		if (m_cubeHandle.IsValid())
		{
			ResourcesWrapper.UnloadAsset(m_cubeHandle);
		}
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
	{
		string text = scene.name;
		if (loadMode == LoadSceneMode.Additive && text.Contains("vp_"))
		{
			ChangeLightPreset(text);
		}
	}

	private void ChangeLightPreset(string sceneName)
	{
		if (skipLightChange)
		{
			skipLightChange = false;
			return;
		}
		string presetName = m_prefs.globals.lightPresetName;
		if (m_prefs.globals.sceneName != sceneName)
		{
			presetName = ScriptableSingleton<TrackLightVariantsBase>.instance.GetLightVariantForScene(sceneName);
		}
		m_prefs.globals.sceneName = sceneName;
		StartChangeLightCoroutine(presetName);
	}

	public void StartChangeLightCoroutine(string presetName)
	{
		StopChangeLightCoroutineIfNeed();
		m_changeLightCoroutine = TimeController.StartNow(ChangeLight(presetName));
	}

	private void StopChangeLightCoroutineIfNeed()
	{
		if (m_changeLightCoroutine != null)
		{
			TimeController.StopNow(m_changeLightCoroutine);
			m_changeLightCoroutine = null;
		}
	}

	private IEnumerator ChangeLight(string presetName)
	{
		if (string.IsNullOrEmpty(presetName) || presetName == "null")
		{
			StopChangeLightCoroutineIfNeed();
			yield break;
		}
		while (!DayNightController.instance.isReady)
		{
			yield return null;
		}
		string sceneName = m_prefs.globals.sceneName;
		Scene scene = SceneManager.GetSceneByName(sceneName);
		string text = "Assets/Art/Internal/EnvironmentProfiles/" + presetName + "/" + $"{presetName}.prefab";
		if (text.Contains(presetName))
		{
			if (text.Contains("\\"))
			{
				text = text.Replace("\\", "/");
			}
			if (m_lightHandle.IsValid())
			{
				ResourcesWrapper.UnloadAsset(m_lightHandle);
			}
			GameObject presetGO = null;
			yield return ResourcesWrapper.LoadOut(text, delegate(AsyncOperationHandle<GameObject> aohHandle)
			{
				presetGO = aohHandle.Result;
				m_lightHandle = aohHandle;
			});
			if (presetGO != null)
			{
				m_prefs.globals.lightPresetName = presetName;
				Volume oldVolume = null;
				if (!scene.isLoaded || !scene.IsValid())
				{
					StopChangeLightCoroutineIfNeed();
					yield break;
				}
				scene.GetRootGameObjects().Find(delegate(GameObject o)
				{
					Volume component2 = o.GetComponent<Volume>();
					if ((bool)component2)
					{
						oldVolume = component2;
						return true;
					}
					return false;
				});
				Volume component = presetGO.GetComponent<Volume>();
				if (component == null || oldVolume == null)
				{
					StopChangeLightCoroutineIfNeed();
					yield break;
				}
				oldVolume.profile = component.profile;
				oldVolume.sharedProfile = component.sharedProfile;
				Light componentInChildren = presetGO.GetComponentInChildren<Light>();
				Light componentInChildren2 = oldVolume.GetComponentInChildren<Light>();
				HDAdditionalLightData componentInChildren3 = presetGO.GetComponentInChildren<HDAdditionalLightData>();
				HDAdditionalLightData componentInChildren4 = oldVolume.GetComponentInChildren<HDAdditionalLightData>();
				if (componentInChildren3 == null || componentInChildren4 == null || componentInChildren == null || componentInChildren2 == null)
				{
					StopChangeLightCoroutineIfNeed();
					yield break;
				}
				componentInChildren4.transform.rotation = componentInChildren3.transform.rotation;
				componentInChildren3.CopyTo(componentInChildren4);
				componentInChildren2.color = componentInChildren.color;
				componentInChildren2.colorTemperature = componentInChildren.colorTemperature;
				componentInChildren2.intensity = componentInChildren.intensity;
				if (m_cubeHandle.IsValid())
				{
					ResourcesWrapper.UnloadAsset(m_cubeHandle);
				}
				Cubemap cube = null;
				yield return ResourcesWrapper.LoadOut("Assets/Scenes/Tracks/AllBakedReflections/" + sceneName + "/" + presetName + ".exr", delegate(AsyncOperationHandle<Cubemap> aohHandle)
				{
					cube = aohHandle.Result;
					m_cubeHandle = aohHandle;
				});
				if (cube == null)
				{
					StopChangeLightCoroutineIfNeed();
					yield break;
				}
				if (!scene.isLoaded || !scene.IsValid())
				{
					StopChangeLightCoroutineIfNeed();
					yield break;
				}
				HDProbe probe = null;
				scene.GetRootGameObjects().Find(delegate(GameObject o)
				{
					HDProbe component2 = o.GetComponent<HDProbe>();
					if ((bool)component2)
					{
						probe = component2;
						return true;
					}
					return false;
				});
				if (probe == null)
				{
					StopChangeLightCoroutineIfNeed();
					yield break;
				}
				probe.SetTexture(ProbeSettings.Mode.Custom, cube);
				reflectionProbe = probe;
				onProbeUpdated.SafeCall();
			}
		}
		StopChangeLightCoroutineIfNeed();
	}
}
