using HUD;
using UnityEngine;

namespace Minimaps
{
	public class InteriorMiniMapVisualizer : RotatableMapVisualizer
	{
		public RaceCar assignedCar { get; private set; }

		public void Initialize()
		{
			assignedCar = GetComponentInParent<RaceCar>();
		}

		protected override void UpdateWaypointItem(UIMinimapItem uiWaypointItem)
		{
			uiWaypointItem.angle = m_currentMapAngle;
			uiWaypointItem.gameObject.SetActive(uiWaypointItem.item.transform.position != Vector3.zero);
		}
	}
}
