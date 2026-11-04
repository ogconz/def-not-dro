using System.Collections.Generic;
using CarX;

namespace GameInput
{
	public class StaticWheelPreset
	{
		public WheelPresetType wheelPreset;

		public List<ControlsSetup.FloatPair> floatParams = new List<ControlsSetup.FloatPair>();

		public List<ControlsSetup.IntPair> intParams = new List<ControlsSetup.IntPair>();

		public StaticWheelPreset(WheelPresetType wheelPreset)
		{
			this.wheelPreset = wheelPreset;
			switch (wheelPreset)
			{
			case WheelPresetType.Beginner:
				floatParams.Add(new ControlsSetup.FloatPair
				{
					paramType = ParamFloat.FFPredictWk,
					value = 3f
				});
				floatParams.Add(new ControlsSetup.FloatPair
				{
					paramType = ParamFloat.FFPredictSk,
					value = 1f
				});
				floatParams.Add(new ControlsSetup.FloatPair
				{
					paramType = ParamFloat.SteeringWheelAccelFF,
					value = 0f
				});
				floatParams.Add(new ControlsSetup.FloatPair
				{
					paramType = ParamFloat.FFAccel,
					value = 1f
				});
				intParams.Add(new ControlsSetup.IntPair
				{
					paramType = ParamInt.SteerSmoothLevel,
					value = 1
				});
				intParams.Add(new ControlsSetup.IntPair
				{
					paramType = ParamInt.FFLogitechSpeedMod,
					value = 1
				});
				break;
			case WheelPresetType.Pro:
				floatParams.Add(new ControlsSetup.FloatPair
				{
					paramType = ParamFloat.FFPredictWk,
					value = 0f
				});
				floatParams.Add(new ControlsSetup.FloatPair
				{
					paramType = ParamFloat.FFPredictSk,
					value = 0.5f
				});
				floatParams.Add(new ControlsSetup.FloatPair
				{
					paramType = ParamFloat.SteeringWheelAccelFF,
					value = 0.5f
				});
				floatParams.Add(new ControlsSetup.FloatPair
				{
					paramType = ParamFloat.FFAccel,
					value = 0.5f
				});
				intParams.Add(new ControlsSetup.IntPair
				{
					paramType = ParamInt.SteerSmoothLevel,
					value = 1
				});
				intParams.Add(new ControlsSetup.IntPair
				{
					paramType = ParamInt.FFLogitechSpeedMod,
					value = 1
				});
				break;
			}
		}
	}
}
