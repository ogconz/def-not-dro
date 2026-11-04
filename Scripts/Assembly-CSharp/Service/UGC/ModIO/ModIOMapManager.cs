using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using DI;
using Services.UGC.Interfaces;
using Services.UGC.Utils;

namespace Service.UGC.ModIO
{
	public class ModIOMapManager : ModIOManager
	{
		private BaseModel m_baseModel;

		public override string ugcTagMatch => "Map";

		public override IUGCManager.ContentType contentType => IUGCManager.ContentType.Tracks;

		protected override async UniTask<bool> ObtainInstalledItem(ModIOItem modIOItem)
		{
			return await UGCSceneBundleManager.TryInitTrackMetaAsync(modIOItem);
		}

		protected override bool IsReadyToHandle()
		{
			return !UGCSceneBundleManager.isLoadingSceneBundle;
		}

		public override void LaunchPackage(ulong id)
		{
			ModIOItem itemWithMeta = m_ugcInstalledItems.Find((ModIOItem item) => item.id == id);
			UGCSceneBundleManager.CheckVersionCompatibility(itemWithMeta);
			UGCSceneBundleManager.LaunchSceneByItem(itemWithMeta);
		}

		public override void RemovePackage(ulong id)
		{
			base.RemovePackage(id);
			UGCSceneBundleManager.DeleteExternalTrackMeta(id);
		}

		protected override void OnInstalledSubscribedItems()
		{
			DeleteAllUnsubscribedTracksFromDB();
		}

		protected override void OnUninstallItem(ModIOItem item)
		{
			UGCSceneBundleManager.DeleteExternalTrackMeta(item.id);
		}

		private void DeleteAllUnsubscribedTracksFromDB()
		{
			if (m_baseModel == null)
			{
				m_baseModel = DependencyInjector.Resolve<BaseModel>();
			}
			foreach (DB.Meta.Track track in m_baseModel.QueryAll<DB.Meta.Track>())
			{
				if (track.external && track.ugcServiceId == (byte)base.serviceProviderType && !m_ugcInstalledItems.Exists((ModIOItem i) => i.id == track.externalId))
				{
					UGCSceneBundleManager.DeleteExternalTrackMeta(track.externalId);
				}
			}
		}
	}
}
