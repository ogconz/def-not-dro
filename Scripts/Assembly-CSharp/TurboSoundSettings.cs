using UnityEngine;
using Utils.InterpolatedSurface.AssetHolder;

public class TurboSoundSettings : ScriptableObject
{
	[Header("Turbo RPM Settings")]
	[SerializeField]
	[Tooltip("Describes function argumented by values of engine load and turbo boost and returning corresponding RPM value")]
	private BaseNormalizedSurfaceHolder m_surfaceHolder;

	[SerializeField]
	private float m_acceleratingInertia = 1f;

	[SerializeField]
	private float m_brakingInertia = 1f;

	[Header("Blow-off settings")]
	[SerializeField]
	private float m_blowOffLagSec = 0.2f;

	[SerializeField]
	[Range(0f, 1f)]
	private float m_blowOffMinValue = 0.05f;

	public static TurboSoundSettings Default => ScriptableObject.CreateInstance<TurboSoundSettings>();

	public float acceleratingInertia => m_acceleratingInertia;

	public float brakingInertia => m_brakingInertia;

	public float blowOffLagSec => m_blowOffLagSec;

	public float blowOffMinValue => m_blowOffMinValue;

	public float CalculateTurboRPM(float normolizedPower, float normalizedBoost)
	{
		return m_surfaceHolder.StoredSurface.Evaluate(normolizedPower, normalizedBoost);
	}
}
