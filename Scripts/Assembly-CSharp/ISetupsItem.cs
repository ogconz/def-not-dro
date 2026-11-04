using LocalCaching.Model;

public interface ISetupsItem
{
	CarProfileProxy profileProxy { get; }

	bool current { get; }

	string customSetupId { get; }

	void PlayHover();
}
