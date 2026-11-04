using UnityEngine;

[CreateAssetMenu(fileName = "KissTheWallCarConfig", menuName = "Other/KissTheWallCarConfig")]
public class KissTheWallCarConfig : ScriptableObject
{
	private const int DefaultStep = 20;

	private const float DefaultCheckAngle = 360f;

	[SerializeField]
	private int m_rayCount;

	[SerializeField]
	private float m_checkAngle = 360f;

	[SerializeField]
	private float m_minDriftAngle;

	[SerializeField]
	private float m_minDriftSpeed;

	[SerializeField]
	[Tooltip("Impulse in kNs (kilonewtons)")]
	private float m_maxImpactImpulse;

	[SerializeField]
	private LayerMask m_layerMask;

	[SerializeField]
	private float m_delayAfterAchieve;

	[SerializeField]
	private float m_timeToAchieve;

	[SerializeField]
	private float m_maxDistance;

	[SerializeField]
	private float m_detectDistance;

	[SerializeField]
	private string[] m_ignoredMaterials;

	public LayerMask layerMask => m_layerMask;

	public float delayAfterAchieve => m_delayAfterAchieve;

	public float timeToAchieve => m_timeToAchieve;

	public float detectDistance => m_detectDistance;

	public float maxDistance => m_maxDistance;

	public float minDriftAngle => m_minDriftAngle;

	public float maxImpactImpulse => m_maxImpactImpulse;

	public float maxImpactImpulseSqr => m_maxImpactImpulse * m_maxImpactImpulse;

	public float minDriftSpeed => m_minDriftSpeed;

	public float checkAngle => m_checkAngle;

	public int rayCount => m_rayCount;

	public string[] ignoredMaterials => m_ignoredMaterials;
}
