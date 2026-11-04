using System;

namespace GameOverlay
{
	public interface IFilterTextManager
	{
		bool TryGetFilteredCachedText(TextFilterType textFilterType, UserPlatform.Id platformId, string nonFiltered, out string filteredText);

		string GetFilteredCachedText(TextFilterType textFilterType, UserPlatform.Id platformId, string nonFiltered, Action<string> filteredCallback);
	}
}
