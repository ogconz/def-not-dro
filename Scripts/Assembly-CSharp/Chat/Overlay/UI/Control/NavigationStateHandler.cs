using System;
using Chat.Overlay.UI.State;
using GameInput;
using Rewired;

namespace Chat.Overlay.UI.Control
{
	public class NavigationStateHandler
	{
		private StateContext m_state;

		private DeviceType m_lastDeviceType;

		public void Setup(StateContext state)
		{
			m_state = state;
			m_state.controlLayerActive.onChanged += OnControlLayerActiveHandler;
			GameInput.InputManager instance = GameInput.InputManager.instance;
			instance.onChangeActiveDevice = (Action<Controller>)Delegate.Combine(instance.onChangeActiveDevice, new Action<Controller>(OnChangeActiveDeviceHandler));
			OnChangeActiveDeviceHandler(GameInput.InputManager.instance.lastController);
		}

		public void Release()
		{
			m_state.controlLayerActive.onChanged -= OnControlLayerActiveHandler;
			GameInput.InputManager instance = GameInput.InputManager.instance;
			instance.onChangeActiveDevice = (Action<Controller>)Delegate.Remove(instance.onChangeActiveDevice, new Action<Controller>(OnChangeActiveDeviceHandler));
			m_state = null;
		}

		private void OnControlLayerActiveHandler(bool isActive)
		{
			UpdateNavigationLayerState();
		}

		private void OnChangeActiveDeviceHandler(Controller controller)
		{
			DeviceType deviceType = controller.GetMetaInfo().deviceType;
			if (IsDeviceChanged(in deviceType))
			{
				m_lastDeviceType = deviceType;
				UpdateNavigationLayerState();
			}
		}

		private void UpdateNavigationLayerState()
		{
			m_state.deviceWithNavigation.Set(CanActivateNavigationLayer(in m_lastDeviceType));
			m_state.navigationLayerActive.Set(m_state.controlLayerActive.Get() && m_state.deviceWithNavigation.Get());
		}

		private bool CanActivateNavigationLayer(in DeviceType deviceType)
		{
			if (IsStandaloneDevice(in deviceType))
			{
				return false;
			}
			return true;
		}

		private bool IsDeviceChanged(in DeviceType deviceType)
		{
			return IsStandaloneDevice(in m_lastDeviceType) != IsStandaloneDevice(in deviceType);
		}

		private bool IsStandaloneDevice(in DeviceType deviceType)
		{
			if (deviceType != DeviceType.Keyboard)
			{
				return deviceType == DeviceType.Mouse;
			}
			return true;
		}
	}
}
