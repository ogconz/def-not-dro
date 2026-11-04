using UnityEngine;

namespace CarX.Gameplay.ClippingZone
{
	public class ClippingFailGate : MonoBehaviour
	{
		public BaseClipping host;

		public void Capture()
		{
			host = GetComponentInParent<BaseClipping>();
		}

		private void OnTriggerEnter(Collider other)
		{
			host.OnFailGate(other);
		}
	}
}
