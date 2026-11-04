using TrackLayout.Data;

public class TrackConfigMetaItem : BaseListItem
{
	public UISelectorTrackMetaConfigContext context { get; private set; }

	public TrackMetaConfig meta { get; private set; }

	public string text { get; private set; }

	public TrackConfigMetaItem(UISelectorTrackMetaConfigContext context, TrackMetaConfig meta)
	{
		this.context = context;
		this.meta = meta;
		text = meta.GetLocalizedName();
	}

	public override void OnClick()
	{
		base.OnClick();
		context.SetTrackInfo(this);
	}

	public override void OnDoubleClick()
	{
		base.OnDoubleClick();
		context.LoadTrack(this);
	}
}
