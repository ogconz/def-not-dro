using System.Collections.Generic;
using Services.Patchnote.Processors.Interfaces;

namespace Services.Patchnote.Processors
{
	public class PatchNotesTypeProcessor : IPatchnoteListProcessor<PatchNoteItem>
	{
		public void Process(List<PatchNoteItem> keysList)
		{
			foreach (PatchNoteItem keys in keysList)
			{
				ConvertPatchNote(keys);
			}
		}

		private void ConvertPatchNote(PatchNoteItem patchNoteItem)
		{
			switch (patchNoteItem.postfix)
			{
			case "title":
				patchNoteItem.formatType = FormatType.Title;
				patchNoteItem.SetText(GetLocalizedTextFromNewKey(patchNoteItem.key));
				break;
			case "header":
				patchNoteItem.formatType = FormatType.Header;
				patchNoteItem.SetText(GetLocalizedTextFromNewKey(patchNoteItem.key));
				break;
			case "image":
				patchNoteItem.formatType = FormatType.Image;
				break;
			case "space":
				patchNoteItem.formatType = FormatType.Space;
				break;
			default:
			{
				string localizedTextFromNewKey = GetLocalizedTextFromNewKey(patchNoteItem.key);
				patchNoteItem.formatType = FormatType.Text;
				patchNoteItem.SetText(localizedTextFromNewKey);
				break;
			}
			}
		}

		private string CreateTagStringForPatchnoteOnDev(string key)
		{
			string[] array = key.Split('_');
			List<string> list = new List<string>(array);
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (text.Contains("patchnote"))
				{
					list.Remove(text);
				}
			}
			if (list.Count <= 0)
			{
				return "empty";
			}
			return string.Join(", ", list);
		}

		private string GetLocalizedTextFromNewKey(string key)
		{
			return new LocalizationKeys.Key(key).Localized();
		}
	}
}
