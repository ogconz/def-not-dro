using UnityEngine;

[CreateAssetMenu(fileName = "CollisionsConfigXDS", menuName = "Other/CollisionsConfigXDS")]
public class CollisionsConfigXDS : ScriptableObject
{
	public enum MassRatioMode
	{
		Off = 0,
		On = 1,
		ClampLowest = 2
	}

	[SerializeField]
	[Tooltip("Use old impact calculations")]
	private bool m_useOld;

	[SerializeField]
	[Tooltip("Max allowed collision impact for two car colliding. (Default = 4)")]
	private float m_maxCarCollisionImpact = 4f;

	[SerializeField]
	[Tooltip("Default max collision impact when colliding with track object. (Default = 7)")]
	private float m_maxWallCollisionImpact = 7f;

	[SerializeField]
	[Tooltip("Max collision impact when colliding with track object after colliding with chaser within 'Blame Time After Contact' (Default = 7)")]
	private float m_blameMaxWallCollisionImpact = 7f;

	[SerializeField]
	[Tooltip("Max time (sec) to blame chaser for leader's DQ after cars' contact (Default = 3)")]
	private float m_blameTimeAfterContact = 3f;

	[Space]
	[SerializeField]
	[Tooltip("Use cars' mass ratio when calculating impact\n-Off\n-On\n-Clamp Lowest - applies only if player car is havier\n(Default = off)")]
	private MassRatioMode m_massRatioMode;

	public bool useOld => m_useOld;

	public float maxCarCollisionImpact => m_maxCarCollisionImpact;

	public float maxWallCollisionImpact => m_maxWallCollisionImpact;

	public float blameMaxWallCollisionImpact => m_blameMaxWallCollisionImpact;

	public float blameTimeAfterContact => m_blameTimeAfterContact;

	public MassRatioMode massRatioMode => m_massRatioMode;

	public static CollisionsConfigXDS Load(string resourcePath)
	{
		return ResourcesWrapper.Load<CollisionsConfigXDS>(resourcePath);
	}
}
