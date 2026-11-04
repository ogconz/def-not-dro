using System;

namespace GameOverlay
{
	public class EmptyFilterTextManager : IFilterTextManager
	{
		public bool TryGetFilteredCachedText(TextFilterType textFilterType, UserPlatform.Id platformId, string nonFiltered, out string filteredText)
		{
			filteredText = nonFiltered;
			return true;
		}

		public string GetFilteredCachedText(TextFilterType textFilterType, UserPlatform.Id platformId, string nonFiltered, Action<string> filteredCallback)
		{
			nonFiltered = UIUtils.FilterInvalidTags(nonFiltered);
			filteredCallback.Call(nonFiltered);
			return nonFiltered;
		}
	}
}
