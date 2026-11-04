using UnityEngine;

public class Waypoint : MonoBehaviour
{
	public WaypointMinimapItem minimapItem { get; private set; }

	public void SetMinimapRelyingItem(WaypointMinimapItem item)
	{
		minimapItem = item;
	}
}
