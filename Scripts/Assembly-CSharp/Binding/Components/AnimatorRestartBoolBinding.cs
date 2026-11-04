using UnityEngine;

namespace Binding.Components
{
	[RequireComponent(typeof(Animator))]
	public class AnimatorRestartBoolBinding : BoolBinding
	{
		private Animator m_animator;

		private void Awake()
		{
			m_animator = GetComponent<Animator>();
		}

		protected override void OnUpdateValue()
		{
			if (currentValue && (bool)m_animator)
			{
				m_animator.enabled = false;
				m_animator.enabled = true;
			}
		}
	}
}
