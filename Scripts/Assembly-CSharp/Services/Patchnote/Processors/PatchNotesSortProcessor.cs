using System.Collections.Generic;
using Services.Patchnote.Processors.Interfaces;

namespace Services.Patchnote.Processors
{
	public class PatchNotesSortProcessor : IPatchnoteListProcessor<PatchNoteItem>
	{
		public void Process(List<PatchNoteItem> keysList)
		{
			keysList.Sort((PatchNoteItem lhs, PatchNoteItem rhs) => Comparer(lhs.key, rhs.key));
		}

		private int Comparer(string lhs, string rhs)
		{
			int number = GetNumber(lhs);
			int number2 = GetNumber(rhs);
			if (number < number2)
			{
				return -1;
			}
			if (number > number2)
			{
				return 1;
			}
			return 0;
		}

		private int GetNumber(string text)
		{
			text = text.Substring("patchnote".Length);
			int num = text.IndexOf("_");
			if (num != -1)
			{
				text = text.Substring(0, num);
			}
			return int.Parse(text);
		}
	}
}
