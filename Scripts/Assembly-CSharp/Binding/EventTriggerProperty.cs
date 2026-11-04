using System.Collections.Generic;
using Binding.Base;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Binding
{
	public class EventTriggerProperty : Property<EventTrigger>
	{
		private List<EventTrigger.Entry> m_triggers = new List<EventTrigger.Entry>();

		public EventTriggerProperty()
			: base((EventTrigger)null)
		{
		}

		public EventTriggerProperty(EventTrigger startValue = null)
			: base(startValue)
		{
		}

		public void Add(EventTriggerType type, UnityAction<BaseEventData> callback)
		{
			GetTriggerEntry(type).callback.AddListener(callback);
			UpdateInternalTriggers();
		}

		public void Remove(EventTriggerType type, UnityAction<BaseEventData> callback)
		{
			EventTrigger.Entry triggerEntry = GetTriggerEntry(type);
			triggerEntry.callback.RemoveListener(callback);
			if (triggerEntry.callback.GetPersistentEventCount() == 0)
			{
				m_triggers.Remove(triggerEntry);
				UpdateInternalTriggers();
			}
		}

		private EventTrigger.Entry GetTriggerEntry(EventTriggerType type)
		{
			EventTrigger.Entry entry = m_triggers.Find((EventTrigger.Entry x) => x.eventID == type);
			if (entry == null)
			{
				entry = new EventTrigger.Entry();
				entry.eventID = type;
				m_triggers.Add(entry);
			}
			return entry;
		}

		private void UpdateInternalTriggers()
		{
			base.OnValueChanged -= UpdateInternalTriggers;
			if (base.value != null)
			{
				base.value.triggers = m_triggers;
			}
			else
			{
				base.OnValueChanged += UpdateInternalTriggers;
			}
		}
	}
}
