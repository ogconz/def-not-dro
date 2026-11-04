using System.Collections;
using UnityEngine;

namespace Utils
{
	internal class TaskManager : MonoBehaviour
	{
		public class TaskState
		{
			public delegate void FinishedHandler(bool manual);

			private IEnumerator m_coroutine;

			private bool m_running;

			private bool m_paused;

			private bool m_stopped;

			public bool running => m_running;

			public bool paused => m_paused;

			public event FinishedHandler Finished;

			public TaskState(IEnumerator c)
			{
				m_coroutine = c;
			}

			public void Pause()
			{
				m_paused = true;
			}

			public void Unpause()
			{
				m_paused = false;
			}

			public void Start()
			{
				m_running = true;
				m_singleton.StartCoroutine(CallWrapper());
			}

			public void Stop()
			{
				m_stopped = true;
				m_running = false;
			}

			private IEnumerator CallWrapper()
			{
				yield return null;
				IEnumerator e = m_coroutine;
				while (m_running)
				{
					if (m_paused)
					{
						yield return null;
					}
					else if (e != null && e.MoveNext())
					{
						yield return e.Current;
					}
					else
					{
						m_running = false;
					}
				}
				this.Finished?.Invoke(m_stopped);
			}
		}

		private static TaskManager m_singleton;

		public static TaskState CreateTask(IEnumerator coroutine)
		{
			if (m_singleton == null)
			{
				m_singleton = new GameObject("TaskManager").AddComponent<TaskManager>();
			}
			return new TaskState(coroutine);
		}
	}
}
