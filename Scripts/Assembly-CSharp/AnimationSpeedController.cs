using UnityEngine;

[RequireComponent(typeof(Animation))]
public class AnimationSpeedController : MonoBehaviour
{
	[SerializeField]
	private float m_speed = 1f;

	private void Start()
	{
		foreach (AnimationState item in GetComponent<Animation>())
		{
			item.speed = m_speed;
		}
	}
}
