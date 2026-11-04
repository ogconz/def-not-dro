namespace Binding.Components
{
	public class VisibilityBoolBinding : BoolBinding
	{
		protected override void OnUpdateValue()
		{
			base.gameObject.SetActive(currentValue);
		}

		private void OnEnable()
		{
			if (base.initialized)
			{
				UpdateValue();
			}
		}
	}
}
