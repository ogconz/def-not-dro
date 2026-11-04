using System;
using UnityEngine;

[Serializable]
public class UniversalShaderParameter : ShaderParameter
{
	public enum ValueType
	{
		Unknown = 0,
		Float = 1,
		Color = 2,
		Int = 3
	}

	public ValueType valueType;

	public float floatValue;

	public Color colorValue;

	public int intValue;

	public void InitFrom(ShaderParameter parameter)
	{
		if (parameter == null)
		{
			valueType = ValueType.Unknown;
			return;
		}
		name = parameter.name;
		valueType = GetValueType(parameter);
		switch (valueType)
		{
		case ValueType.Float:
			floatValue = (parameter as ShaderParameterFloat).value;
			break;
		case ValueType.Color:
			colorValue = (parameter as ShaderParameterColor).value;
			break;
		case ValueType.Int:
			intValue = (parameter as ShaderParameterInt).value;
			break;
		}
	}

	public ShaderParameter GetTypedShaderParameter()
	{
		return valueType switch
		{
			ValueType.Float => new ShaderParameterFloat
			{
				name = name,
				value = floatValue
			}, 
			ValueType.Color => new ShaderParameterColor
			{
				name = name,
				value = colorValue
			}, 
			ValueType.Int => new ShaderParameterInt
			{
				name = name,
				value = intValue
			}, 
			_ => null, 
		};
	}

	private ValueType GetValueType(ShaderParameter parameter)
	{
		if (parameter is ShaderParameterFloat)
		{
			return ValueType.Float;
		}
		if (parameter is ShaderParameterColor)
		{
			return ValueType.Color;
		}
		if (parameter is ShaderParameterInt)
		{
			return ValueType.Int;
		}
		return ValueType.Unknown;
	}
}
