namespace SharedConfigs_DR2
{
	public class RimSharedMeta : BaseMeta
	{
		public string name;

		public string icon;

		public string prefab;

		public RimSharedMeta()
		{
		}

		public RimSharedMeta(int id, string name, string icon, string prefab)
		{
			base.id = id;
			this.name = name;
			this.icon = icon;
			this.prefab = prefab;
		}
	}
}
