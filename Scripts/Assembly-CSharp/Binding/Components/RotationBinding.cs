using Binding.Base;

namespace Binding.Components
{
	public class RotationBinding : BaseBinding<QuaternionProperty>
	{
		protected override void OnUpdateValue()
		{
			base.transform.rotation = base.property.value;
		}
	}
}
