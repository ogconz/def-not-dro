using System;

[Serializable]
public class NightSettings
{
	public bool volumetric;

	public bool shadows;

	public float fadeDistance = 300f;

	public int spotShadows = 4;
}
