using Sfs2X.Entities.Data;

namespace SyncMultiplayer
{
	public class NetGameSyncPointsSystem : BaseNetGameSystem
	{
		private NetworkGame m_game;

		private NetGameSubroomsSystem m_subrooms;

		public override void Attach(NetGameSystemsManager owner)
		{
			base.Attach(owner);
			m_game = owner.host;
			m_subrooms = owner.Get<NetGameSubroomsSystem>();
			GameEvents.OnDriftPointsEarned -= new GameEventHandler<GameEvents.DriftPointsInfo>(OnDriftPointsEarned);
			GameEvents.OnDriftPointsEarned += new GameEventHandler<GameEvents.DriftPointsInfo>(OnDriftPointsEarned);
		}

		public override void OnGameConnect()
		{
			if (m_game.LocalPlayer.NetworkID != -1)
			{
				m_game.LocalPlayer.points.Clear(allData: true);
			}
		}

		private void OnDriftPointsEarned(GameEvents.DriftPointsInfo e)
		{
			if (m_game.LocalPlayer.NetworkID != -1)
			{
				NetworkPlayer localPlayer = m_game.LocalPlayer;
				if (e.curPoints > (float)localPlayer.points.maxPoints)
				{
					localPlayer.points.maxPoints = (int)e.curPoints;
				}
			}
		}

		public void UpdatePoints()
		{
			SyncNetFreerideRaceModeState syncNetFreerideRaceModeState = States.mainCurrent as SyncNetFreerideRaceModeState;
			NetworkPlayer localPlayer = m_game.LocalPlayer;
			NetworkPlayer.Points points = localPlayer.points;
			int num = syncNetFreerideRaceModeState.currentDriftPoints - points.RAWDriftPoints;
			points.RAWDriftPoints = syncNetFreerideRaceModeState.currentDriftPoints;
			if (num > 0)
			{
				points.points += num;
			}
			if (m_subrooms.myRoom != null)
			{
				if ((int)m_subrooms.myRoom.state < 3)
				{
					UIXDSInfoContext instance = UIXDSInfoContext.instance;
					points.driftPoints = (instance.activeXDS ? DriftControllerXDS.ConvertScoresToInt(instance.scores) : points.RAWDriftPoints);
					points.raceTime = (syncNetFreerideRaceModeState.currentPenaltyTime.HasValue ? syncNetFreerideRaceModeState.currentPenaltyTime.Value : 0f);
					points.raceTime += (syncNetFreerideRaceModeState.currentLapTime.HasValue ? syncNetFreerideRaceModeState.currentLapTime.Value : 0f);
					bool flag = localPlayer.userCar == null || (m_game != null && m_game.lastRoomOptions.ExternalTrackId != 0);
					points.raceProgress = ((!flag) ? ((int)syncNetFreerideRaceModeState.checkpointManager.GetCompletedTrackPercent(localPlayer.userCar)) : 0);
				}
				else
				{
					if (m_subrooms.myRoom.subroomMode.IsTime())
					{
						points.raceTime = m_subrooms.myLastResult.time;
					}
					points.raceProgress = 100;
				}
			}
			else
			{
				points.raceProgress = 100;
			}
		}

		public SFSArray GetPacketData()
		{
			UpdatePoints();
			NetworkPlayer.Points points = m_game.LocalPlayer.points;
			SFSArray sFSArray = new SFSArray();
			sFSArray.AddInt(points.points);
			sFSArray.AddInt(points.maxPoints);
			sFSArray.AddInt(points.raceProgress);
			sFSArray.AddInt(points.driftPoints);
			sFSArray.AddFloat(points.raceTime);
			return sFSArray;
		}

		public void ApplyPackedData(NetworkPlayer sender, ISFSArray array)
		{
			int num = 0;
			sender.points.points = array.GetInt(num++);
			sender.points.maxPoints = array.GetInt(num++);
			sender.points.raceProgress = array.GetInt(num++);
			sender.points.driftPoints = array.GetInt(num++);
			sender.points.raceTime = array.GetFloat(num++);
		}
	}
}
