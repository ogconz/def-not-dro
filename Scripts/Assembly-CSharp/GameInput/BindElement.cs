using System;
using SimpleJSON;

namespace GameInput
{
	public abstract class BindElement
	{
		[JsonIgnore]
		public ControllerMetaInfo deviceInfo;

		[JsonIgnore]
		public ActionElementResources.ElementActionResource elementActionResource;

		public ActionType bindedAction { get; set; }

		public string elementName { get; set; }

		[JsonIgnore]
		public string elementId
		{
			get
			{
				if (elementActionResource == null)
				{
					return elementName;
				}
				return elementActionResource.NameBinding;
			}
		}

		public abstract float lastTimeValueChanged { get; }

		public override string ToString()
		{
			return string.Format("{0} ({2},{3}) -> {1}", elementName, bindedAction, (elementActionResource != null) ? elementActionResource.NameBinding : elementId, (elementActionResource != null) ? elementActionResource.AltNameBinding : ".");
		}

		public bool IsBindElementName(string otherElementName)
		{
			if (elementActionResource != null)
			{
				return elementActionResource.IsBinding(otherElementName);
			}
			return InputManager.CompareControlElementName(elementName, otherElementName);
		}

		public void UpdateElementActionResource()
		{
			elementActionResource = ScriptableSingleton<ActionElementResources>.instance.FindElement(elementName);
		}

		public bool IsSystemAction()
		{
			return Action.IsSystemAction(bindedAction);
		}

		public virtual bool IsEqualElement(BindElement element)
		{
			if (element != null && deviceInfo == element.deviceInfo)
			{
				if (IsBindElementName(element.elementName))
				{
					return true;
				}
				if (element.IsBindElementName(elementName))
				{
					return true;
				}
			}
			return false;
		}

		public virtual void Assign(BindElement action)
		{
			throw new Exception("InputElement: assign operation for the base classes not allowed!");
		}

		public abstract float GetValueAsFloat();

		public abstract float GetRAWValueAsFloat();

		public abstract bool IsHold();

		public abstract bool IsPressed();

		public abstract bool IsReleased();

		public abstract bool IsActive();

		public abstract void Update();

		public abstract void Apply(Action action);
	}
}
