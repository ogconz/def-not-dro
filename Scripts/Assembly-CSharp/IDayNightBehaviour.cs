using UnityEngine;

public abstract class IDayNightBehaviour : MonoBehaviour
{
	public abstract void ChangeToDay();

	public abstract void ChangeToNight(bool force = false);

	protected virtual void Start()
	{
		DayNightController.Register(this);
	}

	protected virtual void OnDestroy()
	{
		DayNightController.UnRegister(this);
	}
}
