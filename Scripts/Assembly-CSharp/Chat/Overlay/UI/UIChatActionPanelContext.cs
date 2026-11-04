using System;
using System.Collections.Generic;
using Binding.Base;
using Chat.Overlay.UI.ActionList;
using Chat.Overlay.UI.State;
using Chat.Overlay.UI.Utils;
using UnityEngine;

namespace Chat.Overlay.UI
{
	public class UIChatActionPanelContext : BaseBindingBehaviourTarget
	{
		private ChatActionListProperty m_actions = new ChatActionListProperty();

		private ActionPanelStateHandler m_stateHandler = new ActionPanelStateHandler();

		private FocusHandler m_focusHandler = new FocusHandler();

		private AutoCloseHandler m_autoCloseHandler = new AutoCloseHandler();

		private AutoSelectHandler m_autoSelectHandler = new AutoSelectHandler();

		private HotkeyProvider m_hotkeyProvider = new HotkeyProvider();

		private NavigationBinder m_navigationBinder = new NavigationBinder();

		private StateContext m_stateContext;

		private bool m_initialized;

		public event Action onActivityChanged;

		public event Action onReleased;

		public bool IsActive()
		{
			return base.isActive;
		}

		public void Setup(StateContext stateContext)
		{
			m_stateContext = stateContext;
			m_stateHandler.Setup(this, m_stateContext);
			m_focusHandler.Setup(m_stateContext, m_actions);
			m_autoCloseHandler.Setup(this, m_actions, m_stateContext);
			m_autoSelectHandler.Setup(m_stateContext, m_actions);
		}

		public void Release()
		{
			Close();
			m_autoSelectHandler.Release();
			m_autoCloseHandler.Release();
			m_focusHandler.Release();
			m_stateHandler.Release();
			m_stateContext = null;
		}

		public void Open(Dictionary<string, Action> actions)
		{
			if (!m_initialized)
			{
				m_initialized = true;
				m_stateContext.forceActivateControlLayer.Set(value: true);
				m_actions.Setup(actions);
				base.owner.Push<UIChatActionPanelContext>();
				m_navigationBinder.Setup(m_hotkeyProvider, this, m_actions);
				InvokeOnActivityChanged();
				m_stateContext.forceActivateControlLayer.Set(value: false);
			}
		}

		public void Close()
		{
			if (m_initialized)
			{
				m_initialized = false;
				base.owner.Hide<UIChatActionPanelContext>();
				this.onReleased.SafeCall();
				m_navigationBinder.Release();
				m_actions.Release();
				InvokeOnActivityChanged();
			}
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			m_hotkeyProvider.Setup(this);
		}

		protected override void OnDeactivate()
		{
			m_hotkeyProvider.Release();
			Close();
			base.OnDeactivate();
		}

		public void SetPosition(Vector2 position)
		{
			if (m_initialized)
			{
				RectTransform obj = base.transform as RectTransform;
				obj.position = new Vector3(obj.position.x, position.y, 0f);
			}
		}

		private void InvokeOnActivityChanged()
		{
			this.onActivityChanged.SafeCall();
		}

		public override bool IsTopOrBottomContext(BaseContext context)
		{
			if (m_stateContext != null && m_stateContext.controlLayerActive.Get())
			{
				return false;
			}
			return base.IsTopOrBottomContext(context);
		}
	}
}
