using Services.Patchnote.Processors;

namespace Services.Patchnote
{
	public class PatchNoteItem
	{
		public FormatType formatType;

		public string key;

		public string postfix;

		public string text { get; private set; }

		public PatchNoteItem(string _key, string _postfix, FormatType _formatType)
		{
			key = _key;
			postfix = _postfix;
			formatType = _formatType;
		}

		public void SetText(string text)
		{
			this.text = text;
		}
	}
}
