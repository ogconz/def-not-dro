using UnityEngine;

public class GarlandAnimation : MonoBehaviour
{
	[SerializeField]
	private AnimationCurve m_animationCurve;

	private float m_animationCurveDuration;

	private float m_elapsedTime;

	protected virtual void ApplyFillAmount(float fillAmount)
	{
	}

	protected virtual void OnEnable()
	{
		UpdateAnimationCurveDuration();
	}

	protected virtual void OnDisable()
	{
	}

	protected virtual void Update()
	{
		if (m_animationCurveDuration != 0f)
		{
			m_elapsedTime = Mathf.Repeat(m_elapsedTime + Time.deltaTime, m_animationCurveDuration);
			float fillAmount = m_animationCurve.Evaluate(m_elapsedTime);
			ApplyFillAmount(fillAmount);
		}
	}

	private void UpdateAnimationCurveDuration()
	{
		m_animationCurveDuration = ((m_animationCurve.length != 0) ? m_animationCurve[m_animationCurve.length - 1].time : 0f);
	}
}
