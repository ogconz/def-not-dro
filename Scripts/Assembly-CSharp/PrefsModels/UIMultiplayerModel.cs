namespace PrefsModels
{
	public class UIMultiplayerModel : BasePrefsModel
	{
		private UIMultiplayerData m_data;

		public UIMultiplayerData data
		{
			get
			{
				return m_data;
			}
			set
			{
				m_data = value;
				Save();
			}
		}
	}
}
