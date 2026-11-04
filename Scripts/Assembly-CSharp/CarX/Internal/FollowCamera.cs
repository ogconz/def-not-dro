using System;
using UnityEngine;
using UnityEngine.XR;

namespace CarX.Internal
{
	public class FollowCamera
	{
		private float m_nearClipPlane = 0.3f;

		private float m_fieldOfView = 50f;

		public float nearClipPlane => m_nearClipPlane;

		public float fieldOfView => m_fieldOfView;

		public void Reset()
		{
			m_nearClipPlane = 0.3f;
		}

		public void Update(float carFieldOfView, Vector3 targetPosition, Vector3 cameraPosition, float dt)
		{
			float num = Vector3.Distance(targetPosition, cameraPosition);
			float num2 = 50f;
			float num3 = 0.3f;
			if (XRSettings.isDeviceActive)
			{
				num3 = 0f;
			}
			float min = num * 2f * Mathf.Tan(MathF.PI / 180f);
			float max = num * 2f * Mathf.Tan(0.34906584f);
			float num4 = Mathf.Clamp(carFieldOfView, min, max);
			if (dt >= 0f)
			{
				float num5 = num * 2f * Mathf.Tan(m_fieldOfView * (MathF.PI / 180f) / 2f);
				float num6 = num4 - num5;
				num4 = Mathf.Lerp(num4, num5 + num6, 2f * dt);
			}
			float b = 2f * Mathf.Atan2(num4, 2f * num) * 57.29578f;
			num2 = Mathf.Lerp(m_fieldOfView, b, Time.deltaTime);
			num3 = 0.3f + 20f / num2;
			if (XRModeManager.IsXRActive())
			{
				num3 = 0.1f;
			}
			m_nearClipPlane = num3;
			m_fieldOfView = num2;
		}
	}
}
