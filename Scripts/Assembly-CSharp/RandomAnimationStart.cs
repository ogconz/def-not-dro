using UnityEngine;

public class RandomAnimationStart : MonoBehaviour
{
	[SerializeField]
	private Animator m_animator;

	[SerializeField]
	private AnimationClip m_animation;

	[SerializeField]
	private int m_layer;

	private void OnEnable()
	{
		if (m_animator == null || m_animation == null)
		{
			GameLog.LogError("[RandomAnimationStart] Animator or Animation is not assigned.");
			return;
		}
		float normalizedTime = Random.Range(0f, m_animation.length);
		m_animator.Play(m_animation.name, m_layer, normalizedTime);
	}
}
