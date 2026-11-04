using Binding;
using Binding.Base;
using Chat.Overlay.UI.State;
using Chat.Overlay.UI.Utils;
using GameInput;

namespace Chat.Overlay.UI.Control
{
	public class ControlStateBinder : BaseBindingTarget
	{
		private ActionElementBindingProperty m_openTextChatAction = new ActionElementBindingProperty();

		private StateContext m_state;

		private HotkeyProvider m_hotkeyProvider;

		public void Setup(StateContext state, HotkeyProvider hotkeyProvider)
		{
			m_state = state;
			m_hotkeyProvider = hotkeyProvider;
			m_state.controlLayerActive.onChanged += OnControlLayerActiveHandler;
			m_state.deviceWithNavigation.onChanged += OnDeviceWithNavigationChangedHandler;
			m_openTextChatAction.BindAction(EnableControlLayer);
		}

		public void Release()
		{
			m_state.controlLayerActive.onChanged -= OnControlLayerActiveHandler;
			m_state.deviceWithNavigation.onChanged -= OnDeviceWithNavigationChangedHandler;
			m_openTextChatAction.ReleaseAction();
			m_state = null;
			m_hotkeyProvider = null;
		}

		private void OnControlLayerActiveHandler(bool isActive)
		{
			if (isActive)
			{
				m_hotkeyProvider.Add(ActionType.NavCancelBtnB, DisableControlLayer, repeat: false);
			}
			else
			{
				m_hotkeyProvider.Remove(ActionType.NavCancelBtnB, DisableControlLayer);
			}
			m_hotkeyProvider.UpdateHotkeys();
		}

		private void OnDeviceWithNavigationChangedHandler(bool isDeviceWithNavigation)
		{
			m_state.windowFocused.Set(m_state.controlLayerActive.Get() && isDeviceWithNavigation);
		}

		private void EnableControlLayer()
		{
			if (m_state.deviceWithNavigation.Get() && !m_state.controlLayerActive.Get())
			{
				m_state.messageSelecting.Set(value: true);
			}
			else
			{
				m_state.inputFieldFocused.Set(value: true);
			}
			m_state.windowFocused.Set(m_state.deviceWithNavigation.Get());
		}

		private void DisableControlLayer()
		{
			m_state.windowFocused.Set(value: false);
			m_state.messageSelecting.Set(value: false);
			m_state.inputFieldFocused.Set(value: false);
		}
	}
}
