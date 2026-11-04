using GameInput;
using Rewired;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;

namespace SyncMultiplayer
{
	public class NetPlayerInputSystem : BaseNetGameSystem
	{
		public enum PlayerInputDevice
		{
			Keyboard = 0,
			Wheel = 1,
			Gamepad = 2,
			Count = 3,
			Unknown = -1
		}

		private NetworkGame m_game;

		private SetUserVariablesRequest[] m_cachedSetDeviceTypeReqs;

		private PlayerInputDevice m_lastSentDeviceType = PlayerInputDevice.Unknown;

		private PlayerInputDevice m_cachedDeviceType = PlayerInputDevice.Unknown;

		private const float INPUT_DEVICE_CHANGED_HZ = 1f;

		private TimeKeeper m_timer = new TimeKeeper(1000);

		private bool m_subscribed;

		public PlayerInputDevice cachedDeviceType
		{
			get
			{
				if (m_cachedDeviceType == PlayerInputDevice.Unknown)
				{
					m_cachedDeviceType = ExtractInputDevice(GameInput.InputManager.instance?.lastController?.GetMetaInfo());
				}
				return m_cachedDeviceType;
			}
		}

		public override void Attach(NetGameSystemsManager owner)
		{
			base.Attach(owner);
			m_game = owner.host;
			InitRequestCache(ref m_cachedSetDeviceTypeReqs, 3);
		}

		public override void OnGameConnect()
		{
			base.OnGameConnect();
			OnReconnect();
		}

		public override void OnReconnect()
		{
			base.OnReconnect();
			PlayerInputDevice playerInputDevice = ExtractInputDevice(GameInput.InputManager.instance.lastController.GetMetaInfo());
			if (m_cachedDeviceType != playerInputDevice)
			{
				m_cachedDeviceType = playerInputDevice;
				SendDeviceTypeChanged(m_cachedDeviceType);
			}
			m_timer.ResetToNow();
			SubscribeOnChangeActiveDevice(shouldSub: true);
		}

		public override void OnDisconnect()
		{
			base.OnDisconnect();
			SubscribeOnChangeActiveDevice(shouldSub: false);
		}

		public override void Update()
		{
			base.Update();
			if (m_lastSentDeviceType != m_cachedDeviceType && m_timer.ShouldExecute)
			{
				SendDeviceTypeChanged(m_cachedDeviceType);
				m_lastSentDeviceType = m_cachedDeviceType;
				m_timer.Reset();
			}
		}

		private void OnChangeActiveDevice(Controller controller)
		{
			if (controller != null)
			{
				ControllerMetaInfo metaInfo = controller.GetMetaInfo();
				PlayerInputDevice playerInputDevice = ExtractInputDevice(metaInfo);
				if (m_cachedDeviceType != playerInputDevice)
				{
					m_cachedDeviceType = playerInputDevice;
				}
			}
		}

		private void SubscribeOnChangeActiveDevice(bool shouldSub)
		{
			if (m_subscribed != shouldSub)
			{
				if (shouldSub)
				{
					ReInput.controllers.AddLastActiveControllerChangedDelegate(OnChangeActiveDevice);
				}
				else
				{
					ReInput.controllers.RemoveLastActiveControllerChangedDelegate(OnChangeActiveDevice);
				}
				m_subscribed = shouldSub;
			}
		}

		private void SendDeviceTypeChanged(PlayerInputDevice type)
		{
			if ((int)type < m_cachedSetDeviceTypeReqs.Length && type != PlayerInputDevice.Unknown)
			{
				m_game.SendRAW(m_cachedSetDeviceTypeReqs[(int)type]);
			}
		}

		private PlayerInputDevice ExtractInputDevice(ControllerMetaInfo controllerMeta)
		{
			if (controllerMeta == null)
			{
				return PlayerInputDevice.Keyboard;
			}
			switch (controllerMeta.deviceType)
			{
			case DeviceType.Wheel:
			case DeviceType.Pedals:
				return PlayerInputDevice.Wheel;
			case DeviceType.Keyboard:
			case DeviceType.Mouse:
				return PlayerInputDevice.Keyboard;
			case DeviceType.Gamepad:
				return PlayerInputDevice.Gamepad;
			default:
				return PlayerInputDevice.Keyboard;
			}
		}

		private void InitRequestCache(ref SetUserVariablesRequest[] requestCache, int count)
		{
			if (requestCache == null)
			{
				requestCache = new SetUserVariablesRequest[count];
				for (int i = 0; i < count; i++)
				{
					SFSUserVariable sFSUserVariable = new SFSUserVariable("idt", i);
					sFSUserVariable.IsPrivate = false;
					requestCache[i] = new SetUserVariablesRequest(new UserVariable[1] { sFSUserVariable });
				}
			}
		}
	}
}
