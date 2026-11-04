using System;
using System.Collections.Generic;
using CarX;
using GameInput;

[Serializable]
public class ControlsSetup : ScriptableSingleton<ControlsSetup>
{
	[Serializable]
	public class IntPair
	{
		public ParamInt paramType;

		public int value;
	}

	[Serializable]
	public class FloatPair
	{
		public ParamFloat paramType;

		public float value;
	}

	[Serializable]
	public class Setup
	{
		public SteeringMode steeringMode;

		public List<FloatPair> floatParams;

		public List<IntPair> intParams;
	}

	[Serializable]
	public class DefaultSetup
	{
		public List<FloatPair> floatParams;

		public List<IntPair> intParams;
	}

	public DefaultSetup defaults;

	public List<Setup> custom;

	private List<Setup> m_runtimeOverrides;

	public SteeringMode lastSteeringMode { get; protected set; }

	public void ResetOverrides()
	{
		m_runtimeOverrides = new List<Setup>();
	}

	public void ApplyDefaultsParams()
	{
		if (defaults != null)
		{
			ApplyParams(defaults.floatParams, defaults.intParams);
		}
	}

	public bool ApplyCustomParams(SteeringMode steeringMode)
	{
		if (custom != null)
		{
			Setup setup = custom.Find((Setup s) => s.steeringMode == steeringMode);
			if (setup != null)
			{
				ApplyParams(setup.floatParams, setup.intParams);
				return true;
			}
		}
		return false;
	}

	private static void ApplyParams(List<FloatPair> floatParams, List<IntPair> intParams)
	{
		floatParams?.ForEach(delegate(FloatPair param)
		{
			Simulator.SetParamFloat(param.paramType, param.value);
		});
		intParams?.ForEach(delegate(IntPair param)
		{
			Simulator.SetParamInt(param.paramType, param.value);
		});
	}

	public void ApplyParams(SteeringMode steeringMode)
	{
		lastSteeringMode = steeringMode;
		if (custom == null)
		{
			ApplyDefaultsParams();
			return;
		}
		if (!ApplyCustomParams(steeringMode))
		{
			ApplyDefaultsParams();
		}
		ApplyParamsFromSaves(steeringMode);
		ApplyOverrideParams(steeringMode);
	}

	private void ApplyParamsFromSaves(SteeringMode steeringMode)
	{
		List<Setup> customUnlockedPhysicsParams = InputManager.settings.Params.customUnlockedPhysicsParams;
		if (customUnlockedPhysicsParams == null)
		{
			TryForceApplyLogitechSpeedModeToAccel(steeringMode);
			UIInputSettingsContext.TryOverrideAccelThrustT248(null, needCreateSaveData: false);
			return;
		}
		UIInputSettingsContext.TryOverrideAccelThrustT248();
		Setup setup = customUnlockedPhysicsParams.Find((Setup setup2) => setup2.steeringMode == steeringMode);
		if (setup != null)
		{
			ApplyParams(setup.floatParams, setup.intParams);
		}
		UIInputSettingsContext.TryApplyNonCustomPresetBySteeringMode(steeringMode, ApplyParams);
	}

	private void TryForceApplyLogitechSpeedModeToAccel(SteeringMode steeringMode)
	{
		if (steeringMode != SteeringMode.Axis && steeringMode != SteeringMode.Buttons)
		{
			InputSettings.Parameters parameters = InputManager.settings.Params;
			if ((steeringMode != SteeringMode.Wheel || !parameters.wheelParams.isThrustAccelOverrided) && (steeringMode != SteeringMode.LogitechWheel || !parameters.wheelParams.isLogitechAccelOverrided))
			{
				int builtInParamInt = GetBuiltInParamInt(steeringMode, ParamInt.FFLogitechSpeedMod);
				OverrideParamFloat(steeringMode, ParamFloat.FFAccel, builtInParamInt);
			}
		}
	}

	private void ApplyOverrideParams(SteeringMode steeringMode)
	{
		Setup setup = m_runtimeOverrides.Find((Setup s) => s.steeringMode == steeringMode);
		if (setup != null)
		{
			ApplyParams(setup.floatParams, setup.intParams);
		}
	}

	public void OverrideParamFloat(SteeringMode steerMode, ParamFloat paramType, float value)
	{
		SaveOverrideParam(steerMode, paramType, value);
		if (lastSteeringMode == steerMode)
		{
			Simulator.SetParamFloat(paramType, value);
		}
	}

	public void OverrideParamInt(SteeringMode steerMode, ParamInt paramType, int value)
	{
		SaveOverrideParam(steerMode, paramType, value);
		if (lastSteeringMode == steerMode)
		{
			Simulator.SetParamInt(paramType, value);
		}
	}

	private void SaveOverrideParam(SteeringMode steerMode, ParamFloat paramType, float value)
	{
		Setup orCreateOverrideSetup = GetOrCreateOverrideSetup(steerMode);
		FloatPair floatPair = orCreateOverrideSetup.floatParams.Find((FloatPair p) => p.paramType == paramType);
		if (floatPair == null)
		{
			floatPair = new FloatPair
			{
				paramType = paramType
			};
			orCreateOverrideSetup.floatParams.Add(floatPair);
		}
		floatPair.value = value;
		if (value == GetBuildInParamFloat(steerMode, paramType))
		{
			orCreateOverrideSetup.floatParams.Remove(floatPair);
		}
	}

	private void SaveOverrideParam(SteeringMode steerMode, ParamInt paramType, int value)
	{
		Setup orCreateOverrideSetup = GetOrCreateOverrideSetup(steerMode);
		IntPair intPair = orCreateOverrideSetup.intParams.Find((IntPair p) => p.paramType == paramType);
		if (intPair == null)
		{
			intPair = new IntPair
			{
				paramType = paramType
			};
			orCreateOverrideSetup.intParams.Add(intPair);
		}
		intPair.value = value;
		if (value == GetBuiltInParamInt(steerMode, paramType))
		{
			orCreateOverrideSetup.intParams.Remove(intPair);
		}
	}

	private Setup GetOrCreateOverrideSetup(SteeringMode steerMode)
	{
		Setup setup = m_runtimeOverrides.Find((Setup s) => s.steeringMode == steerMode);
		if (setup == null)
		{
			setup = new Setup
			{
				steeringMode = steerMode,
				floatParams = new List<FloatPair>(),
				intParams = new List<IntPair>()
			};
			m_runtimeOverrides.Add(setup);
		}
		return setup;
	}

	public float GetBuildInParamFloat(SteeringMode steerMode, ParamFloat param)
	{
		if (GetFromSetup(custom, steerMode, param, out var value))
		{
			return value;
		}
		if (GetFromList(defaults.floatParams, param, out var value2))
		{
			return value2;
		}
		return Simulator.GetParamFloat(param);
	}

	public int GetBuiltInParamInt(SteeringMode steerMode, ParamInt param)
	{
		if (GetFromSetup(custom, steerMode, param, out var value))
		{
			return value;
		}
		if (GetFromList(defaults.intParams, param, out var value2))
		{
			return value2;
		}
		return Simulator.GetParamInt(param);
	}

	public void GetCurrentAndDefaultInt(SteeringMode steerMode, ParamInt param, out int current, out int defaultValue)
	{
		defaultValue = GetBuiltInParamInt(steerMode, param);
		current = GetCurrentInt(steerMode, param);
	}

	public float GetCurrentFloat(SteeringMode steerMode, ParamFloat param)
	{
		if (GetFromSetup(m_runtimeOverrides, steerMode, param, out var value))
		{
			return value;
		}
		return GetBuildInParamFloat(steerMode, param);
	}

	private bool GetFromSetup(List<Setup> m_runtimeOverrides, SteeringMode steerMode, ParamFloat param, out float value)
	{
		Setup setup = m_runtimeOverrides.Find((Setup s) => s.steeringMode == steerMode);
		if (setup != null)
		{
			return GetFromList(setup.floatParams, param, out value);
		}
		value = 0f;
		return false;
	}

	private bool GetFromList(List<FloatPair> list, ParamFloat param, out float value)
	{
		FloatPair floatPair = list.Find((FloatPair fp) => fp.paramType == param);
		if (floatPair != null)
		{
			value = floatPair.value;
			return true;
		}
		value = 0f;
		return false;
	}

	public int GetCurrentInt(SteeringMode steerMode, ParamInt param)
	{
		if (GetFromSetup(m_runtimeOverrides, steerMode, param, out var value))
		{
			return value;
		}
		return GetBuiltInParamInt(steerMode, param);
	}

	private bool GetFromSetup(List<Setup> m_runtimeOverrides, SteeringMode steerMode, ParamInt param, out int value)
	{
		Setup setup = m_runtimeOverrides.Find((Setup s) => s.steeringMode == steerMode);
		if (setup != null)
		{
			return GetFromList(setup.intParams, param, out value);
		}
		value = 0;
		return false;
	}

	private bool GetFromList(List<IntPair> list, ParamInt param, out int value)
	{
		IntPair intPair = list.Find((IntPair fp) => fp.paramType == param);
		if (intPair != null)
		{
			value = intPair.value;
			return true;
		}
		value = 0;
		return false;
	}
}
