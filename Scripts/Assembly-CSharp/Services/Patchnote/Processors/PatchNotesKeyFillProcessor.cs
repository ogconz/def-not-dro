using System.Collections.Generic;
using Services.Patchnote.Processors.Interfaces;

namespace Services.Patchnote.Processors
{
	public class PatchNotesKeyFillProcessor : IPatchnoteListProcessor<PatchNoteItem>
	{
		public void Process(List<PatchNoteItem> keysList)
		{
			foreach (string item in LocalizationService.GetAllKeysWithPrefix("patchnote"))
			{
				keysList.Add(Create(item));
			}
		}

		private PatchNoteItem Create(string key)
		{
			int num = key.IndexOf("_");
			string text = string.Empty;
			if (num != -1)
			{
				text = key.Substring(num + 1);
				num = text.IndexOf("_");
				if (num != -1)
				{
					text = text.Substring(0, num);
				}
			}
			return new PatchNoteItem(key, text, FormatType.Unknown);
		}
	}
}
