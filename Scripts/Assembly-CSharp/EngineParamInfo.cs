using System;

[Serializable]
public class EngineParamInfo
{
	public string name;

	public float value;

	public float min;

	public float max;

	public EngineParamInfo Copy()
	{
		return new EngineParamInfo
		{
			name = name,
			value = value,
			min = min,
			max = max
		};
	}
}
