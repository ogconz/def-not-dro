using System;

[Serializable]
public class EngineParamShiftInfo : EngineParamInfo
{
	public float multiplier;

	public float shift;

	public new EngineParamShiftInfo Copy()
	{
		if (!(base.Copy() is EngineParamShiftInfo engineParamShiftInfo))
		{
			return null;
		}
		engineParamShiftInfo.multiplier = multiplier;
		engineParamShiftInfo.shift = shift;
		return engineParamShiftInfo;
	}
}
