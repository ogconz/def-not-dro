using System;
using System.Collections.Generic;
using Customization;
using Services.UGC.Interfaces;
using Steamworks.Ugc;

namespace Services.UGC.Steamworks
{
	public class SteamWorkshopCarManager : SteamWorkshopManager
	{
		private const string CAR_TAG = "car";

		private const string SKIN_TAG = "skin";

		public override IUGCManager.ContentType contentType => IUGCManager.ContentType.Cars;

		public override void Update()
		{
			base.Update();
			if (CarPartDatabase.isReadyForLoadExternals && m_isAllDownloadedOnStart)
			{
				SteamWorkshopItem steamWorkshopItem = PopPendingPackage();
				if (steamWorkshopItem != null)
				{
					CarPartDatabase.ImportExternalData(steamWorkshopItem);
					GameEvents.UGCCarReady(steamWorkshopItem.id);
				}
			}
		}

		protected override Query CreateQueryList()
		{
			return Query.Items.WithTag("car").WithTag("skin").MatchAnyTag();
		}

		protected override bool EqualsTag(string tag)
		{
			if (!tag.Equals("car", StringComparison.OrdinalIgnoreCase))
			{
				return tag.Equals("skin", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}

		protected override void OnFavouriteItemsAquired()
		{
			base.OnFavouriteItemsAquired();
			IReadOnlyList<(int, ulong)> favoriteRims = m_prefs.favoriteRims.favoriteRims;
			List<(int, ulong)> list = new List<(int, ulong)>();
			foreach (var item in favoriteRims)
			{
				list.Add(item);
			}
			foreach (var part in list)
			{
				if (part.Item2 != 0L && !m_ugcDownloadedPackages.Exists((SteamWorkshopItem i) => i.id == part.Item2))
				{
					m_prefs.favoriteRims.RemoveRim(part);
				}
			}
		}
	}
}
