using System;
using System.Collections.Generic;
using Binding;
using HUD;
using SyncMultiplayer;

namespace Championship.HUD
{
	[Obsolete]
	public class UITop32XDSClippingPostCombatContext : UIXDSPostCombatNetworkContext
	{
		private class SectorResultItem : BaseListElementData
		{
			public StringProperty pointsLeader = new StringProperty();

			public StringProperty sectorName = new StringProperty();

			public StringProperty pointsChaser = new StringProperty();

			public ListProperty zones = new ListProperty();

			public SectorResultItem(string name, string leader, string chaser, ICollection<int> zones)
			{
				sectorName.value = name;
				pointsLeader.value = leader;
				pointsChaser.value = chaser;
				FillZones(zones);
			}

			public void FillZones(ICollection<int> zones)
			{
				if (zones == null)
				{
					GameLog.Print(AlertLevel.Error, "Multiplayer", "Unable to retrive clipping zones while builing XDS Post Combat!");
					return;
				}
				foreach (int zone in zones)
				{
					this.zones.Add(new ClipingZoneItem(zone));
				}
			}
		}

		private class ClipingZoneItem : BaseListElementData
		{
			public BoolProperty active = new BoolProperty();

			public ClipingZoneItem(int active)
			{
				this.active.value = active == 1;
			}
		}

		private static readonly string DefaultRaceDQResultStr = "DQ";

		private ListProperty m_leftSectors = new ListProperty();

		private ListProperty m_rightSectors = new ListProperty();

		private BoolProperty m_leftLeaderDQ = new BoolProperty();

		private BoolProperty m_leftChaserDQ = new BoolProperty();

		private BoolProperty m_rightLeaderDQ = new BoolProperty();

		private BoolProperty m_rightChaserDQ = new BoolProperty();

		protected override (ValidatedRoomResult left, ValidatedRoomResult right)? InternalFillData(SubroomInfo info, out bool wonByQual)
		{
			(ValidatedRoomResult, ValidatedRoomResult)? result = base.InternalFillData(info, out wonByQual);
			if (!result.HasValue)
			{
				return result;
			}
			(ValidatedRoomResult, ValidatedRoomResult) value = result.Value;
			if (value.Item1?.result != null)
			{
				SubroomInfoRaceResult.XDSResultDetails xDSResultDetails = value.Item1.result.GetXDSResultDetails();
				FillSectors(m_leftSectors, xDSResultDetails);
				m_leftChaserDQ.value = !xDSResultDetails.drc.IsEmpty();
				m_leftLeaderDQ.value = !xDSResultDetails.drl.IsEmpty();
			}
			if (value.Item2?.result != null)
			{
				SubroomInfoRaceResult.XDSResultDetails xDSResultDetails2 = value.Item2.result.GetXDSResultDetails();
				FillSectors(m_rightSectors, xDSResultDetails2);
				m_rightChaserDQ.value = !xDSResultDetails2.drc.IsEmpty();
				m_rightLeaderDQ.value = !xDSResultDetails2.drl.IsEmpty();
			}
			return value;
		}

		protected override void ValidReasonOrScore(StringProperty scoreSp, StringProperty drSp, DisqualifyReason reason, string score)
		{
			if (!reason.IsEmpty())
			{
				scoreSp.value = DefaultRaceDQResultStr;
			}
			else if (OpponentHadDQ(score))
			{
				scoreSp.value = ExtractValidScore(score);
			}
			else
			{
				scoreSp.value = score;
			}
			static string ExtractValidScore(string scoreStr)
			{
				if (string.IsNullOrEmpty(scoreStr))
				{
					return string.Empty;
				}
				string text = scoreStr.Replace(SubroomInfoRaceResult.XDSResultDetails.OpponentHasDQStr, string.Empty);
				if (!float.TryParse(text, out var _))
				{
					text = string.Empty;
				}
				return text;
			}
		}

		protected override string GetTotalScoreStr(SubroomInfoRaceResult.XDSResultDetails rd, SubroomInfoRaceResult result)
		{
			if (!IsValidResult(rd))
			{
				return UIXDSPostCombatNetworkContext.ZeroScoreStr;
			}
			return result.asString;
		}

		protected override bool IsValidResult(SubroomInfoRaceResult.XDSResultDetails details)
		{
			if (details.drc.IsEmpty())
			{
				return details.drl.IsEmpty();
			}
			return false;
		}

		private void FillSectors(ListProperty list, SubroomInfoRaceResult.XDSResultDetails details)
		{
			list.Clear();
			for (int i = 0; i < details.s.Count; i++)
			{
				SubroomInfoRaceResult.XDSResultDetails.Sector sector = details.s[i];
				if (sector.b == 1)
				{
					list.Add(new SectorResultItem($"S{i} BLANK", string.Empty, string.Empty, sector.z));
				}
				else
				{
					list.Add(new SectorResultItem($"S{i}", UIXDSPostCombatNetworkContext.ValidResultStr(sector.l), UIXDSPostCombatNetworkContext.ValidResultStr(sector.c), sector.z));
				}
			}
		}
	}
}
