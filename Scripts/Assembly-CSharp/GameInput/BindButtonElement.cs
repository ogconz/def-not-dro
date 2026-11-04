using Rewired;
using SimpleJSON;
using UnityEngine;

namespace GameInput
{
	public class BindButtonElement : BindElement
	{
		public BindButtonType bindType = BindButtonType.Positive;

		private float m_value;

		private bool m_isPressed;

		private bool m_isPrevPressed;

		private bool m_skipShiftCtrlButtons;

		[JsonIgnore]
		public Controller.Button inputButton { get; set; }

		public override float lastTimeValueChanged
		{
			get
			{
				if (inputButton == null)
				{
					return 0f;
				}
				return (float)inputButton.lastTimeStateChanged;
			}
		}

		public BindButtonElement(string button, ActionType bindedAction, BindButtonType bindType = BindButtonType.Positive, bool skipShiftCtrlButtons = false)
		{
			inputButton = null;
			base.elementName = button;
			base.bindedAction = bindedAction;
			this.bindType = bindType;
			m_skipShiftCtrlButtons = skipShiftCtrlButtons;
		}

		public BindButtonElement()
		{
		}

		public override float GetValueAsFloat()
		{
			return m_value;
		}

		public override float GetRAWValueAsFloat()
		{
			return m_value;
		}

		public override bool IsPressed()
		{
			if (m_isPressed)
			{
				return !m_isPrevPressed;
			}
			return false;
		}

		public override bool IsHold()
		{
			return m_isPressed;
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
			return m_isPressed != m_isPrevPressed;
		}

		public override void Update()
		{
			m_isPrevPressed = m_isPressed;
			m_isPressed = inputButton.value;
			if (m_skipShiftCtrlButtons && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
			{
				m_isPressed = false;
			}
			if (inputButton.value)
			{
				m_value = ((bindType == BindButtonType.Positive) ? 1f : (-1f));
			}
			else
			{
				m_value = 0f;
			}
		}

		public override void Apply(Action action)
		{
			action.value += m_value;
			action.pressed = action.pressed || IsPressed();
			action.released = action.released || IsReleased();
		}

		public override bool IsEqualElement(BindElement action)
		{
			if (action is BindButtonElement)
			{
				return base.IsEqualElement(action);
			}
			return false;
		}

		public override void Assign(BindElement action)
		{
			if (action is BindButtonElement bindButtonElement)
			{
				base.elementName = bindButtonElement.elementName;
				elementActionResource = bindButtonElement.elementActionResource;
				deviceInfo = bindButtonElement.deviceInfo;
			}
		}
	}
}
