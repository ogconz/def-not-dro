using Binding.Base;
using UnityEngine;

namespace Minimaps
{
	public class UIMapBaseContext<T> : BaseBindingBehaviourTarget where T : BaseMinimapVisualizer
	{
		[SerializeField]
		protected T m_visualizer;

		public T visualizer => m_visualizer;
	}
}
