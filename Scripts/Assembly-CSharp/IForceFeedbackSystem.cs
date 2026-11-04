using Rewired;

public interface IForceFeedbackSystem
{
	bool isInitiated { get; }

	bool IsThisDeviceController(Controller controller);

	bool IsFFBWheelExist();

	void SetConstantForce(float k);

	void PlayImpactEffect(float k);

	void PlayGrassSurfaceEffect(float k);

	void PlayGravelSurfaceEffect(float k);

	void StopEffects(bool instant);

	void Initialize();

	void Enable();

	void Disable();

	void Update();
}
