using System;
using UnityEngine;

namespace Utils.InterpolatedSurface
{
	[Serializable]
	public class BilinearSurface : INormalizedSurface
	{
		[Serializable]
		private struct Coeffisients
		{
			public float a0;

			public float a1;

			public float a2;

			public float a3;
		}

		[SerializeField]
		[HideInInspector]
		private Coeffisients[] m_coeffs;

		[SerializeField]
		[HideInInspector]
		private Vector2Int m_stepCount;

		[SerializeField]
		[HideInInspector]
		private Vector2 m_step;

		public float Evaluate(float x, float y)
		{
			Coeffisients coeffisients = m_coeffs[GetAreaIndex(x, y)];
			return coeffisients.a0 + coeffisients.a1 * x + coeffisients.a2 * y + coeffisients.a3 * x * y;
		}

		private int GetAreaIndex(float x, float y)
		{
			Vector2Int vector2Int = new Vector2Int(Mathf.Min(Mathf.FloorToInt(x / m_step.x), m_stepCount.x - 1), Mathf.Min(Mathf.FloorToInt(y / m_step.y), m_stepCount.y - 1));
			return vector2Int.x + m_stepCount.x * vector2Int.y;
		}
	}
}
