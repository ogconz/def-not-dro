using UnityEngine;
using UnityEngine.Events;

namespace HUD
{
	public class EventHandler : MonoBehaviour
	{
		[SerializeField]
		private UnityEvent OnInvoke;

		public void InvokeTrigger()
		{
			OnInvoke.Invoke();
		}
	}
}
