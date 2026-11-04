using Binding;
using Binding.Base;
using CarX.Gameplay.ClippingZone;
using UnityEngine;

namespace HUD
{
	public class ClipZoneBindingsItem : BaseBindingBehaviourTarget
	{
		public new IntProperty state = new IntProperty();

		public Property<Color> color = new Property<Color>();

		public bool failedInCurrentFrame;

		private BaseClipping m_host;

		private BaseClipping.State m_oldState;

		public ClipZoneBindingsItem(bool state)
		{
			ApplyState(state);
		}

		public ClipZoneBindingsItem(BaseClipping host)
		{
			m_host = host;
			m_oldState = host.state + 1;
			Update();
		}

		public void Update()
		{
			failedInCurrentFrame = false;
			if (m_oldState != m_host.state)
			{
				m_oldState = m_host.state;
				if (m_host.state == BaseClipping.State.Idle)
				{
					state.value = 0;
					color.value = Color.white;
				}
				else
				{
					state.value = ((m_host.state == BaseClipping.State.Activated) ? 1 : (-1));
					ApplyState(state.value == 1);
				}
				if (m_host.state == BaseClipping.State.Failed)
				{
					failedInCurrentFrame = true;
				}
			}
		}

		private void ApplyState(bool state)
		{
			color.value = (state ? Color.green : Color.red);
		}
	}
}
