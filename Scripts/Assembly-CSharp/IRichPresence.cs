public interface IRichPresence
{
	void Show(RichPresenceStatus richPresenceStatus);

	void SetVariable<T>(RichPresenceVariables richPresenceStatus, T value);

	void ClearAll();
}
