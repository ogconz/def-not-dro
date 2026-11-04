using Binding;
using Binding.Base;
using UnityEngine;

public class UIDebugGitInfo : BehaviourBindingTarget
{
	private StringProperty m_gitInfo = new StringProperty("");

	[SerializeField]
	private GitInfoAsset m_gitInfoAsset;

	private void Awake()
	{
		base.gameObject.SetActive(value: false);
	}
}
