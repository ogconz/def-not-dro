using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;

namespace TriLib
{
	public class StringUtils
	{
		public static string GenerateUniqueName(object id)
		{
			return id.GetHashCode().ToString(CultureInfo.InvariantCulture);
		}

		public static byte[] UnZip(string value)
		{
			byte[] array = Convert.FromBase64String(value);
			using MemoryStream memoryStream = new MemoryStream();
			int num = BitConverter.ToInt32(array, 0);
			memoryStream.Write(array, 4, array.Length - 4);
			byte[] array2 = new byte[num];
			memoryStream.Position = 0L;
			using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
			{
				gZipStream.Read(array2, 0, array2.Length);
			}
			return array2;
		}
	}
}
