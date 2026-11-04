using UnityEngine;

public class AnimatorStateKeeper : MonoBehaviour
{
	public AnimatorStateInfo animatorStateInfo;

	private Animator animator;

	private bool saved;

	private void Start()
	{
		animator = GetComponent<Animator>();
	}

	private void OnEnable()
	{
		if (saved && animator != null)
		{
			animator.Play(animatorStateInfo.shortNameHash, 0, animatorStateInfo.normalizedTime);
		}
	}

	private void OnDisable()
	{
		if (animator != null)
		{
			animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
			saved = true;
		}
	}
}
