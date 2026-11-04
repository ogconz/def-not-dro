using System.Security.Cryptography;
using System.Text;
using SyncMultiplayer;

namespace Utlis
{
	public static class SymmetricAlgorithmTextCrypter
	{
		private static readonly byte[] m_key = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };

		private static readonly byte[] m_iv = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };

		private static readonly SymmetricAlgorithm m_algorithm = DES.Create();

		public static byte[] Crypt(string text)
		{
			ICryptoTransform cryptoTransform = m_algorithm.CreateEncryptor(m_key, m_iv);
			byte[] bytes = Encoding.Unicode.GetBytes(text);
			return cryptoTransform.TransformFinalBlock(bytes, 0, bytes.Length);
		}

		public static byte[] CryptZip(string text)
		{
			ICryptoTransform cryptoTransform = m_algorithm.CreateEncryptor(m_key, m_iv);
			byte[] array = CustomTypes.Zip(text);
			return cryptoTransform.TransformFinalBlock(array, 0, array.Length);
		}

		public static string Decrypt(byte[] buffer)
		{
			byte[] bytes = m_algorithm.CreateDecryptor(m_key, m_iv).TransformFinalBlock(buffer, 0, buffer.Length);
			return Encoding.Unicode.GetString(bytes);
		}

		public static string DecryptZip(byte[] buffer)
		{
			return CustomTypes.Unzip(m_algorithm.CreateDecryptor(m_key, m_iv).TransformFinalBlock(buffer, 0, buffer.Length));
		}
	}
}
