using System.Collections.Generic;
using DI;
using UnityEngine;

namespace PrefsModels
{
	public class PrefsModelCustomCameras : BasePrefsModel
	{
		public class CameraDescriptor
		{
			public Vector3 position;
		}

		public Dictionary<int, List<CameraDescriptor>> cameras = new Dictionary<int, List<CameraDescriptor>>();

		private GamePrefs m_prefs;

		public PrefsModelCustomCameras()
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}

		public void RemoveTrackCustomCameras(int trackId)
		{
			cameras.Remove(trackId);
			Save();
		}

		public void UpdateCameras(List<FreeCamItemViewModel> list)
		{
			int lastTrackId = m_prefs.globals.lastTrackId;
			if (lastTrackId == -1)
			{
				GameLog.Print(AlertLevel.Warning, "CustomCameras", "UpdateCameras: Unknown track " + lastTrackId);
				return;
			}
			if (!cameras.TryGetValue(lastTrackId, out var value))
			{
				value = new List<CameraDescriptor>();
				cameras.Add(lastTrackId, value);
			}
			value.Clear();
			foreach (FreeCamItemViewModel item in list)
			{
				value.Add(new CameraDescriptor
				{
					position = item.position
				});
			}
			Save();
		}

		public List<CameraDescriptor> GetCameras()
		{
			int lastTrackId = m_prefs.globals.lastTrackId;
			if (lastTrackId == -1)
			{
				GameLog.Print(AlertLevel.Warning, "CustomCameras", "GetCameras: Unknown track " + lastTrackId);
				return null;
			}
			cameras.TryGetValue(lastTrackId, out var value);
			return value;
		}
	}
}
