using System;
using System.Collections.Generic;
using DB;
using DB.Meta;
using DI;

namespace Minimaps
{
	public class MinimapProvider
	{
		private MinimapData m_data;

		private List<(RaceCar, string)> m_cars = new List<(RaceCar, string)>();

		private static MinimapProvider s_instance;

		private static List<MinimapItem> s_itemList = new List<MinimapItem>(32);

		private BaseModel m_model;

		private GamePrefs m_prefs;

		public RaceCar playerCar { get; private set; }

		public CheckpointManager checkpointManager { get; private set; }

		public string trackName { get; private set; }

		public MinimapData data => m_data;

		public List<(RaceCar, string)> cars => m_cars;

		public static List<MinimapItem> itemList => s_itemList;

		public static MinimapProvider instance => s_instance ?? (s_instance = new MinimapProvider());

		public static event Action OnRefreshMinimap;

		public event Action OnSwitchCar;

		public event Action<RaceCar> OnDisponseCar;

		public event Action<RaceCar, string> OnCreateNickName;

		public event Action<RaceCar, string> OnUpdateNickName;

		private MinimapProvider()
		{
		}

		public void Init(RaceCar car, CheckpointManager checkpointManager)
		{
			playerCar = car;
			this.checkpointManager = checkpointManager;
			InitCheckpointRangeList();
			if (m_model == null)
			{
				m_model = DependencyInjector.Resolve<BaseModel>();
			}
			if ((object)m_prefs == null)
			{
				m_prefs = DependencyInjector.Resolve<GamePrefs>();
			}
			InitTrackName();
		}

		private void InitTrackName()
		{
			string nameById = m_model.GetNameById<DB.Meta.Track>(m_prefs.globals.lastTrackId);
			trackName = LocalizationService.Localize(nameById);
		}

		public void SetCar(RaceCar car)
		{
			playerCar = car;
		}

		public void OnCreateNicknameHandlerInvoke(RaceCar car, string name)
		{
			m_cars.Add((car, name));
			this.OnCreateNickName.SafeCall(car, name);
		}

		public void OnSwitchCarHandlerInvoke()
		{
			this.OnSwitchCar.SafeCall();
		}

		public void OnDisposeCarHandlerInvoke(RaceCar car)
		{
			this.OnDisponseCar.SafeCall(car);
		}

		public void OnUpdateNicknameHandlerInvoke(RaceCar car, string name)
		{
			this.OnUpdateNickName.SafeCall(car, name);
		}

		private void InitCheckpointRangeList()
		{
			m_data.zoneList = null;
			if (!MinimapHelper.IsManyZoneExists(checkpointManager.checkpoints))
			{
				return;
			}
			IList<Checkpoint> checkpoints = checkpointManager.checkpoints;
			m_data.zoneList = new List<MinimapZone>(checkpoints.Count);
			float num = -1f;
			float num2 = -1f;
			int? fromStageId = null;
			int num3 = checkpoints.Count - 1;
			for (int i = 0; i < num3; i++)
			{
				Checkpoint checkpoint = checkpoints[i];
				Checkpoint checkpoint2 = checkpoints[i + 1];
				num = ((num < 0f) ? 0f : num2);
				num2 = ((i + 1 >= num3) ? checkpoint2.spline.length : (checkpoint.distance + (checkpoint2.distance - checkpoint.distance) * 0.5f));
				MinimapZone item;
				if (checkpoint.zone == checkpoint2.zone && !fromStageId.HasValue)
				{
					item = new MinimapZone(num, num2, checkpoint.zone);
				}
				else if (!fromStageId.HasValue)
				{
					fromStageId = checkpoint.zone;
					item = new MinimapZone(num, num2, checkpoint.zone);
				}
				else
				{
					item = new MinimapZone(num, num2, checkpoint.zone, fromStageId);
					fromStageId = null;
				}
				m_data.zoneList.Add(item);
			}
		}

		public void DisposeCar(RaceCar car)
		{
			m_cars.RemoveAll(((RaceCar, string) element) => element.Item1 == car);
		}

		public void ResetCheckpoints()
		{
			InitCheckpointRangeList();
		}

		public static void SortList()
		{
			s_itemList.Sort((MinimapItem x1, MinimapItem x2) => x1.depth.CompareTo(x2.depth));
		}

		public static void AddIcon(MinimapItem icon)
		{
			s_itemList.Add(icon);
			MinimapProvider.OnRefreshMinimap.SafeCall();
		}

		public static void RemoveIcon(MinimapItem icon)
		{
			s_itemList.Remove(icon);
			MinimapProvider.OnRefreshMinimap.SafeCall();
			if (icon.host is RaceCar car)
			{
				instance.OnDisposeCarHandlerInvoke(car);
			}
		}

		public void DisposeResources()
		{
			m_cars.Clear();
			s_itemList.Clear();
			playerCar = null;
		}
	}
}
