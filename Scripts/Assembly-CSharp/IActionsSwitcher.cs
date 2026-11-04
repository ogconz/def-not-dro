using Binding.Components;

public interface IActionsSwitcher
{
	void AddActions(HeaderActionData[] headerActions);

	void RemoveAction(HeaderActionBinding binding);

	void ClearActions();

	void SetSelectedActionIndex(int index);

	void SelectionAction(int index);
}
