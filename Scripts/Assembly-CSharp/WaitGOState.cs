using UnityEngine;

public class WaitGOState : BaseState
{
	[SerializeField]
	private GameObject m_targetGO;

	public void PushWaiting(GameObject target)
	{
		m_targetGO = target;
		States.Push<WaitGOState>();
	}

	public override void OnUpdate()
	{
		if (m_targetGO == null)
		{
			States.Pop();
		}
	}
}
