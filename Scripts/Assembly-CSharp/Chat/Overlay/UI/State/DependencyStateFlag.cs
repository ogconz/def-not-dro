using System.Collections.Generic;

namespace Chat.Overlay.UI.State
{
	public class DependencyStateFlag : StateFlag
	{
		private readonly List<StateFlag> m_dependencies = new List<StateFlag>();

		private readonly DependencyType m_type;

		public DependencyStateFlag(DependencyType type, params StateFlag[] dependencies)
		{
			m_type = type;
			m_dependencies.AddRange(dependencies);
			foreach (StateFlag dependency in m_dependencies)
			{
				dependency.onChanged += Recalculate;
			}
			Recalculate(_: false);
		}

		private void Recalculate(bool _)
		{
			DependencyType type = m_type;
			if (type != DependencyType.Or && type == DependencyType.And)
			{
				CalculateAndFunc();
			}
			else
			{
				CalculateOrFunc();
			}
		}

		private void CalculateAndFunc()
		{
			bool value = true;
			foreach (StateFlag dependency in m_dependencies)
			{
				if (!dependency.Get())
				{
					value = false;
					break;
				}
			}
			Set(value);
		}

		private void CalculateOrFunc()
		{
			bool value = false;
			foreach (StateFlag dependency in m_dependencies)
			{
				if (dependency.Get())
				{
					value = true;
					break;
				}
			}
			Set(value);
		}
	}
}
