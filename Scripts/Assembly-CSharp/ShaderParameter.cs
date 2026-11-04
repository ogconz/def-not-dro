using System.Xml.Serialization;
using UnityEngine;

[XmlInclude(typeof(ShaderParameterInt))]
[XmlInclude(typeof(ShaderParameterFloat))]
[XmlInclude(typeof(ShaderParameterColor))]
public abstract class ShaderParameter
{
	[XmlAttribute]
	public string name;

	public static ShaderParameter CreateFromMaterial<T>(Material mat, string paramName)
	{
		ShaderParameter shaderParameter = null;
		if (typeof(T) == typeof(ShaderParameterInt))
		{
			shaderParameter = new ShaderParameterInt
			{
				value = mat.GetInt(paramName)
			};
		}
		else if (typeof(T) == typeof(ShaderParameterFloat))
		{
			shaderParameter = new ShaderParameterFloat
			{
				value = mat.GetFloat(paramName)
			};
		}
		else if (typeof(T) == typeof(ShaderParameterColor))
		{
			shaderParameter = new ShaderParameterColor
			{
				value = mat.GetColor(paramName)
			};
		}
		else
		{
			GameLog.Print(AlertLevel.Error, "CarVisualManager", "Unsupported ShaderParameter's type: " + typeof(T).Name);
		}
		shaderParameter.name = paramName;
		return shaderParameter;
	}
}
