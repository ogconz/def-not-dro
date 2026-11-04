using UnityEngine;

[CreateAssetMenu(fileName = "SpeedControlZonesConfig", menuName = "Other/SpeedControlZonesConfig")]
public class SpeedControlConfig : ScriptableObject
{
	[SerializeField]
	[Tooltip("Time for 'CriticalBraking Flag'")]
	private float m_coolDownTime;

	[SerializeField]
	[Tooltip("Time (in sec) for max deltaV to become 0 if not braking")]
	private float m_deltaDecreseTimeSec = 1f;

	[SerializeField]
	[Tooltip("Max braking acceleration in KMPH/sec")]
	private float m_maxBrakeAccel;

	public float coolDownTime => m_coolDownTime;

	public float deltaDecreaseTimeSec => m_deltaDecreseTimeSec;

	public float maxBrakeAccel => m_maxBrakeAccel;
}
