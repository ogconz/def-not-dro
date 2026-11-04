namespace TriLib
{
	public class AssimpMetadata
	{
		public AssimpMetadataType MetadataType;

		public uint MetadataIndex;

		public string MetadataKey;

		public object MetadataValue;

		public AssimpMetadata(AssimpMetadataType metadataType, uint metadataIndex, string metadataKey, object metadataValue)
		{
			MetadataType = metadataType;
			MetadataIndex = metadataIndex;
			MetadataKey = metadataKey;
			MetadataValue = metadataValue;
		}
	}
}
