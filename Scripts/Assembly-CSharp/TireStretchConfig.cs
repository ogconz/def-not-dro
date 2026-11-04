using UnityEngine;

public class TireStretchConfig : ScriptableSingleton<TireStretchConfig>
{
	public float maxNegativeStretchOffset = -30f;

	public float maxPositiveStretchOffset = 50f;

	public float defaultStretchOffset = 10f;

	public AnimationCurve stretchCurve;
}
