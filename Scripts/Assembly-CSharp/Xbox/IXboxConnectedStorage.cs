using System.Collections;

namespace Xbox
{
	public interface IXboxConnectedStorage
	{
		string prefix { get; }

		IEnumerator Submit();

		IEnumerator Clear(string containerName);

		bool Write(string modelName, byte[] data);

		(bool, byte[] data) Read(string modelName);

		void Dispose();
	}
}
