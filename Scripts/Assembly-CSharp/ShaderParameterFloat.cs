using System.Xml.Serialization;

public class ShaderParameterFloat : ShaderParameter
{
	[XmlAttribute]
	public float value;
}
