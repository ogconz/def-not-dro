using Binding;
using GameOverlay;

namespace HUD
{
	public class PlayerInRoomItem : BaseListElementData
	{
		private StringProperty m_name = new StringProperty();

		private IntProperty m_number = new IntProperty();

		private BoolProperty m_self = new BoolProperty();

		private StringProperty m_result = new StringProperty();

		private IntProperty m_racePropgress = new IntProperty();

		private BoolProperty m_disqualified = new BoolProperty();

		private BoolProperty m_showPlatformIcon = new BoolProperty();

		public BoolProperty m_badConnection = new BoolProperty(false);

		private IntProperty m_platform = new IntProperty();

		public IntProperty playerInputType = new IntProperty();

		private PlayerId m_id;

		public PlayerInRoomItem(int number)
		{
			m_number.value = number;
		}

		public void SetConnectionStatus(bool connection)
		{
			m_badConnection.value = connection;
		}

		public void Fill(PlayerId id, string nickName, string result, int progress, bool isLocal, bool disqualified)
		{
			if (m_id != id)
			{
				m_id = id;
				m_name.value = nickName;
			}
			m_result.value = result;
			m_racePropgress.value = progress;
			m_self.value = isLocal;
			m_disqualified.value = disqualified;
		}
	}
}
