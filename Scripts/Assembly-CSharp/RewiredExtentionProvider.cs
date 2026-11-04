using Rewired;
using Rewired.ControllerExtensions;

public static class RewiredExtentionProvider
{
	public static float TryGetDS4AccelerationX(this Controller controller)
	{
		if (controller == null)
		{
			return 0f;
		}
		return controller.GetExtension<IDualShock4Extension>()?.GetAccelerometerValue().x ?? 0f;
	}
}
