using System;

namespace Binding
{
	public class AlphaFadeProperty : FloatProperty
	{
		public float fadeTime { get; set; }

		public bool force { get; private set; }

		public event Action OnFadeFinish;

		public AlphaFadeProperty()
		{
			fadeTime = 1f;
		}

		public AlphaFadeProperty(float startFadeTime = 1f)
		{
			fadeTime = startFadeTime;
		}

		public AlphaFadeProperty(float startValue = 0f, float startFadeTime = 1f)
			: base(startValue)
		{
			fadeTime = startFadeTime;
		}

		public void SetForce(float alpha)
		{
			force = true;
			base.value = alpha;
			force = false;
		}

		public void Finish()
		{
			this.OnFadeFinish.Call();
		}
	}
}
