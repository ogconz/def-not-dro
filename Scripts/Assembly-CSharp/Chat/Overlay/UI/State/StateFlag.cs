using System;

namespace Chat.Overlay.UI.State
{
	public class StateFlag : IDisposable
	{
		private bool m_value;

		public event Action<bool> onChanged;

		public StateFlag(bool value = false)
		{
			m_value = value;
		}

		~StateFlag()
		{
			Release();
		}

		public bool Get()
		{
			return m_value;
		}

		public void Set(bool value)
		{
			if (m_value != value)
			{
				m_value = value;
				this.onChanged.SafeCall(value);
			}
		}

		private void Release()
		{
			this.onChanged = null;
		}

		public void Dispose()
		{
			Release();
		}

		public static explicit operator bool(StateFlag flag)
		{
			return flag.Get();
		}
	}
}
