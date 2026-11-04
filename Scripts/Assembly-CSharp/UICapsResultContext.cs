using Binding;
using Binding.Base;
using DB.Meta;

public class UICapsResultContext : BaseBindingBehaviourTarget
{
	public readonly Property<bool> visible = new BoolProperty(false);

	public readonly Property<string> goldLimit = new StringProperty();

	public readonly Property<string> silverLimit = new StringProperty();

	public readonly Property<string> bronzeLimit = new StringProperty();

	public readonly Property<int> cupsLevel = new IntProperty(4);

	public readonly Property<bool> isTimeattack = new BoolProperty(false);

	public void SetInfo(IRaceNorm norms, int cupIndex)
	{
		visible.value = norms != null;
		if (visible.value)
		{
			isTimeattack.value = norms is TrackTimeAttackRaceNormMeta;
			goldLimit.value = norms.GetNormAsString(RaceCupType.Gold);
			silverLimit.value = norms.GetNormAsString(RaceCupType.Silver);
			bronzeLimit.value = norms.GetNormAsString(RaceCupType.Bronze);
			cupsLevel.value = cupIndex;
			if (cupsLevel.value < 1)
			{
				cupsLevel.value = 4;
			}
		}
	}
}
