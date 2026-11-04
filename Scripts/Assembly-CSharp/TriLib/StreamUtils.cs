using System.IO;

namespace TriLib
{
	public static class StreamUtils
	{
		public static byte[] ReadFullStream(Stream stream)
		{
			byte[] array = new byte[4096];
			using MemoryStream memoryStream = new MemoryStream();
			int count;
			while ((count = stream.Read(array, 0, array.Length)) > 0)
			{
				memoryStream.Write(array, 0, count);
			}
			return memoryStream.ToArray();
		}
	}
}
