using System;
using System.Collections.Generic;
using SimpleJSON;

namespace PrefsModels
{
	public class PopupLastShownModel : BasePrefsModel
	{
		[JsonSerialize]
		private Dictionary<string, DateTime> m_data = new Dictionary<string, DateTime>();

		public DateTime GetPopupLastShownDate(string popupType)
		{
			if (m_data.TryGetValue(popupType, out var value))
			{
				return value;
			}
			return DateTime.MinValue;
		}

		public DateTime UpdatePopupLastShownDate(string popupType)
		{
			DateTime now = DateTime.Now;
			m_data[popupType] = now;
			Save();
			return now;
		}
	}
}
