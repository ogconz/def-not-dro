using System;
using UnityEngine;

public class CameraCustomizerLimits : ScriptableSingleton<CameraCustomizerLimits>
{
	[Serializable]
	public class LimitsDynamicCam
	{
		[SerializeField]
		private string m_identifier;

		[SerializeField]
		private float m_min;

		[SerializeField]
		private float m_max = 1f;

		[SerializeField]
		private float m_step = 0.1f;

		[SerializeField]
		private float m_defaultValue = 0.5f;

		public string identifier => m_identifier;

		public float min => m_min;

		public float max => m_max;

		public float step => m_step;

		public float defaultValue => m_defaultValue;
	}

	[SerializeField]
	private LimitsCamera[] m_cockpitsCamera;

	[SerializeField]
	private LimitsDynamicCam[] m_dynamicCockpitCamera;

	public LimitsCamera GetCockpitLimits(string identifier)
	{
		LimitsCamera[] cockpitsCamera = m_cockpitsCamera;
		foreach (LimitsCamera limitsCamera in cockpitsCamera)
		{
			if (limitsCamera.identifier == identifier)
			{
				return limitsCamera;
			}
		}
		return new LimitsCamera();
	}

	public LimitsDynamicCam GetDynamicCockpitLimits(string identifier)
	{
		LimitsDynamicCam[] dynamicCockpitCamera = m_dynamicCockpitCamera;
		foreach (LimitsDynamicCam limitsDynamicCam in dynamicCockpitCamera)
		{
			if (limitsDynamicCam.identifier == identifier)
			{
				return limitsDynamicCam;
			}
		}
		return null;
	}
}
