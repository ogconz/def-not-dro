using Binding;
using Binding.Base;

public class UIWaitOpponentContext : BaseBindingBehaviourTarget
{
	private BoolProperty m_isWaitingOpponent = new BoolProperty(false);

	public void SetNeedWaitOpponent(bool needWait)
	{
		m_isWaitingOpponent.value = needWait;
	}
}
