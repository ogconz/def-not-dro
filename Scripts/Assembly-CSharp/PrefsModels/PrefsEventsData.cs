using System.Collections.Generic;

namespace PrefsModels
{
	public class PrefsEventsData : BasePrefsModel
	{
		public Dictionary<int, int> halloweenLocksTracks = new Dictionary<int, int>();

		public int pumpkinsCount;

		public bool halloweenComplete;

		public PrefsEventsData()
		{
			halloweenLocksTracks.Clear();
		}

		public override void PostLoad()
		{
		}

		protected override bool CompareRecovePrefs(BasePrefsModel reserveModel)
		{
			if (!(reserveModel is PrefsEventsData prefsEventsData))
			{
				return true;
			}
			if (halloweenLocksTracks == prefsEventsData.halloweenLocksTracks)
			{
				return halloweenComplete == prefsEventsData.halloweenComplete;
			}
			return false;
		}

		public override void Reset()
		{
			base.Reset();
		}
	}
}
