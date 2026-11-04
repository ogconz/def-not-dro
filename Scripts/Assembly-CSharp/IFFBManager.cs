using GameInput;
using Rewired;

public interface IFFBManager
{
	IForceFeedbackSystem feedback { get; }

	void Initialize();

	void Release();

	void OnControllerListChanged(Controller addedController, Controller removedController);

	void OnEnable();

	void OnDisable();

	void OnAppFocusChanged(bool focusState);

	void OnUpdateDInputSettings();

	void OnFailedCurrentllySupported(DeviceType lastSteeringDeviceType, ControllerMetaInfo oldSteeringDevice, ControllerMetaInfo lastSteeringDevice);

	void Update();
}
