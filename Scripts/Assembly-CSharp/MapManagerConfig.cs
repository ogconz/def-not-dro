public class MapManagerConfig : ScriptableSingleton<MapManagerConfig>
{
	public MapMetaConfig mapMetaConfigValue;

	public MapMetaConfigValue Value => mapMetaConfigValue.mapMetaConfigValue;
}
