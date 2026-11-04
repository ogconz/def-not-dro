using System.Collections.Generic;
using GameOverlay;
using Services.Patchnote.Processors.Interfaces;

namespace Services.Patchnote.Processors
{
	public class PatchNotesTagsProcessor : IPatchnoteListProcessor<PatchNoteItem>
	{
		private PatchnoteTagsMap m_tagsMap = new PatchnoteTagsMap();

		private UserPlatform.Id m_platform;

		private PlatformRelatedTagMap m_platformRelatedTagMap;

		public PatchNotesTagsProcessor()
		{
			m_platform = Overlay.instance.GetPlatform();
			m_platformRelatedTagMap = m_tagsMap.GetTagsMap(m_platform);
		}

		public void Process(List<PatchNoteItem> keysList)
		{
			FillPatchNoteItems(keysList);
		}

		private void FillPatchNoteItems(List<PatchNoteItem> items)
		{
			for (int i = 0; i < items.Count; i++)
			{
				string[] tags = items[i].key.Split('_');
				if (!CheckTagsFromKey(tags))
				{
					items.RemoveAt(i);
					i--;
				}
			}
		}

		private bool CheckTagsFromKey(string[] tags)
		{
			if (CheckForIncludableTags(tags))
			{
				return !CheckForMutuallyExclusiveTags(tags);
			}
			return false;
		}

		private bool CheckForIncludableTags(string[] tags)
		{
			bool result = true;
			foreach (string item in tags)
			{
				if (m_platformRelatedTagMap.exclude.Contains(item))
				{
					result = false;
				}
				if (m_platformRelatedTagMap.include.Contains(item))
				{
					result = true;
					break;
				}
			}
			return result;
		}

		private bool CheckForMutuallyExclusiveTags(string[] tags)
		{
			foreach (string text in tags)
			{
				List<string> list = new List<string>(tags);
				list.Remove(text);
				foreach (string item in list)
				{
					if (item.Contains(text))
					{
						GameLog.LogError("Mutually exclusive or duplicate patchnotetags found in: `" + string.Join("_", tags) + "`");
						return true;
					}
				}
			}
			return false;
		}
	}
}
