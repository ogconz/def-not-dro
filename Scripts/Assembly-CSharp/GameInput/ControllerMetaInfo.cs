using Rewired;

namespace GameInput
{
	public class ControllerMetaInfo
	{
		public bool disableFFB;

		public int deviceData = -1;

		public int userId;

		public Controller rewiredController { get; private set; }

		public ControllerType rewiredType => rewiredController.type;

		public string name { get; private set; }

		public string hardwareIdentifier => rewiredController.hardwareIdentifier;

		public DeviceType deviceType { get; set; }

		public bool isPS4PadCompatible { get; private set; }

		public bool isXboxCompatible { get; private set; }

		public bool isVibroSupport { get; private set; }

		public ControllerMetaInfo(Controller controller, string controllerName)
		{
			rewiredController = controller;
			name = controllerName;
			isPS4PadCompatible = InputManager.IsPS4PadCompatible(controller);
			isXboxCompatible = InputManager.IsXboxCompatible(controller);
			userId = InputManager.GetUserId(controller);
			deviceType = InputSettings.DeviceProfile.DetermineDeviceType(controller, controllerName);
			isVibroSupport = (controller as Joystick)?.supportsVibration ?? false;
			deviceData = -1;
			disableFFB = deviceType == DeviceType.Keyboard;
		}

		public override string ToString()
		{
			return $"{name}[{deviceType}]";
		}

		public void SetDefaultDeviceType()
		{
			deviceType = InputSettings.DeviceProfile.DetermineDeviceType(rewiredController, name, ignoreSaves: true);
		}
	}
}
