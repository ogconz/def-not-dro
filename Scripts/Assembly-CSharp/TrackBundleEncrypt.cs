using System;
using System.IO;
using System.Threading.Tasks;

public class TrackBundleEncrypt<T> where T : EncryptAlgorithm, new()
{
	private EncryptAlgorithm m_encryptAlgorithm;

	public TrackBundleEncrypt()
	{
		m_encryptAlgorithm = new T();
	}

	public async Task Encrypt(string pathToFile, string outEncryptFilePath)
	{
		await Crypt(pathToFile, outEncryptFilePath, m_encryptAlgorithm.Encrypt);
	}

	public async Task Decrypt(string pathToFile, string outEncryptFilePath)
	{
		await Crypt(pathToFile, outEncryptFilePath, m_encryptAlgorithm.Decrypt);
	}

	private async Task Crypt(string pathToFile, string outEncryptFilePath, Func<byte, byte> crypt)
	{
		new FileInfo(pathToFile).CopyTo(outEncryptFilePath, overwrite: true);
		using FileStream sourceStream = File.Open(pathToFile, FileMode.Open, FileAccess.Read);
		using FileStream outStream = File.Open(outEncryptFilePath, FileMode.OpenOrCreate, FileAccess.Write);
		sourceStream.Seek(0L, SeekOrigin.Begin);
		byte[] buffer = new byte[sourceStream.Length];
		await sourceStream.ReadAsync(buffer, 0, buffer.Length);
		sourceStream.Dispose();
		for (int i = 0; i < buffer.Length; i++)
		{
			buffer[i] = crypt(buffer[i]);
		}
		outStream.Seek(0L, SeekOrigin.Begin);
		Task writeAsync = outStream.WriteAsync(buffer, 0, buffer.Length);
		await writeAsync;
		writeAsync.Dispose();
	}

	public async void StartCrypt(params Func<Task>[] tasks)
	{
		for (int i = 0; i < tasks.Length; i++)
		{
			await tasks[i]();
		}
	}
}
