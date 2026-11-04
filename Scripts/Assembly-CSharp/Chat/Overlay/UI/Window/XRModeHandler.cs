using System;
using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.Window
{
	public class XRModeHandler
	{
		private StateContext m_state;

		private XRModeManager m_instanceHandle;

		private bool m_initialized;

		public void Setup(StateContext state)
		{
			m_state = state;
			BindInstanceHandle();
		}

		public void Release()
		{
			ReleaseInstanceHandle();
			m_state = null;
		}

		private void BindInstanceHandle()
		{
			XRModeManager.onXRInstanceCreated = (Action<XRModeManager>)Delegate.Combine(XRModeManager.onXRInstanceCreated, new Action<XRModeManager>(OnXRInstanceCreatedHandler));
			XRModeManager.onXRInstanceDestroyed = (Action<XRModeManager>)Delegate.Combine(XRModeManager.onXRInstanceDestroyed, new Action<XRModeManager>(OnXRInstanceDestroyedHandler));
			if (XRModeManager.instance != null)
			{
				OnXRInstanceCreatedHandler(XRModeManager.instance);
			}
		}

		private void ReleaseInstanceHandle()
		{
			if (m_instanceHandle != null)
			{
				OnXRInstanceDestroyedHandler(m_instanceHandle);
			}
			XRModeManager.onXRInstanceCreated = (Action<XRModeManager>)Delegate.Remove(XRModeManager.onXRInstanceCreated, new Action<XRModeManager>(OnXRInstanceCreatedHandler));
			XRModeManager.onXRInstanceDestroyed = (Action<XRModeManager>)Delegate.Remove(XRModeManager.onXRInstanceDestroyed, new Action<XRModeManager>(OnXRInstanceDestroyedHandler));
		}

		private void OnXRInstanceCreatedHandler(XRModeManager instance)
		{
			if (m_instanceHandle != null)
			{
				OnXRInstanceDestroyedHandler(m_instanceHandle);
			}
			m_instanceHandle = instance;
			BindInstance();
		}

		private void OnXRInstanceDestroyedHandler(XRModeManager instance)
		{
			if (m_instanceHandle == instance)
			{
				ReleaseInstance();
				m_instanceHandle = null;
			}
		}

		private void BindInstance()
		{
			XRModeManager instanceHandle = m_instanceHandle;
			instanceHandle.OnXRSubsystemActivated = (Action)Delegate.Combine(instanceHandle.OnXRSubsystemActivated, new Action(BindHandler));
			XRModeManager instanceHandle2 = m_instanceHandle;
			instanceHandle2.OnXRSubsystemDeactivated = (Action)Delegate.Combine(instanceHandle2.OnXRSubsystemDeactivated, new Action(ReleaseHandler));
			BindHandler();
		}

		private void ReleaseInstance()
		{
			ReleaseHandler();
			XRModeManager instanceHandle = m_instanceHandle;
			instanceHandle.OnXRSubsystemActivated = (Action)Delegate.Remove(instanceHandle.OnXRSubsystemActivated, new Action(BindHandler));
			XRModeManager instanceHandle2 = m_instanceHandle;
			instanceHandle2.OnXRSubsystemDeactivated = (Action)Delegate.Remove(instanceHandle2.OnXRSubsystemDeactivated, new Action(ReleaseHandler));
		}

		private void BindHandler()
		{
			if (!m_initialized && m_instanceHandle.XRSubsystemActive)
			{
				XRModeManager instanceHandle = m_instanceHandle;
				instanceHandle.OnHeadDeviceMounted = (Action)Delegate.Combine(instanceHandle.OnHeadDeviceMounted, new Action(OnHeadDeviceMountedHandler));
				XRModeManager instanceHandle2 = m_instanceHandle;
				instanceHandle2.OnHeadDeviceUnmounted = (Action)Delegate.Combine(instanceHandle2.OnHeadDeviceUnmounted, new Action(OnHeadDeviceUnmountedHandler));
				m_initialized = true;
			}
		}

		private void ReleaseHandler()
		{
			if (m_initialized)
			{
				m_initialized = false;
				XRModeManager instanceHandle = m_instanceHandle;
				instanceHandle.OnHeadDeviceMounted = (Action)Delegate.Remove(instanceHandle.OnHeadDeviceMounted, new Action(OnHeadDeviceMountedHandler));
				XRModeManager instanceHandle2 = m_instanceHandle;
				instanceHandle2.OnHeadDeviceUnmounted = (Action)Delegate.Remove(instanceHandle2.OnHeadDeviceUnmounted, new Action(OnHeadDeviceUnmountedHandler));
			}
		}

		private void OnHeadDeviceMountedHandler()
		{
			m_state.xrModeActive.Set(value: true);
		}

		private void OnHeadDeviceUnmountedHandler()
		{
			m_state.xrModeActive.Set(value: false);
		}
	}
}
