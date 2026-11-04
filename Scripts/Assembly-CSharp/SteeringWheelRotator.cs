using UnityEngine;

public class SteeringWheelRotator : MonoBehaviour
{
	private static readonly int m_animWheelRotation = Animator.StringToHash("WheelRotation");

	private Animator m_animator;

	private RaceCar m_raceCar;

	private void Start()
	{
		m_animator = GetComponent<Animator>();
		m_animator.speed = 0f;
		m_raceCar = GetComponentInParent<RaceCar>();
	}

	private void Update()
	{
		float normalizedTime = 0f;
		if ((bool)m_raceCar)
		{
			normalizedTime = m_raceCar.GetSmoothSteer() * -0.5f + 0.5f;
		}
		m_animator.Play(m_animWheelRotation, 0, normalizedTime);
	}
}
