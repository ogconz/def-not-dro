using System;

public class ActionHolderWParam<T> : ActionHolder
{
	private readonly Action<T> m_actionWParam;

	public T param;

	public ActionHolderWParam(float _timeToExecute, Action<T> action, T param)
		: base(_timeToExecute, null)
	{
		m_actionWParam = action;
		this.param = param;
	}

	public override void Call()
	{
		if (m_actionWParam == null)
		{
			return;
		}
		try
		{
			m_actionWParam(param);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "TimeController", "Exception in delayed callback: " + ex.ToString());
		}
	}
}
