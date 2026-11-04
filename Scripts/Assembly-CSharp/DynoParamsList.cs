public class DynoParamsList : ListView<BaseDynoParamsItemViewModel, DynoParamsItemView>
{
	public override int selectedIndex
	{
		get
		{
			return m_selectedIndex;
		}
		set
		{
			base.selectedIndex = value;
		}
	}
}
