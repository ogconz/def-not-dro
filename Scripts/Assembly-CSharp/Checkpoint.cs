using System;
using UnityEngine;

[AddComponentMenu("Checkpoints/Checkpoint")]
public class Checkpoint : BaseObjectPositionedOnSpline, IComparable<Checkpoint>
{
	public int weightXDS;

	public bool isBlankXDS;

	public int sideChangeCountXDS = 1;

	public float sideChangeFactorXDS = 1f;

	public float minSpeedXDS = 30f;

	public bool isVisited;

	public int zone;

	public Action<Checkpoint, RaceCar> OnCarEnterEvent;

	public static Checkpoint Create(int index, TrackSpline spline, float distance, Vector3 size)
	{
		GameObject obj = new GameObject("Checkpoint " + index);
		BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
		boxCollider.center = Vector3.zero;
		boxCollider.size = size;
		boxCollider.isTrigger = true;
		Checkpoint checkpoint = obj.AddComponent<Checkpoint>();
		checkpoint.spline = spline;
		checkpoint.distance = distance;
		return checkpoint;
	}

	public void Awake()
	{
		MinimapItem minimapItem = GetComponent<MinimapItem>();
		if (minimapItem == null)
		{
			minimapItem = base.gameObject.AddComponent<MinimapItem>();
		}
		minimapItem.host = this;
		minimapItem.typeIndex = 2;
	}

	public int CompareTo(Checkpoint other)
	{
		if (other == null)
		{
			return 1;
		}
		return base.distance.CompareTo(other.distance);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (OnCarEnterEvent != null)
		{
			RaceCar component = other.transform.root.GetComponent<RaceCar>();
			OnCarEnterEvent(this, component);
		}
	}

	[ContextMenu("Reposition")]
	private void RepositionContextMenuHandle()
	{
		Reposition();
	}
}
