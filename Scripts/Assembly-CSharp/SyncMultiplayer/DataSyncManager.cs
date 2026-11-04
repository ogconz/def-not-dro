using Sfs2X.Entities.Data;

namespace SyncMultiplayer
{
	public class DataSyncManager : BaseNetGameSystem
	{
		public const string KNOWLEDGE_UPDATE = "ku";

		public const string KNOWLEDGE_ADD_OWNER = "kao";

		public const string KNOWLEDGE_REMOVE_OWNER = "kro";

		public const string KNOWLEDGE_REMOVED = "kr";

		public const string KNOWLEDGE_DATA = "kd";

		public const string KNOWLEDGE_ID = "ki";

		public const string KNOWLEDGE_TTL = "kt";

		public const string KNOWLEDGE_GENERATION = "kg";

		private NetworkGame m_game;

		private SFSObject m_addDataSFSObject = new SFSObject();

		private SFSObject m_updateDataSFSObject = new SFSObject();

		private SFSObject m_subscribeSFSObject = new SFSObject();

		public override void Attach(NetGameSystemsManager owner)
		{
			base.Attach(owner);
			m_game = owner.host;
		}

		public void AddData(string id, int ttl, ISFSObject knowledgeData)
		{
			m_addDataSFSObject.PutUtfString("ki", id);
			m_addDataSFSObject.PutInt("kt", ttl);
			m_addDataSFSObject.PutSFSObject("kd", knowledgeData);
			m_game.SendServerCommand("ku", m_addDataSFSObject);
		}

		public void UpdateData(string id, ISFSObject knowledgeData)
		{
			m_updateDataSFSObject.PutUtfString("ki", id);
			m_updateDataSFSObject.PutSFSObject("kd", knowledgeData);
			m_game.SendServerCommand("ku", m_updateDataSFSObject);
		}

		public void Subscribe(string id)
		{
			m_subscribeSFSObject.PutUtfString("ki", id);
			m_game.SendServerCommand("kao", m_subscribeSFSObject);
		}

		public void Unsubscribe(string id)
		{
			m_subscribeSFSObject.PutUtfString("ki", id);
			m_game.SendServerCommand("kro", m_subscribeSFSObject);
		}
	}
}
