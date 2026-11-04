using Binding.Base;
using UnityEngine;

namespace Binding.Components
{
	public class RotationAxisBinding : BaseBinding<FloatProperty>
	{
		public enum AxisType
		{
			X = 0,
			Y = 1,
			Z = 2
		}

		[SerializeField]
		private AxisType m_axis;

		[SerializeField]
		private bool m_local;

		protected override void OnUpdateValue()
		{
			Vector3 eulerAngles = base.transform.eulerAngles;
			switch (m_axis)
			{
			case AxisType.X:
				SetAngle(base.property.value, eulerAngles.y, eulerAngles.z);
				break;
			case AxisType.Y:
				SetAngle(eulerAngles.x, base.property.value, eulerAngles.z);
				break;
			case AxisType.Z:
				SetAngle(eulerAngles.x, eulerAngles.y, base.property.value);
				break;
			}
		}

		private void SetAngle(float x, float y, float z)
		{
			Vector3 vector = new Vector3(x, y, z);
			if (m_local)
			{
				base.transform.localEulerAngles = vector;
			}
			else
			{
				base.transform.eulerAngles = vector;
			}
		}
	}
}
