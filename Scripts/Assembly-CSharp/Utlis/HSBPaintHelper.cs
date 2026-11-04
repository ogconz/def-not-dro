using Binding.Base;
using UnityEngine;

namespace Utlis
{
	public static class HSBPaintHelper
	{
		private const float SATURATION_THRESHOLD = 0.01f;

		private const float SATURATION_DEFAULT = 0.5f;

		private const float BRIGHTNESS_THRESHOLD = 0.01f;

		private const float BRIGHTNESS_DEFAULT = 0.5f;

		public static void ResetColorIfNeed(ref float previous, Property<float> current, Property<float> saturation, Property<float> brightness)
		{
			if (!Mathf.Approximately(previous, current.value))
			{
				previous = current.value;
				if (saturation.value < 0.01f)
				{
					saturation.value = 0.5f;
				}
				if (brightness.value < 0.01f)
				{
					brightness.value = 0.5f;
				}
			}
		}
	}
}
