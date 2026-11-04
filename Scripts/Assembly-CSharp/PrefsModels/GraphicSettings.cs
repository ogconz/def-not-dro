using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SimpleJSON;
using UnityEngine;

namespace PrefsModels
{
	public class GraphicSettings : BasePrefsModel
	{
		public class ResolutionInfo
		{
			public int width { get; private set; }

			public int height { get; private set; }

			public ResolutionInfo()
			{
			}

			public ResolutionInfo(int w, int h)
			{
				width = w;
				height = h;
			}

			public ResolutionInfo(Resolution resolution)
			{
				width = resolution.width;
				height = resolution.height;
			}

			public override bool Equals(object prm)
			{
				if (prm == null)
				{
					return false;
				}
				if (!(prm is ResolutionInfo resolutionInfo))
				{
					return false;
				}
				if (width == resolutionInfo.width)
				{
					return height == resolutionInfo.height;
				}
				return false;
			}

			public bool Equals(ResolutionInfo prm)
			{
				if (prm == null)
				{
					return false;
				}
				if (width == prm.width)
				{
					return height == prm.height;
				}
				return false;
			}

			public override int GetHashCode()
			{
				return width + height << 16;
			}

			public override string ToString()
			{
				return $"{width} x {height}";
			}
		}

		private readonly Dictionary<string, int> m_defaultParameters = new Dictionary<string, int>
		{
			{ "fullscreen", 1 },
			{ "antiAliasing", 1 },
			{ "volumetricFog", 1 },
			{ "ao", 2 },
			{ "shadows", 2 },
			{ "motionBlur", 2 },
			{ "bloom", 2 },
			{ "lod", 2 },
			{ "textureQuality", 2 },
			{ "anisoLevel", 2 },
			{ "vSync", 1 },
			{ "smokeLevel", 2 },
			{ "ssrLevel", 0 },
			{ "ssgi", 0 },
			{ "skidmarkLength", 1 },
			{ "opponentSmokeOpacity", 100 },
			{ "playerSmokeOpacity", 100 },
			{ "lightOpponentPower", 1 },
			{ "lightPlayerPower", 1 },
			{ "compressVinyls", 1 }
		};

		private int m_antiAliasing = 1;

		private int m_volumetricFog = 1;

		private int m_ao = 2;

		private int m_shadows = 2;

		private int m_motionBlur = 2;

		private int m_bloom = 2;

		private int m_lod = 2;

		private int m_textureQuality = 2;

		private int m_anisoLevel = 2;

		private int m_ssgi;

		private int m_skidmarkLength = 1;

		private int m_vSync = 1;

		private float m_lightPlayerPower = 1f;

		private float m_lightOpponentPower = 1f;

		private int m_qualityLevel = 2;

		private int m_fullscreen = 1;

		private int m_smokeLevel = 2;

		private int m_ssrLevel;

		private float m_opponentSmokeOpacity = 100f;

		private float m_playerSmokeOpacity = 100f;

		public int? vSyncPrevVal;

		private int m_compressVinyls = 1;

		public readonly int[] skidmarkPower = new int[4] { 0, 1, 25, 50 };

		private ResolutionInfo m_windowedResolution;

		private ResolutionInfo m_fullscreenResolution;

		private readonly List<ResolutionInfo> m_supportedResolutionList = new List<ResolutionInfo>();

		[JsonIgnore]
		public Action<ResolutionInfo> onResolutionChanged;

		public int antiAliasing
		{
			get
			{
				return m_antiAliasing;
			}
			set
			{
				SetValue(ref m_antiAliasing, value);
			}
		}

		public int volumetricFog
		{
			get
			{
				return m_volumetricFog;
			}
			set
			{
				SetValue(ref m_volumetricFog, value);
			}
		}

		public int ao
		{
			get
			{
				return m_ao;
			}
			set
			{
				SetValue(ref m_ao, value);
			}
		}

		public int shadows
		{
			get
			{
				return m_shadows;
			}
			set
			{
				SetValue(ref m_shadows, value);
			}
		}

		public int motionBlur
		{
			get
			{
				return m_motionBlur;
			}
			set
			{
				SetValue(ref m_motionBlur, value);
			}
		}

		public int bloom
		{
			get
			{
				return m_bloom;
			}
			set
			{
				SetValue(ref m_bloom, value);
			}
		}

		public int lod
		{
			get
			{
				return m_lod;
			}
			set
			{
				SetValue(ref m_lod, value);
			}
		}

		public int textureQuality
		{
			get
			{
				return m_textureQuality;
			}
			set
			{
				SetValue(ref m_textureQuality, value);
			}
		}

		public int anisoLevel
		{
			get
			{
				return m_anisoLevel;
			}
			set
			{
				SetValue(ref m_anisoLevel, value);
			}
		}

		public int ssrLevel
		{
			get
			{
				return m_ssrLevel;
			}
			set
			{
				SetValue(ref m_ssrLevel, value);
			}
		}

		public int qualityLevel
		{
			get
			{
				return m_qualityLevel;
			}
			set
			{
				SetValue(ref m_qualityLevel, value);
			}
		}

		public int fullscreen
		{
			get
			{
				return m_fullscreen;
			}
			set
			{
				SetValue(ref m_fullscreen, value);
			}
		}

		public int vSync
		{
			get
			{
				return m_vSync;
			}
			set
			{
				SetValue(ref m_vSync, value);
			}
		}

		public float lightPlayerPower
		{
			get
			{
				return m_lightPlayerPower;
			}
			set
			{
				SetValue(ref m_lightPlayerPower, value);
			}
		}

		public float lightOpponentPower
		{
			get
			{
				return m_lightOpponentPower;
			}
			set
			{
				SetValue(ref m_lightOpponentPower, value);
			}
		}

		public int skidmarkLength
		{
			get
			{
				return m_skidmarkLength;
			}
			set
			{
				SetValue(ref m_skidmarkLength, value);
			}
		}

		public int smokeLevel
		{
			get
			{
				return m_smokeLevel;
			}
			set
			{
				SetValue(ref m_smokeLevel, value);
			}
		}

		public float opponentSmokeOpacity
		{
			get
			{
				return m_opponentSmokeOpacity;
			}
			set
			{
				SetValue(ref m_opponentSmokeOpacity, value);
			}
		}

		public float playerSmokeOpacity
		{
			get
			{
				return m_playerSmokeOpacity;
			}
			set
			{
				SetValue(ref m_playerSmokeOpacity, value);
			}
		}

		public int ssgi
		{
			get
			{
				return m_ssgi;
			}
			set
			{
				SetValue(ref m_ssgi, value);
			}
		}

		public int compressVinyls
		{
			get
			{
				return m_compressVinyls;
			}
			set
			{
				SetValue(ref m_compressVinyls, value);
			}
		}

		public ResolutionInfo resolution
		{
			get
			{
				if (m_fullscreen != 0)
				{
					return m_fullscreenResolution;
				}
				return m_windowedResolution;
			}
			set
			{
				if (m_fullscreen == 0)
				{
					m_windowedResolution = value;
				}
				else
				{
					m_fullscreenResolution = value;
				}
				onResolutionChanged.SafeCall(value);
				Save();
			}
		}

		public ReadOnlyCollection<ResolutionInfo> supportedResolutionsList => m_supportedResolutionList.AsReadOnly();

		private void SetValue<T>(ref T origValue, T newValue)
		{
			if (!origValue.Equals(newValue))
			{
				origValue = newValue;
				Save();
			}
		}

		public GraphicSettings()
		{
			LoadSupportedResolutionList();
			m_windowedResolution = new ResolutionInfo(Display.main.systemWidth, Display.main.systemHeight);
			m_fullscreenResolution = new ResolutionInfo(Display.main.systemWidth, Display.main.systemHeight);
		}

		private void LoadSupportedResolutionList()
		{
			m_supportedResolutionList.Clear();
			Resolution[] resolutions = Screen.resolutions;
			for (int i = 0; i < resolutions.Length; i++)
			{
				Resolution p = resolutions[i];
				if (!m_supportedResolutionList.Exists((ResolutionInfo x) => x.width == p.width && x.height == p.height))
				{
					m_supportedResolutionList.Add(new ResolutionInfo(p.width, p.height));
				}
			}
			m_supportedResolutionList.Sort(delegate(ResolutionInfo lhs, ResolutionInfo rhs)
			{
				if (lhs.width > rhs.width)
				{
					return 1;
				}
				if (lhs.width < rhs.width)
				{
					return -1;
				}
				if (lhs.height > rhs.height)
				{
					return 1;
				}
				return (lhs.height < rhs.height) ? (-1) : 0;
			});
		}

		public void ResetGraphicSettings(out bool hasMainChanges)
		{
			hasMainChanges = false;
			if (antiAliasing != m_defaultParameters["antiAliasing"])
			{
				antiAliasing = m_defaultParameters["antiAliasing"];
				hasMainChanges = true;
			}
			if (volumetricFog != m_defaultParameters["volumetricFog"])
			{
				volumetricFog = m_defaultParameters["volumetricFog"];
				hasMainChanges = true;
			}
			if (ao != m_defaultParameters["ao"])
			{
				ao = m_defaultParameters["ao"];
				hasMainChanges = true;
			}
			if (shadows != m_defaultParameters["shadows"])
			{
				shadows = m_defaultParameters["shadows"];
				hasMainChanges = true;
			}
			if (motionBlur != m_defaultParameters["motionBlur"])
			{
				motionBlur = m_defaultParameters["motionBlur"];
				hasMainChanges = true;
			}
			if (bloom != m_defaultParameters["bloom"])
			{
				bloom = m_defaultParameters["bloom"];
				hasMainChanges = true;
			}
			if (lod != m_defaultParameters["lod"])
			{
				lod = m_defaultParameters["lod"];
				hasMainChanges = true;
			}
			if (textureQuality != m_defaultParameters["textureQuality"])
			{
				textureQuality = m_defaultParameters["textureQuality"];
				hasMainChanges = true;
			}
			if (anisoLevel != m_defaultParameters["anisoLevel"])
			{
				anisoLevel = m_defaultParameters["anisoLevel"];
				hasMainChanges = true;
			}
			if (vSync != m_defaultParameters["vSync"])
			{
				vSync = m_defaultParameters["vSync"];
				hasMainChanges = true;
			}
			if (skidmarkLength != m_defaultParameters["skidmarkLength"])
			{
				skidmarkLength = m_defaultParameters["skidmarkLength"];
				hasMainChanges = true;
			}
			if (smokeLevel != m_defaultParameters["smokeLevel"])
			{
				smokeLevel = m_defaultParameters["smokeLevel"];
				hasMainChanges = true;
			}
			if (ssrLevel != m_defaultParameters["ssrLevel"])
			{
				ssrLevel = m_defaultParameters["ssrLevel"];
				hasMainChanges = true;
			}
			if (ssgi != m_defaultParameters["ssgi"])
			{
				ssgi = m_defaultParameters["ssgi"];
				hasMainChanges = true;
			}
			opponentSmokeOpacity = m_defaultParameters["opponentSmokeOpacity"];
			playerSmokeOpacity = m_defaultParameters["playerSmokeOpacity"];
			lightOpponentPower = m_defaultParameters["lightOpponentPower"];
			lightPlayerPower = m_defaultParameters["lightPlayerPower"];
			compressVinyls = m_defaultParameters["compressVinyls"];
			Save();
		}
	}
}
