using Binding;
using HUD;
using SyncMultiplayer;

namespace Championship.HUD
{
	public class UITop32XDSPostCombatContext : UIXDSPostCombatNetworkContext
	{
		private BoolProperty m_winnerByQual = new BoolProperty(false);

		protected override (ValidatedRoomResult left, ValidatedRoomResult right)? InternalFillData(SubroomInfo info, out bool wonByQual)
		{
			(ValidatedRoomResult, ValidatedRoomResult)? result = base.InternalFillData(info, out wonByQual);
			if (!result.HasValue)
			{
				return result;
			}
			(ValidatedRoomResult, ValidatedRoomResult) value = result.Value;
			bool flag = false;
			if (value.Item1?.result != null && value.Item2?.result != null)
			{
				SubroomInfoRaceResult.XDSResultDetails xDSResultDetails = value.Item1.result.GetXDSResultDetails();
				SubroomInfoRaceResult.XDSResultDetails xDSResultDetails2 = value.Item2.result.GetXDSResultDetails();
				flag = (!xDSResultDetails.drc.IsEmpty() || !xDSResultDetails.drl.IsEmpty()) && (!xDSResultDetails2.drc.IsEmpty() || !xDSResultDetails2.drl.IsEmpty());
			}
			if (flag)
			{
				winner.value = -1;
				m_winnerByQual.value = true;
				wonByQual = true;
			}
			return value;
		}
	}
}
