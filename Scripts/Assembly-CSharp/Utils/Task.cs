using System.Collections;

namespace Utils
{
	public class Task
	{
		public delegate void FinishedHandler(bool manual);

		private TaskManager.TaskState m_task;

		public bool running => m_task.running;

		public bool paused => m_task.paused;

		public event FinishedHandler Finished;

		public Task(IEnumerator c, bool autoStart = true)
		{
			m_task = TaskManager.CreateTask(c);
			m_task.Finished += TaskFinished;
			if (autoStart)
			{
				Start();
			}
		}

		public void Start()
		{
			m_task.Start();
		}

		public void Stop()
		{
			m_task.Stop();
		}

		public void Pause()
		{
			m_task.Pause();
		}

		public void Unpause()
		{
			m_task.Unpause();
		}

		private void TaskFinished(bool manual)
		{
			this.Finished?.Invoke(manual);
		}
	}
}
