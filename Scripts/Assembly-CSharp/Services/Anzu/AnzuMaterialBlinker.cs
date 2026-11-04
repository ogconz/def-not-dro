using System;
using System.Collections;
using UnityEngine;
using anzu;

namespace Services.Anzu
{
	[Serializable]
	public class AnzuMaterialBlinker
	{
		private const float BlinkDurationSec = 0.5f;

		private const float MaxEmissionIntensity = 25f;

		private const string EmissionColorProp = "_EmissiveColor";

		[SerializeField]
		private Renderer m_renderer;

		private Material m_material;

		private Color? m_originalEmissionColor;

		private Coroutine m_blinkRoutine;

		public AnzuMaterialBlinker(AnzuAd nativeAd)
		{
			m_renderer = nativeAd.GetComponent<MeshRenderer>();
		}

		public void BlinkMaterial()
		{
			if (m_material == null)
			{
				m_material = m_renderer.material;
			}
			Color valueOrDefault = m_originalEmissionColor.GetValueOrDefault();
			if (!m_originalEmissionColor.HasValue)
			{
				valueOrDefault = m_material.GetColor("_EmissiveColor");
				m_originalEmissionColor = valueOrDefault;
			}
			m_blinkRoutine = TimeController.StartNow(BlinkRoutine());
		}

		private IEnumerator BlinkRoutine()
		{
			float timer = 0f;
			while (timer < 0.5f)
			{
				float t = Mathf.PingPong(timer, 0.25f);
				Color value = Color.Lerp(m_originalEmissionColor.GetValueOrDefault(), Color.green * 25f, t);
				m_material.SetColor("_EmissiveColor", value);
				timer += Time.deltaTime;
				yield return null;
			}
			ResetMaterial();
		}

		private void ResetMaterial()
		{
			m_material.SetColor("_EmissiveColor", m_originalEmissionColor.GetValueOrDefault());
		}

		public void Dispose()
		{
			if (m_blinkRoutine != null)
			{
				TimeController.StopNow(m_blinkRoutine);
			}
			UnityEngine.Object.Destroy(m_material);
			m_material = null;
		}
	}
}
