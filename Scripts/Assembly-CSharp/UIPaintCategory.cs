using System.Collections.Generic;
using Binding;
using Binding.Base;
using GameInput;
using UnityEngine;

public class UIPaintCategory : BaseBindingBehaviourTarget
{
	public readonly Property<bool> selected = new BoolProperty(false);

	public readonly Property<bool> ableToPaint = new BoolProperty(startValue: true);

	public readonly Property<Sprite> icon = new SpriteProperty();

	public readonly Property<bool> locked = new BoolProperty();

	public readonly Property<int> locklevel = new IntProperty();

	private UIPaintCustomizationContext m_context;

	public bool isLocked => locked.value;

	public UIPaintCustomizationContext.StateType stateType { get; private set; }

	public UIPaintCustomizationContext.BasePaintState paintState { get; private set; }

	public int selectedPartIndex { get; private set; }

	public void SetData(UIPaintCustomizationContext context, UIPaintCustomizationContext.BasePaintState basePaintState)
	{
		m_context = context;
		paintState = basePaintState;
		paintState.category = this;
		UpdateLockedState();
		icon.value = paintState.spriteIcon;
		stateType = paintState.type;
	}

	public void UpdateLockedState()
	{
		GamePrefs prefs = m_context.prefs;
		int lockLevelForFeature = prefs.globals.GetLockLevelForFeature(stateType.AsGameFeature());
		locked.value = prefs.playerProfile.level < lockLevelForFeature;
		if (locked.value)
		{
			locklevel.value = lockLevelForFeature;
		}
	}

	public void SelectPart(int index)
	{
		m_context.lockPartSelection = true;
		selectedPartIndex = index;
		paintState.SelectPart(index);
		if (!Input.GetMouseButton(0) && (!InputManager.instance.IsLastActiveJoystickAxis2D(1, out var axis) || !(axis != Vector2.zero)))
		{
			FocusCamera();
		}
	}

	private void FocusCamera()
	{
		UIBodyPartPaintItem selectedPart = GetSelectedPart();
		if (selectedPart != null)
		{
			m_context.FocusCameraOnPart(selectedPart.partType);
		}
		if (m_context.MultiselectMode)
		{
			return;
		}
		foreach (UIBodyPartPaintItem part in paintState.parts)
		{
			if (part != null)
			{
				part.addedToPaint.value = false;
			}
		}
		if (selectedPart != null)
		{
			selectedPart.addedToPaint.value = true;
		}
	}

	public void SelectFirstEnabledPart()
	{
		for (int i = 0; i < paintState.parts.Count; i++)
		{
			if (paintState.parts[i].ableToPaint.value)
			{
				SelectPart(i);
				break;
			}
		}
	}

	public void TogglePart(int index)
	{
		paintState.TogglePart(index);
	}

	public void ResetMultiselect()
	{
		paintState.ResetMultiselect();
	}

	public void SetCategoryActive(bool active)
	{
		foreach (UIBodyPartPaintItem part in paintState.parts)
		{
			if (!(part == null))
			{
				part.selected.value = false;
				part.addedToPaint.value = false;
				part.gameObject.SetActive(active);
			}
		}
	}

	public UIBodyPartPaintItem GetSelectedPart()
	{
		return paintState.GetSelectedPart(selectedPartIndex);
	}

	public List<UIBodyPartPaintItem> GetAddedToPaintParts()
	{
		List<UIBodyPartPaintItem> addedToPaintParts = paintState.GetAddedToPaintParts();
		if (addedToPaintParts.Count <= 0)
		{
			UIBodyPartPaintItem selectedPart = GetSelectedPart();
			if (selectedPart.partType == UIPaintCustomizationContext.PartType.Chassis)
			{
				addedToPaintParts.AddRange(paintState.parts);
			}
			else
			{
				addedToPaintParts.Add(selectedPart);
			}
		}
		return addedToPaintParts;
	}

	public void NavigateThroughParts(bool navigateRight)
	{
		if (m_context.lockPartSelection)
		{
			return;
		}
		if (navigateRight)
		{
			for (int i = selectedPartIndex + 1; i < paintState.parts.Count && !TrySelectPart(paintState.parts[i], i); i++)
			{
			}
			return;
		}
		int num = selectedPartIndex - 1;
		while (num >= 0 && !TrySelectPart(paintState.parts[num], num))
		{
			num--;
		}
		bool TrySelectPart(UIBodyPartPaintItem part, int partIndex)
		{
			if (part == null || m_context.NeedSkipPartByRule(part.partType))
			{
				return false;
			}
			if (part.ableToPaint.value)
			{
				SelectPart(partIndex);
				return true;
			}
			return false;
		}
	}

	public void OnSelectCategory()
	{
		if (m_context != null)
		{
			m_context.SelectCategory(stateType);
			InputManager.PlayClickSound();
		}
	}

	public void OnSubmitCategory()
	{
		if (!locked.value)
		{
			InputManager.instance.PlayClickEffect(null, delegate
			{
				m_context.ActivateCategory(stateType);
			}, skipPauseToAction: true);
		}
		else
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.locked_level_message_track.LocalizedFormat(locklevel.value));
		}
	}
}
