using System;
using Binding;
using Binding.Base;
using DI;
using PrefsModels;
using UnityEngine;
using UnityEngine.XR;

namespace HUD
{
	public class UIFreerideButtonsPanel : BehaviourBindingTarget
	{
		private enum Mode
		{
			MapExpanded = 0,
			MapHiden = 1,
			Expanded = 2,
			Hiden = 3
		}

		private BoolProperty m_expanded = new BoolProperty();

		private IntProperty m_mode = new IntProperty();

		private GlobalModel m_globals;

		private void RefreshVisibility()
		{
			if (m_globals.showMinimap)
			{
				m_mode.value = ((!m_expanded.value) ? 1 : 0);
			}
			else
			{
				m_mode.value = (m_expanded.value ? 2 : 3);
			}
		}

		private void OnEnable()
		{
			Canvas canvas = null;
			Transform transform = null;
			if (base.transform.childCount > 0)
			{
				transform = base.transform.GetChild(0);
				canvas = transform.GetComponent<Canvas>();
			}
			if (transform == null)
			{
				return;
			}
			if (XRSettings.isDeviceActive)
			{
				if (canvas == null)
				{
					transform.gameObject.AddComponent<Canvas>();
				}
			}
			else if (canvas != null)
			{
				UnityEngine.Object.Destroy(canvas);
			}
		}

		private void Start()
		{
			m_globals = DependencyInjector.Resolve<GamePrefs>().globals;
			RefreshVisibility();
			GlobalModel globals = m_globals;
			globals.onShowMinimapChanged = (Action)Delegate.Combine(globals.onShowMinimapChanged, new Action(RefreshVisibility));
		}

		private void OnDestroy()
		{
			if (m_globals != null)
			{
				GlobalModel globals = m_globals;
				globals.onShowMinimapChanged = (Action)Delegate.Remove(globals.onShowMinimapChanged, new Action(RefreshVisibility));
			}
		}

		public void Toggle()
		{
			m_expanded.value = !m_expanded.value;
			RefreshVisibility();
		}
	}
}
