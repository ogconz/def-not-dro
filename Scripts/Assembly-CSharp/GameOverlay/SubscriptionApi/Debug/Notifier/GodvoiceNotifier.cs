namespace GameOverlay.SubscriptionApi.Debug.Notifier
{
	public class GodvoiceNotifier : INotifier
	{
		public void Notify(string text)
		{
			GUICommonGodVoice.ShowText(text, 1f);
		}
	}
}
