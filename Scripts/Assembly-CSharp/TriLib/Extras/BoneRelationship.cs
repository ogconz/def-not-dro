namespace TriLib.Extras
{
	public class BoneRelationship
	{
		public string HumanBone;

		public string BoneName;

		public bool Optional;

		public BoneRelationship(string humanBone, string boneName, bool optional)
		{
			HumanBone = humanBone;
			BoneName = boneName;
			Optional = optional;
		}
	}
}
