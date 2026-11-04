using System;
using UnityEngine;

public class FreeDroneCameraLimits : ScriptableSingleton<FreeDroneCameraLimits>
{
	[Serializable]
	public class DataParams
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
	private DataParams[] m_limits;

	public DataParams GetDataParam(string identifier)
	{
		DataParams[] limits = m_limits;
		foreach (DataParams dataParams in limits)
		{
			if (dataParams.identifier == identifier)
			{
				return dataParams;
			}
		}
		return new DataParams();
	}
}
