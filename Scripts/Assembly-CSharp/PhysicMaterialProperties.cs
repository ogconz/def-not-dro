using System;

[Serializable]
public struct PhysicMaterialProperties
{
	public float friction;

	public float rollFriction;

	public float bumpMin;

	public float bumpMax;

	public float bumpScale;

	public float bump2Min;

	public float bump2Max;

	public float bump2Scale;

	public void SetProperties(CARXSurface surface)
	{
		surface.carxMaterial.SetParameters(surface.template.carxPhysicMaterial.surfaceType, friction, rollFriction, bumpMin, bumpMax, bumpScale, bump2Min, bump2Max, bump2Scale);
	}
}
