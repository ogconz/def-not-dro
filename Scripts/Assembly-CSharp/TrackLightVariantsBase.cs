using System.Collections.Generic;
using UnityEngine;

public class TrackLightVariantsBase : ScriptableSingleton<TrackLightVariantsBase>
{
	[SerializeField]
	private LightVariants[] m_lightVariants;

	[SerializeField]
	private TrackLightVariants[] m_trackLightVariants;

	[SerializeField]
	private TrackSceneProbePosition[] m_reflectionPosition;

	[SerializeField]
	private TrackSceneToLightScene[] m_sceneToLightScenes;

	[SerializeField]
	private DefaultVariantForLightScene[] m_defaultVariantForLightScenes;

	private const string NIGHT_POSTFIX = "_night";

	private Dictionary<string, int> m_previousLightVariants = new Dictionary<string, int>();

	public string GetLightVariantForScene(string track)
	{
		string text = null;
		TrackLightVariants[] trackLightVariants = m_trackLightVariants;
		foreach (TrackLightVariants trackLightVariants2 in trackLightVariants)
		{
			if (!(trackLightVariants2.track == track))
			{
				continue;
			}
			int[] lightVariants = trackLightVariants2.lightVariants;
			if (lightVariants == null || lightVariants.Length == 0)
			{
				break;
			}
			int num = lightVariants[Random.Range(0, lightVariants.Length)];
			if (!m_previousLightVariants.ContainsKey(track))
			{
				m_previousLightVariants[track] = num;
			}
			else
			{
				if (lightVariants.Length > 1)
				{
					while (m_previousLightVariants[track] == num)
					{
						num = lightVariants[Random.Range(0, lightVariants.Length)];
					}
				}
				m_previousLightVariants[track] = num;
			}
			text = GetLightVariantByID(num);
			break;
		}
		return text ?? track;
	}

	public string[] GetAllLightVariantForScene(string track)
	{
		List<string> list = new List<string>();
		TrackLightVariants[] trackLightVariants = m_trackLightVariants;
		foreach (TrackLightVariants trackLightVariants2 in trackLightVariants)
		{
			if (!(trackLightVariants2.track == track))
			{
				continue;
			}
			int[] lightVariants = trackLightVariants2.lightVariants;
			if (lightVariants != null && lightVariants.Length != 0)
			{
				int[] array = lightVariants;
				foreach (int id in array)
				{
					list.Add(GetLightVariantByID(id));
				}
			}
			break;
		}
		return list.ToArray();
	}

	public string GetLightSceneNameOfScene(string sceneName)
	{
		TrackSceneToLightScene[] sceneToLightScenes = m_sceneToLightScenes;
		foreach (TrackSceneToLightScene trackSceneToLightScene in sceneToLightScenes)
		{
			if (trackSceneToLightScene.scene == sceneName || trackSceneToLightScene.sceneName == sceneName)
			{
				return trackSceneToLightScene.lightScene;
			}
		}
		return string.Empty;
	}

	public string GetVisibleNameForLightPreset(string variantName)
	{
		LightVariants[] lightVariants = m_lightVariants;
		foreach (LightVariants lightVariants2 in lightVariants)
		{
			if (lightVariants2.variantName == variantName)
			{
				return lightVariants2.visibleName;
			}
		}
		return string.Empty;
	}

	public bool IsDefaultPresetForLightScene(string lightSceneName, string lightPreset)
	{
		DefaultVariantForLightScene[] defaultVariantForLightScenes = m_defaultVariantForLightScenes;
		foreach (DefaultVariantForLightScene defaultVariantForLightScene in defaultVariantForLightScenes)
		{
			if (defaultVariantForLightScene.lightScene == lightSceneName)
			{
				return defaultVariantForLightScene.lightVariant == lightPreset;
			}
		}
		return false;
	}

	public string GetDefaultPresetForLightScene(string lightSceneName)
	{
		DefaultVariantForLightScene[] defaultVariantForLightScenes = m_defaultVariantForLightScenes;
		foreach (DefaultVariantForLightScene defaultVariantForLightScene in defaultVariantForLightScenes)
		{
			if (defaultVariantForLightScene.lightScene == lightSceneName)
			{
				return defaultVariantForLightScene.lightVariant;
			}
		}
		return string.Empty;
	}

	public Vector3 GetProbePositionOnTrack(string track)
	{
		TrackSceneProbePosition[] reflectionPosition = m_reflectionPosition;
		foreach (TrackSceneProbePosition trackSceneProbePosition in reflectionPosition)
		{
			if (trackSceneProbePosition.scene == track)
			{
				return trackSceneProbePosition.probePosition;
			}
		}
		return Vector3.zero;
	}

	private string GetLightVariantByID(int id)
	{
		LightVariants[] lightVariants = m_lightVariants;
		foreach (LightVariants lightVariants2 in lightVariants)
		{
			if (lightVariants2.id == id)
			{
				return lightVariants2.variantName;
			}
		}
		return string.Empty;
	}

	public string GetSceneName(string trackScene, bool isNightMode)
	{
		return GetLightSceneNameOfScene(trackScene) + (isNightMode ? "_night" : string.Empty);
	}
}
