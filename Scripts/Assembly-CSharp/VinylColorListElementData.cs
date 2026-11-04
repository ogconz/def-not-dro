using Binding;
using UnityEngine;

public class VinylColorListElementData : BaseListElementData
{
	private ColorProperty m_color = new ColorProperty();

	private int m_material;

	public int material
	{
		get
		{
			return m_material;
		}
		set
		{
			m_material = value;
		}
	}

	public Color color
	{
		get
		{
			return m_color.value;
		}
		set
		{
			m_color.value = value;
		}
	}

	public VinylColorListElementData(Color color, int material = 0)
	{
		m_color.value = color;
		m_material = material;
	}
}
