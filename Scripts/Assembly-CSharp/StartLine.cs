using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Checkpoints/StartLine")]
public class StartLine : BaseObjectPositionedOnSpline
{
	private List<Action> m_lockedCarsWaiters = new List<Action>();

	public Action<RaceCar> OnCarEnterEvent;

	public static StartLine Create(TrackSpline spline, float distance, Vector3 size)
	{
		GameObject obj = new GameObject("StartLine");
		BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
		boxCollider.center = Vector3.zero;
		boxCollider.size = size;
		boxCollider.isTrigger = true;
		StartLine startLine = obj.AddComponent<StartLine>();
		startLine.spline = spline;
		startLine.distance = distance;
		return startLine;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (OnCarEnterEvent == null)
		{
			return;
		}
		RaceCar car = other.transform.root.GetComponent<RaceCar>();
		if ((bool)car && car.lockState)
		{
			Action waiter = null;
			waiter = delegate
			{
				if (!car.lockState)
				{
					TimeController.onFixedUpdate = (Action)Delegate.Remove(TimeController.onFixedUpdate, waiter);
					OnCarEnterEvent.SafeCall(car);
				}
			};
			TimeController.onFixedUpdate = (Action)Delegate.Combine(TimeController.onFixedUpdate, waiter);
		}
		else
		{
			OnCarEnterEvent.SafeCall(car);
		}
	}

	private void OnDisable()
	{
		ClearWaiters();
	}

	private void OnDestroy()
	{
		ClearWaiters();
	}

	private void ClearWaiters()
	{
		foreach (Action lockedCarsWaiter in m_lockedCarsWaiters)
		{
			TimeController.onFixedUpdate = (Action)Delegate.Remove(TimeController.onFixedUpdate, lockedCarsWaiter);
		}
	}

	[ContextMenu("Reposition")]
	private void RepositionContextMenuHandle()
	{
		Reposition();
	}
}
