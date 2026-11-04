using Binding;

public class SlotQuestListElement : BaseListElementData
{
	private StringProperty m_text = new StringProperty();

	private BoolProperty m_isCompleted = new BoolProperty();

	private BoolProperty m_isSeparator = new BoolProperty();

	public SlotQuestListElement(string text, bool isCompleted, bool isSeparator)
	{
		m_text.value = text;
		m_isCompleted.value = isCompleted;
		m_isSeparator.value = isSeparator;
	}
}
