using System.Xml.Serialization;

public class ShaderParameterInt : ShaderParameter
{
	[XmlAttribute]
	public int value;
}
