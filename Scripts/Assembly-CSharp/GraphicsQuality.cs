using System;
using System.Collections;
using System.Collections.ObjectModel;
using DI;
using GameOverlay;
using HDRP;
using HDRP.Scalability;
using PrefsModels;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[AddComponentMenu("Scripts/Custom/Misc/GraphicsQuality")]
public class GraphicsQuality : BaseLoggingMonobeh
{
	private struct QualitySnapshot
	{
		public int bloom;

		public int textureQuality;

		public int antiAliasing;

		public int anisoLevel;

		public int volumetricFog;

		public int ao;

		public int lod;

		public int ssgi;

		public int ssr;
	}

	public enum AllowedWindowModeOptions
	{
		Windowed = 0,
		ExclusiveFullScreen = 1,
		FullScreenWindow = 2
	}

	public static NightSettings nightSettings = new NightSettings();

	private GraphicSettings m_settings;

	private const int m_maxLevel = 3;

	private const int m_maxAntiAliasing = 3;

	private const int m_maxVolumetricFog = 1;

	private const int m_maxAo = 3;

	private const int m_maxShadows = 3;

	private const int m_maxMotionBlur = 3;

	private const int m_maxBloom = 3;

	private const int m_maxLod = 3;

	private const int m_maxTextureQuality = 3;

	private const int m_maxAnisoLevel = 3;

	private const int m_maxSsgi = 1;

	private const int m_maxSSRLevel = 1;

	private const int m_maxSkidmarkLength = 3;

	private QualitySnapshot m_lastQualitySnapshot;

	public Action onSkidmarkChange;

	public Action onLightPowerChange;

	protected bool m_resStateInvalid;

	public Action<int> OnSmokeLevelChange;

	public Action<bool> OnVinylCompressionModeChange;

	public GraphicSettings settings => m_settings ?? (m_settings = DependencyInjector.Resolve<GamePrefs>().graphics);

	public int antiAliasing
	{
		get
		{
			return settings.antiAliasing;
		}
		set
		{
			Apply<AntiAliasingComponent>(settings.antiAliasing, value, 3, delegate(int i)
			{
				settings.antiAliasing = i;
			});
		}
	}

	public int volumetricFog
	{
		get
		{
			return settings.volumetricFog;
		}
		set
		{
			Apply<Fog>(settings.volumetricFog, value, 1, delegate(int i)
			{
				settings.volumetricFog = i;
			});
		}
	}

	public int ao
	{
		get
		{
			return settings.ao;
		}
		set
		{
			Apply<ScreenSpaceAmbientOcclusion>(settings.ao, value, 3, delegate(int i)
			{
				settings.ao = i;
			});
		}
	}

	public int shadows
	{
		get
		{
			return settings.shadows;
		}
		set
		{
			Apply<HDShadowSettings>(settings.shadows, 3, value, delegate(int i)
			{
				settings.shadows = i;
			});
		}
	}

	public int motionBlur
	{
		get
		{
			return settings.motionBlur;
		}
		set
		{
			Apply<MotionBlur>(settings.motionBlur, 3, value, delegate(int i)
			{
				settings.motionBlur = i;
			});
		}
	}

	public int bloom
	{
		get
		{
			return settings.bloom;
		}
		set
		{
			Apply<Bloom>(settings.bloom, value, 3, delegate(int i)
			{
				settings.bloom = i;
			});
		}
	}

	public int lod
	{
		get
		{
			return settings.lod;
		}
		set
		{
			Apply<LODBiasComponent>(settings.lod, value, 3, delegate(int i)
			{
				settings.lod = i;
			});
		}
	}

	public int textureQuality
	{
		get
		{
			return settings.textureQuality;
		}
		set
		{
			Apply<TextureQualityComponent>(settings.textureQuality, value, 3, delegate(int i)
			{
				settings.textureQuality = i;
			});
		}
	}

	public int anisoLevel
	{
		get
		{
			return settings.anisoLevel;
		}
		set
		{
			Apply<AnisotropicLevelComponent>(settings.anisoLevel, 3, value, delegate(int i)
			{
				settings.anisoLevel = i;
			});
		}
	}

	public int ssrLevel
	{
		get
		{
			return settings.ssrLevel;
		}
		set
		{
			Apply<ScreenSpaceReflection>(settings.ssrLevel, 1, value, delegate(int i)
			{
				settings.ssrLevel = i;
			});
		}
	}

	public int vSync
	{
		get
		{
			return settings.vSync;
		}
		set
		{
			SetValue(settings.vSync, value, delegate(int i)
			{
				settings.vSync = i;
				ApplyVSync();
			});
		}
	}

	public float lightPlayerPower
	{
		get
		{
			return settings.lightPlayerPower;
		}
		set
		{
			SetValue(settings.lightPlayerPower, value, delegate(float i)
			{
				settings.lightPlayerPower = i;
				ApplyLightPower();
			});
		}
	}

	public float lightOpponentPower
	{
		get
		{
			return settings.lightOpponentPower;
		}
		set
		{
			SetValue(settings.lightOpponentPower, value, delegate(float i)
			{
				settings.lightOpponentPower = i;
				ApplyLightPower();
			});
		}
	}

	public int skidmarkLength
	{
		get
		{
			return settings.skidmarkLength;
		}
		set
		{
			SetValue(settings.skidmarkLength, value, delegate(int i)
			{
				settings.skidmarkLength = i;
				ApplySkidmarkLength();
			});
		}
	}

	public int ssgi
	{
		get
		{
			return settings.ssgi;
		}
		set
		{
			Apply<GlobalIllumination>(settings.ssgi, 1, value, delegate(int i)
			{
				settings.ssgi = i;
			});
		}
	}

	public int smokeLevel
	{
		get
		{
			return settings.smokeLevel;
		}
		set
		{
			SetValue(settings.smokeLevel, value, delegate(int i)
			{
				settings.smokeLevel = i;
				ApplySmokeLevel();
			});
		}
	}

	public bool smokeEnabled
	{
		get
		{
			return smokeLevel > 0;
		}
		set
		{
			smokeLevel = (value ? 3 : 0);
		}
	}

	public float opponentSmokeOpacity
	{
		get
		{
			return settings.opponentSmokeOpacity;
		}
		set
		{
			SetValue(settings.opponentSmokeOpacity, value, delegate(float i)
			{
				settings.opponentSmokeOpacity = i;
				ApplySmokeLevel();
			});
		}
	}

	public float playerSmokeOpacity
	{
		get
		{
			return settings.playerSmokeOpacity;
		}
		set
		{
			SetValue(settings.playerSmokeOpacity, value, delegate(float i)
			{
				settings.playerSmokeOpacity = i;
				ApplySmokeLevel();
			});
		}
	}

	public float opponentLightOpacity
	{
		get
		{
			return settings.lightOpponentPower;
		}
		set
		{
			SetValue(settings.lightOpponentPower, value, delegate(float i)
			{
				settings.lightOpponentPower = i;
				ApplyLightPower();
			});
		}
	}

	public float playerLightOpacity
	{
		get
		{
			return settings.lightPlayerPower;
		}
		set
		{
			SetValue(settings.lightPlayerPower, value, delegate(float i)
			{
				settings.lightPlayerPower = i;
				ApplyLightPower();
			});
		}
	}

	public bool compressVinyls
	{
		get
		{
			return settings.compressVinyls == 1;
		}
		set
		{
			SetValue(settings.compressVinyls, value ? 1 : 0, delegate(int i)
			{
				settings.compressVinyls = i;
				ApplyVinylCompression();
			});
		}
	}

	public int fullscreen
	{
		get
		{
			return settings.fullscreen;
		}
		set
		{
			SetValue(settings.fullscreen, value, delegate(int i)
			{
				settings.fullscreen = i;
				m_resStateInvalid = true;
			});
		}
	}

	public ReadOnlyCollection<GraphicSettings.ResolutionInfo> supportedResolutionList => settings.supportedResolutionsList;

	public GraphicSettings.ResolutionInfo resolution
	{
		get
		{
			return settings.resolution;
		}
		set
		{
			if (settings.resolution.width != value.width || settings.resolution.height != value.height)
			{
				settings.resolution = value;
				m_resStateInvalid = true;
			}
		}
	}

	private static bool SetValue<T>(T origValue, T newValue, Action<T> onSet)
	{
		if (!origValue.Equals(newValue))
		{
			onSet?.Invoke(newValue);
			return true;
		}
		return false;
	}

	private void ApplySmokeLevel()
	{
		OnSmokeLevelChange.SafeCall(smokeLevel);
	}

	private void ApplyVinylCompression()
	{
		OnVinylCompressionModeChange?.Invoke(compressVinyls);
	}

	private string IndexToId(int index)
	{
		return index switch
		{
			0 => "0", 
			1 => "1", 
			2 => "2", 
			3 => "3", 
			_ => null, 
		};
	}

	private void Apply<T>(int origValue, int newValue, int max, Action<int> onSet) where T : VolumeComponent
	{
		newValue = Mathf.Clamp(newValue, 0, max);
		if (origValue != newValue)
		{
			onSet?.Invoke(newValue);
			ApplyComponent<T>(IndexToId(newValue));
		}
	}

	private VolumeComponent ApplyComponent<T>(string id) where T : VolumeComponent
	{
		T component = null;
		if (!string.IsNullOrEmpty(id))
		{
			Resources.Load<VolumeProfile>("QualityProfiles/QualityProfile_" + id).TryGet<T>(out component);
		}
		MasterVolumes.OverrideComponent(component);
		return component;
	}

	private void ApplyVSync()
	{
		ApplyComponent<VSyncComponent>(IndexToId(vSync));
	}

	private void ApplyNightSettings(int id)
	{
		string text = IndexToId(id);
		if (!string.IsNullOrEmpty(text) && Resources.Load<VolumeProfile>("QualityProfiles/QualityProfile_" + text).TryGet<NightSettingsComponent>(out var component))
		{
			nightSettings = component.value.value;
		}
	}

	private void ApplySkidmarkLength()
	{
		onSkidmarkChange.SafeCall();
	}

	private void ApplyLightPower()
	{
		onLightPowerChange.SafeCall();
	}

	private void ApplyScreenResolution()
	{
		m_resStateInvalid = false;
		if (!Application.isEditor)
		{
			GraphicSettings.ResolutionInfo resolutionInfo = settings.resolution;
			Screen.SetResolution(resolutionInfo.width, resolutionInfo.height, (FullScreenMode)fullscreen);
		}
	}

	public void ApplySettings()
	{
		ApplyComponent<AntiAliasingComponent>(IndexToId(settings.antiAliasing));
		ApplyComponent<Fog>(IndexToId(settings.volumetricFog));
		ApplyComponent<ScreenSpaceAmbientOcclusion>(IndexToId(settings.ao));
		ApplyComponent<HDShadowSettings>(IndexToId(settings.shadows));
		ApplyComponent<MotionBlur>(IndexToId(settings.motionBlur));
		ApplyComponent<Bloom>(IndexToId(settings.bloom));
		ApplyComponent<LODBiasComponent>(IndexToId(settings.lod));
		ApplyComponent<TextureQualityComponent>(IndexToId(settings.textureQuality));
		ApplyComponent<AnisotropicLevelComponent>(IndexToId(settings.anisoLevel));
		ApplyComponent<GlobalIllumination>(IndexToId(settings.ssgi));
		ApplyComponent<ScreenSpaceReflection>(IndexToId(settings.ssrLevel));
		ApplySmokeLevel();
		ApplyVSync();
		ApplySkidmarkLength();
		ApplyLightPower();
		ApplyNightSettings(Mathf.RoundToInt(GetOverallLevel() * 3f));
	}

	public void SetOverallLevel(float f)
	{
		antiAliasing = Mathf.RoundToInt(f * 3f);
		volumetricFog = Mathf.RoundToInt(f * 1f);
		ao = Mathf.RoundToInt(f * 3f);
		shadows = Mathf.RoundToInt(f * 3f);
		motionBlur = Mathf.RoundToInt(f * 3f);
		bloom = Mathf.RoundToInt(f * 3f);
		lod = Mathf.RoundToInt(f * 3f);
		textureQuality = Mathf.RoundToInt(f * 3f);
		anisoLevel = Mathf.RoundToInt(f * 3f);
		ssgi = Mathf.RoundToInt(f * 1f);
	}

	public void SetLastSnapShotQualityLevel()
	{
		antiAliasing = m_lastQualitySnapshot.antiAliasing;
		volumetricFog = m_lastQualitySnapshot.volumetricFog;
		bloom = m_lastQualitySnapshot.bloom;
		textureQuality = m_lastQualitySnapshot.textureQuality;
		anisoLevel = m_lastQualitySnapshot.anisoLevel;
		lod = m_lastQualitySnapshot.lod;
		ao = m_lastQualitySnapshot.ao;
		ssgi = m_lastQualitySnapshot.ssgi;
		ssrLevel = m_lastQualitySnapshot.ssr;
	}

	public void SetQualityLevel(float level)
	{
		antiAliasing = Mathf.RoundToInt(level * 3f);
		volumetricFog = Mathf.RoundToInt(level * 1f);
		ao = Mathf.RoundToInt(level * 3f);
		shadows = Mathf.RoundToInt(level * 3f);
		motionBlur = Mathf.RoundToInt(level * 3f);
		bloom = Mathf.RoundToInt(level * 3f);
		lod = Mathf.RoundToInt(level * 3f);
		textureQuality = Mathf.RoundToInt(level * 3f);
		anisoLevel = Mathf.RoundToInt(level * 3f);
		ssgi = Mathf.RoundToInt(level * 1f);
	}

	public void SetSnapShotQualityLevel(float level)
	{
		m_lastQualitySnapshot = new QualitySnapshot
		{
			antiAliasing = antiAliasing,
			volumetricFog = volumetricFog,
			bloom = bloom,
			textureQuality = textureQuality,
			anisoLevel = anisoLevel,
			lod = lod,
			ao = ao,
			ssgi = ssgi,
			ssr = ssrLevel
		};
		antiAliasing = Mathf.RoundToInt(level * 3f);
		volumetricFog = 0;
		bloom = Mathf.RoundToInt(level * 3f);
		textureQuality = Mathf.RoundToInt(level * 3f);
		anisoLevel = Mathf.RoundToInt(level * 3f);
		lod = Mathf.RoundToInt(level * 3f);
		ao = Mathf.RoundToInt(level * 3f);
		ssgi = 0;
		ssrLevel = 0;
	}

	public float GetOverallLevel()
	{
		return (0f + (float)antiAliasing / 3f + (float)volumetricFog / 1f + (float)ao / 3f + (float)shadows / 3f + (float)motionBlur / 3f + (float)bloom / 3f + (float)lod / 3f + (float)textureQuality / 3f + (float)anisoLevel / 3f) * (1f / 9f);
	}

	private void ApplyDefaultParams()
	{
		antiAliasing = 2;
		volumetricFog = 0;
		ao = 2;
		shadows = 2;
		motionBlur = 2;
		bloom = 2;
		lod = 2;
		textureQuality = 2;
		anisoLevel = 2;
		lightPlayerPower = 1f;
		lightOpponentPower = 1f;
		ssgi = 0;
		ssrLevel = 0;
		skidmarkLength = 1;
	}

	protected override void AwakeHandler()
	{
		Overlay.OnReady = (Action)Delegate.Combine(Overlay.OnReady, new Action(OnOverlayReady));
	}

	private void OnOverlayReady()
	{
		if ((bool)DependencyInjector.Resolve<GamePrefs>().stats.isFirstPlay)
		{
			ApplyDefaultParams();
		}
		else
		{
			ApplySettings();
		}
	}

	private IEnumerator Start()
	{
		yield return null;
		m_resStateInvalid = true;
		GameConsole.SetCmd("r_vsync", delegate
		{
			if (GameConsole.cmdArgs.Length == 2)
			{
				int result = 0;
				if (int.TryParse(GameConsole.cmdArgs[1], out result))
				{
					vSync = result;
					GameConsole.Print("r_vsync set to " + result);
				}
			}
			else
			{
				GameConsole.Print("r_vsync is " + QualitySettings.vSyncCount);
			}
		});
		GameConsole.SetCmd("r_resolution", delegate
		{
			if (GameConsole.cmdArgs.Length < 2)
			{
				GameConsole.Print("Resolution list: ");
				int num = 0;
				string text = "";
				string text2 = "";
				foreach (GraphicSettings.ResolutionInfo supportedResolutions in settings.supportedResolutionsList)
				{
					text2 = supportedResolutions.width + "x" + supportedResolutions.height;
					text = text + "<" + num + "> " + text2 + ";    ";
					num++;
					if (num % 5 == 0)
					{
						GameConsole.Print(text);
						text = "";
					}
				}
				if (!string.IsNullOrEmpty(text))
				{
					GameConsole.Print(text);
					text = "";
				}
			}
			else
			{
				int result = 0;
				if (!int.TryParse(GameConsole.cmdArgs[1], out result))
				{
					GameConsole.Print("Invalid resolution index");
				}
				else
				{
					GraphicSettings.ResolutionInfo resolutionInfo = settings.supportedResolutionsList[result];
					settings.resolution = new GraphicSettings.ResolutionInfo(resolutionInfo.width, resolutionInfo.height);
					ApplyScreenResolution();
					GameConsole.Print("Applying...");
				}
			}
		});
	}

	public float GetResolutionIndex(GraphicSettings.ResolutionInfo resolution)
	{
		int num = 0;
		float num2 = float.MaxValue;
		for (int i = 0; i <= settings.supportedResolutionsList.Count - 1; i++)
		{
			GraphicSettings.ResolutionInfo resolutionInfo = settings.supportedResolutionsList[i];
			int num3 = resolution.width - resolutionInfo.width;
			int num4 = resolution.height - resolutionInfo.height;
			float num5 = Mathf.Sqrt(num3 * num3 + num4 * num4);
			if (num5 < num2)
			{
				num2 = num5;
				num = i;
			}
		}
		if (num2 < 0.1f)
		{
			return num;
		}
		return Mathf.Clamp((float)num - 0.5f, 0f, settings.supportedResolutionsList.Count - 1);
	}

	public int GetScreenModeUISwtcherIndex(FullScreenMode prevMode)
	{
		return prevMode switch
		{
			FullScreenMode.ExclusiveFullScreen => 1, 
			FullScreenMode.FullScreenWindow => 2, 
			FullScreenMode.Windowed => 0, 
			_ => -1, 
		};
	}

	public int GetScreenModeIndex(AllowedWindowModeOptions prevMode)
	{
		return prevMode switch
		{
			AllowedWindowModeOptions.ExclusiveFullScreen => 0, 
			AllowedWindowModeOptions.FullScreenWindow => 1, 
			AllowedWindowModeOptions.Windowed => 3, 
			_ => -1, 
		};
	}

	private void Update()
	{
		if (m_resStateInvalid)
		{
			ApplyScreenResolution();
		}
		else if (Overlay.IsReady() && settings != null)
		{
			bool fullScreen = Screen.fullScreen;
			int width = Screen.width;
			int height = Screen.height;
			if (!fullScreen && (width != settings.resolution.width || height != settings.resolution.height))
			{
				settings.resolution = new GraphicSettings.ResolutionInfo(width, height);
			}
		}
	}
}
