using System;
using UnityEngine;

namespace Binding.Base
{
	public interface IBindingTarget
	{
		string Name { get; }

		GameObject selectedItem { get; set; }

		GameObject hoveredItem { get; set; }

		event Action OnChangeSelected;

		event Action OnChangeHovered;

		event Action OnForceUpdateProperties;

		BaseProperty FindProperty(string propertyName, int deep);
	}
}
