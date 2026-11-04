using PrefsModels;

public interface IStorageSystem
{
	bool SkipClearProfileOnReadError();

	string EncodeName(string name);

	void Save(string modelName, string data);

	void FlushSave();

	void Read(BasePrefsModel model);

	bool IsRemoteSupported();

	void WriteToRemoteStorage(string modelName, byte[] data);

	void ReadFromRemoteStorage(string modelName, out byte[] data);

	bool NeedUpdateRemote(BasePrefsModel model, string remoteName);
}
