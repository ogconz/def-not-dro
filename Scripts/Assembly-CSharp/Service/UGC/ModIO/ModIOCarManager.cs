using Customization;
using Cysharp.Threading.Tasks;
using Services.UGC.Interfaces;

namespace Service.UGC.ModIO
{
	public class ModIOCarManager : ModIOManager
	{
		public override string ugcTagMatch => "Vehicle";

		public override IUGCManager.ContentType contentType => IUGCManager.ContentType.Cars;

		public override void LaunchPackage(ulong id)
		{
		}

		protected override async UniTask<bool> ObtainInstalledItem(ModIOItem modIOItem)
		{
			await UniTask.WaitUntil(() => CarPartDatabase.isReadyForLoadExternals);
			CarPartDatabase.ImportExternalData(modIOItem);
			GameEvents.UGCCarReady(modIOItem.id);
			return true;
		}

		protected override void OnInstalledSubscribedItems()
		{
		}

		protected override void OnUninstallItem(ModIOItem modIOItem)
		{
			CarPartDatabase.DeleteExternalData(modIOItem);
		}
	}
}
