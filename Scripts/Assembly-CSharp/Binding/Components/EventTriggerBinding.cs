using Binding.Base;
using UnityEngine.EventSystems;

namespace Binding.Components
{
	public class EventTriggerBinding : BaseBinding<EventTriggerProperty>
	{
		protected void Awake()
		{
			Init();
			if (base.property != null)
			{
				base.property.value = GetComponent<EventTrigger>();
			}
		}

		protected override void Start()
		{
			base.Start();
			if (base.property != null)
			{
				base.property.value = GetComponent<EventTrigger>();
			}
		}
	}
}
