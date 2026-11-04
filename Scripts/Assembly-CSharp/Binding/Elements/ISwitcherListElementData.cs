namespace Binding.Elements
{
	public interface ISwitcherListElementData
	{
		bool selected { get; set; }

		void Next();

		void Prev();
	}
}
