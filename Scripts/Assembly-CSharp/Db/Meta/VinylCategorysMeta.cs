using System;
using GameOverlay;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class VinylCategorysMeta
	{
		public const int VINYL_PRESETS_CATEGORY_ID = -1;

		public string icon;

		public string name;

		public int id;

		public int sort;

		[SerializeField]
		private string m_dlc;

		private DLCCode? m_dlcCode;

		public AvailablePlatform availablePlatform;

		public DLCCode dlcCode
		{
			get
			{
				if (m_dlcCode.HasValue)
				{
					return m_dlcCode.Value;
				}
				if (!string.IsNullOrEmpty(m_dlc))
				{
					try
					{
						m_dlcCode = (DLCCode)Enum.Parse(typeof(DLCCode), m_dlc, ignoreCase: true);
						return m_dlcCode.Value;
					}
					catch (Exception message)
					{
						Debug.Log(message);
					}
				}
				m_dlcCode = DLCCode.Unknown;
				return m_dlcCode.Value;
			}
		}

		public void SetDLCCode(DLCCode code)
		{
			if (Application.isPlaying)
			{
				throw new InvalidOperationException("SetDLCCode can not be done at runtime");
			}
			m_dlc = code.ToString();
		}

		public void ResetDlcField()
		{
			m_dlc = string.Empty;
		}
	}
}
