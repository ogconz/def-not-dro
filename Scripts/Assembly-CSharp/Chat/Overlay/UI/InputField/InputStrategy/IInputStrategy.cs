namespace Chat.Overlay.UI.InputField.InputStrategy
{
	public interface IInputStrategy
	{
		void Setup();

		void Release();

		bool IsLocked();
	}
}
