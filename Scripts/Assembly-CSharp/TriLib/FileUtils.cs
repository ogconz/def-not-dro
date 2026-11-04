using System;
using System.IO;

namespace TriLib
{
	public static class FileUtils
	{
		private static byte[] m_zeroArray = new byte[0];

		public static string GetShortFilename(string filename)
		{
			int num = filename.LastIndexOf("\\");
			if (num >= 0)
			{
				return filename.Substring(num + 1);
			}
			int num2 = filename.LastIndexOf("/");
			if (num2 >= 0)
			{
				return filename.Substring(num2 + 1);
			}
			return filename;
		}

		public static string GetFileDirectory(string filename)
		{
			int num = filename.LastIndexOf("\\");
			if (num >= 0)
			{
				return filename.Substring(0, num);
			}
			int num2 = filename.LastIndexOf("/");
			if (num2 >= 0)
			{
				return filename.Substring(0, num2);
			}
			return null;
		}

		public static string GetFilenameWithoutExtension(string filename)
		{
			int num = filename.LastIndexOf('.');
			if (num < 0)
			{
				return null;
			}
			int num2 = filename.LastIndexOf("\\");
			if (num2 >= 0)
			{
				return filename.Substring(num2 + 1, num - 1);
			}
			int num3 = filename.LastIndexOf("/");
			if (num3 >= 0)
			{
				return filename.Substring(num3 + 1, num - num3 - 1);
			}
			return null;
		}

		public static string GetFileExtension(string filename)
		{
			int num = filename.LastIndexOf('.');
			if (num < 0)
			{
				return null;
			}
			return filename.Substring(num).ToLowerInvariant();
		}

		public static string GetFilename(string path)
		{
			string fileName = Path.GetFileName(path);
			if (path == fileName)
			{
				int num = path.LastIndexOf("\\");
				if (num >= 0)
				{
					return path.Substring(num + 1);
				}
				int num2 = path.LastIndexOf("/");
				if (num2 >= 0)
				{
					return path.Substring(num2 + 1);
				}
				return path;
			}
			return fileName;
		}

		public static byte[] LoadFileData(string filename)
		{
			try
			{
				if (filename == null)
				{
					return m_zeroArray;
				}
				if (!File.Exists(filename))
				{
					return m_zeroArray;
				}
				return File.ReadAllBytes(filename);
			}
			catch (Exception)
			{
				return m_zeroArray;
			}
		}
	}
}
