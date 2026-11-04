using System;
using System.Collections.Generic;
using GameInput;
using UnityEngine;

public class ActionElementResources : ScriptableSingleton<ActionElementResources>
{
	[Serializable]
	public class ElementActionResource
	{
		public string NameBinding;

		public string AltNameBinding;

		public bool ignoreRawInput;

		public string ShortName;

		public Sprite Default;

		public Sprite Xbox;

		public Sprite DualShock;

		public Sprite Switch;

		public bool IsBinding(string bindingElelementName)
		{
			if (InputManager.CompareControlElementName(bindingElelementName, NameBinding))
			{
				return true;
			}
			if (!string.IsNullOrEmpty(AltNameBinding) && InputManager.CompareControlElementName(bindingElelementName, AltNameBinding))
			{
				return true;
			}
			return false;
		}
	}

	public enum PlatformTypeElementAction
	{
		Default = 0,
		Xbox = 1,
		DualShock = 2,
		Switch = 3
	}

	public List<ElementActionResource> bindElements = new List<ElementActionResource>();

	public bool isEmpty => bindElements.Count == 0;

	public ElementActionResource FindElement(string nameElementAction)
	{
		foreach (ElementActionResource bindElement in bindElements)
		{
			if (bindElement.IsBinding(nameElementAction))
			{
				return bindElement;
			}
		}
		return null;
	}

	public Sprite GetActionSpriteByBindName(string nameBinding, PlatformTypeElementAction platformType)
	{
		ElementActionResource elementActionResource = bindElements.Find((ElementActionResource actionResource) => InputManager.CompareControlElementName(nameBinding, actionResource.NameBinding));
		return GetActionSpriteByPlatform(elementActionResource, platformType);
	}

	public Sprite GetBindActionSprite(BindElement bind, PlatformTypeElementAction platformType)
	{
		return GetActionSpriteByPlatform(bind?.elementActionResource, platformType);
	}

	private Sprite GetActionSpriteByPlatform(ElementActionResource elementActionResource, PlatformTypeElementAction platformType)
	{
		return platformType switch
		{
			PlatformTypeElementAction.Default => elementActionResource?.Default, 
			PlatformTypeElementAction.Xbox => elementActionResource?.Xbox, 
			PlatformTypeElementAction.DualShock => elementActionResource?.DualShock, 
			PlatformTypeElementAction.Switch => elementActionResource?.Switch, 
			_ => null, 
		};
	}
}
