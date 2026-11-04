using UnityEngine;

namespace CarX.Gameplay.ClippingZone
{
	public class ClippingPointObject : MonoBehaviour
	{
		public BaseClippingPoint clippingPoint;

		private void OnCollisionEnter(Collision other)
		{
			if (!(clippingPoint == null) && BaseClipping.IsCar(other.collider))
			{
				clippingPoint.SetTouch(touch: true);
			}
		}
	}
}
