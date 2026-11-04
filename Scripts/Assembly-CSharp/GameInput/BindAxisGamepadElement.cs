using Rewired;
using SimpleJSON;
using UnityEngine;

namespace GameInput
{
	public class BindAxisGamepadElement : BindAxisElement
	{
		[JsonIgnore]
		public Controller.Axis inputAxisY;

		public string elementNameY;

		private float m_coeff;

		public BindAxisGamepadElement(string axisX, string axisY, float min, float max, bool invert, ActionType bindedAction)
			: base(axisX, min, max, invert, bindedAction)
		{
			inputAxisY = null;
			elementNameY = axisY;
		}

		public BindAxisGamepadElement()
		{
		}

		private void InterpolateStickValue()
		{
			float value = base.inputAxis.value;
			if (inputAxisY != null)
			{
				float value2 = inputAxisY.value;
				Vector2 vector = new Vector2(value, value2);
				float num = Vector2.Angle(vector, Vector2.right);
				if (num > 90f)
				{
					num = 180f - num;
				}
				if (!(num < 5f))
				{
					float num2 = Mathf.Clamp01(vector.sqrMagnitude);
					float num3 = ((m_value > 0.5f) ? 1 : (-1));
					num3 = ((num2 > 0.95f && num < 50f) ? num3 : 0f);
					float num4 = ((num3 != 0f) ? 0.5f : 4f);
					m_coeff = Mathf.Lerp(m_coeff, num3, Time.deltaTime * num4);
					m_value += m_coeff;
					m_value = Mathf.Clamp01(m_value);
				}
			}
		}
	}
}
