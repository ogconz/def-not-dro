using UnityEngine;

public class SettingsInputList : ListView<InputSettingsListItem, InputSettingsItemController>
{
	[SerializeField]
	private RectTransform m_consolePresetContainer;

	public float consoleContainerPresetWidth => m_consolePresetContainer.GetWidth();
}
