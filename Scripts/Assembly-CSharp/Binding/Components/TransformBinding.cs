using Binding.Base;

namespace Binding.Components
{
	public class TransformBinding : BaseBinding<TransformProperty>
	{
		protected void Awake()
		{
			Init();
			UpdateValue();
		}

		protected override void OnUpdateValue()
		{
			if (base.property.value == null)
			{
				base.property.value = base.transform;
			}
		}
	}
}
