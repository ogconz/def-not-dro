namespace LoadingArt
{
	public class LoadingSlideshowConfigLoader
	{
		private const string AddressablesRelativeDirLoadingArtConfigPath = "LoadingArt/LoadingArtConfig";

		private LoadingArtConfigSO loadingArtConfigSO;

		public LoadingArtConfigSO LoadSptiresByScreenTarget(LoadingScreenTarget target)
		{
			if (target == LoadingScreenTarget.Start)
			{
				return LoadStartScreenSptires();
			}
			return null;
		}

		private LoadingArtConfigSO LoadStartScreenSptires()
		{
			if (loadingArtConfigSO == null)
			{
				loadingArtConfigSO = ResourcesWrapper.Load<LoadingArtConfigSO>("LoadingArt/LoadingArtConfig");
			}
			return loadingArtConfigSO;
		}
	}
}
