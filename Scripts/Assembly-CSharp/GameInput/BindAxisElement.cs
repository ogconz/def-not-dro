using DI;
using PrefsModels;
using Rewired;
using SimpleJSON;
using UnityEngine;

namespace GameInput
{
	public class BindAxisElement : BindElement
	{
		public const float MOUSE_AXIS_SCALE_FACTOR = 1000f;

		public const string MOUSE_HORIZONTAL_AXIS_TITLE = "mouse horizontal";

		public const string MOUSE_VERTICAL_AXIS_TITLE = "mouse vertical";

		public const string ACCELEROMETER_AXIS_TITLE = "Gyroscope Horizontal Axis";

		protected float m_value;

		protected float m_slideValue;

		protected bool m_isPressed;

		protected bool m_isPrevPressed;

		private float? m_minValueCached;

		private float? m_maxValueCached;

		private GlobalModel m_globalModel;

		[JsonIgnore]
		public Controller.Axis inputAxis { get; set; }

		public override float lastTimeValueChanged
		{
			get
			{
				if (inputAxis == null)
				{
					return 0f;
				}
				return (float)inputAxis.lastTimeValueChanged;
			}
		}

		public bool enabled { get; set; }

		public bool invert { get; set; }

		public float minValue { get; set; }

		public float maxValue { get; set; }

		public BindAxisElement(string axis, float min, float max, bool invert, ActionType bindedAction)
		{
			inputAxis = null;
			base.elementName = axis;
			this.invert = invert;
			minValue = min;
			maxValue = max;
			base.bindedAction = bindedAction;
			enabled = true;
		}

		public BindAxisElement()
		{
			invert = false;
			enabled = true;
		}

		public override float GetValueAsFloat()
		{
			return m_value;
		}

		public override float GetRAWValueAsFloat()
		{
			return inputAxis.value;
		}

		public override bool IsHold()
		{
			return m_isPressed;
		}

		public override bool IsPressed()
		{
			if (m_isPressed)
			{
				return !m_isPrevPressed;
			}
			return false;
		}

		public override bool IsReleased()
		{
			if (!m_isPressed)
			{
				return m_isPrevPressed;
			}
			return false;
		}

		public override bool IsActive()
		{
			int num;
			if (inputAxis.lastTimeValueChanged != ReInput.time.unscaledTime)
			{
				if (Mathf.Abs(InputManager.instance.lastController.TryGetDS4AccelerationX()) > 0.01f)
				{
					num = ((deviceInfo.deviceType == DeviceType.Gamepad) ? 1 : 0);
					if (num != 0)
					{
						goto IL_0049;
					}
				}
				else
				{
					num = 0;
				}
				goto IL_005a;
			}
			num = 1;
			goto IL_0049;
			IL_0049:
			m_slideValue = inputAxis.value;
			goto IL_005a;
			IL_005a:
			return (byte)num != 0;
		}

		private void UpdateMouseMoveBindValue(bool isHorizontal)
		{
			Vector2 screenPosition = ReInput.controllers.Mouse.screenPosition;
			int num = (isHorizontal ? Screen.width : Screen.height);
			float num2 = (isHorizontal ? screenPosition.x : screenPosition.y);
			num2 -= (float)num / 2f;
			float num3 = (m_maxValueCached.HasValue ? m_maxValueCached.Value : maxValue);
			float num4 = (m_minValueCached.HasValue ? m_minValueCached.Value : minValue);
			num3 *= 1000f;
			num4 *= 1000f;
			if (num2 < num4)
			{
				UpdateMouseCachedInputRange(num2, ref m_minValueCached, ref m_maxValueCached, maxValue, num4);
			}
			if (num2 > num3)
			{
				UpdateMouseCachedInputRange(num2, ref m_maxValueCached, ref m_minValueCached, minValue, num3);
			}
			float num5 = ((m_maxValueCached ?? 0f) - (m_minValueCached ?? 0f)) / 2f + (m_minValueCached ?? 0f);
			num5 *= 1000f;
			bool num6 = num2 > num5;
			float num7 = (num6 ? 1f : (-1f));
			num7 *= (invert ? (-1f) : 1f);
			float num8 = (num6 ? (maxValue * 1000f) : (minValue * 1000f));
			if (Mathf.Approximately(num8, 0f))
			{
				m_value = 0.5f;
			}
			else
			{
				m_value = Mathf.Clamp01((1f + num7 * (num2 - num5) / num8) / 2f);
			}
		}

		private void UpdateMouseCachedInputRange(float offsettedMousePos, ref float? firstRangeValue, ref float? secondRangeValue, float defaultSecondRangeValue, float prevRange)
		{
			firstRangeValue = offsettedMousePos / 1000f;
			secondRangeValue = (secondRangeValue.HasValue ? secondRangeValue : new float?(defaultSecondRangeValue));
			secondRangeValue -= (prevRange - offsettedMousePos) / 1000f;
		}

		public void ResetCacheInputRange()
		{
			m_minValueCached = minValue;
			m_maxValueCached = maxValue;
		}

		private void UpdateMouseMoveBindDelta(bool isHorizontal, float axisValue)
		{
			Vector2 screenPosition = ReInput.controllers.Mouse.screenPosition;
			float num = (isHorizontal ? screenPosition.x : screenPosition.y);
			int num2 = (isHorizontal ? Screen.width : Screen.height);
			float a = ((num / (float)num2 >= 0.5f) ? maxValue : minValue);
			float num3 = (invert ? (-1f) : 1f);
			if (Mathf.Approximately(a, 0f))
			{
				m_value = 0.5f;
				return;
			}
			float num4 = ((minValue < maxValue) ? Mathf.Clamp(axisValue, minValue, maxValue) : Mathf.Clamp(axisValue, maxValue, minValue));
			m_value = Mathf.Clamp01(m_value + num3 * num4 * Time.deltaTime);
		}

		public override void Update()
		{
			float value = inputAxis.value;
			m_slideValue = Mathf.MoveTowards(m_slideValue, value, 0.12f * Time.deltaTime);
			bool isHorizontal;
			if (deviceInfo.rewiredType == ControllerType.Mouse && ((isHorizontal = base.elementName.ToLower().Contains("mouse horizontal")) || base.elementName.ToLower().Contains("mouse vertical")))
			{
				if (m_globalModel == null)
				{
					m_globalModel = DependencyInjector.Resolve<GamePrefs>().globals;
				}
				GlobalModel globalModel = m_globalModel;
				if (globalModel != null && globalModel.moveMouseAxisType == GlobalModel.MouseMoveType.Coord)
				{
					UpdateMouseMoveBindValue(isHorizontal);
				}
				else
				{
					UpdateMouseMoveBindDelta(isHorizontal, value);
				}
				return;
			}
			float value2 = value;
			if (base.elementName == "Gyroscope Horizontal Axis")
			{
				value2 = InputManager.instance.lastController.TryGetDS4AccelerationX();
			}
			if (IsSystemAction())
			{
				value2 = inputAxis.value;
			}
			float num = ((minValue < maxValue) ? Mathf.Clamp(value2, minValue, maxValue) : Mathf.Clamp(value2, maxValue, minValue));
			float num2 = Mathf.Abs(maxValue - minValue);
			float num3 = ((num2 > 0f) ? (Mathf.Abs(num - minValue) / num2) : 0f);
			m_value = Mathf.Clamp01(invert ? (1f - num3) : num3);
			m_isPrevPressed = m_isPressed;
			if (m_value > 0.8f)
			{
				m_isPressed = true;
			}
			if (m_value < 0.6f)
			{
				m_isPressed = false;
			}
		}

		public override void Apply(Action action)
		{
			float num = ((action.valueType != ActionValueType.Full) ? m_value : (2f * m_value - 1f));
			action.value = action.signalCurveAffector?.Invoke(num) ?? num;
			action.pressed = action.pressed || IsPressed();
			action.released = action.released || IsReleased();
		}

		public float GetCurvedValue(Action action)
		{
			if (action.valueType == ActionValueType.Full)
			{
				if (action.signalCurveAffector != null)
				{
					return 0.5f * (action.signalCurveAffector(2f * m_value - 1f) + 1f);
				}
				return m_value;
			}
			return action.signalCurveAffector?.Invoke(m_value) ?? m_value;
		}

		public override bool IsEqualElement(BindElement action)
		{
			return false;
		}

		public override void Assign(BindElement action)
		{
			if (action is BindAxisElement bindAxisElement)
			{
				base.elementName = bindAxisElement.elementName;
				elementActionResource = bindAxisElement.elementActionResource;
				deviceInfo = bindAxisElement.deviceInfo;
			}
		}
	}
}
