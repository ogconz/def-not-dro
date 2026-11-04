using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

public static class EncryptedXmlSerializer
{
	private static readonly string PrivateKey = "AZAZA42AZAZAZAZA";

	public static T Load<T>(string path) where T : class
	{
		if (!File.Exists(path))
		{
			return null;
		}
		string value;
		using (StreamReader streamReader = new StreamReader(path))
		{
			value = DecryptData(streamReader.ReadToEnd());
		}
		MemoryStream memoryStream = new MemoryStream();
		using StreamWriter streamWriter = new StreamWriter(memoryStream)
		{
			AutoFlush = true
		};
		streamWriter.WriteLine(value);
		memoryStream.Position = 0L;
		return new XmlSerializer(typeof(T)).Deserialize(memoryStream) as T;
	}

	public static object LoadFromData(string data, Type type)
	{
		using (StringReader stringReader = new StringReader(data))
		{
			data = DecryptData(stringReader.ReadToEnd());
		}
		XmlSerializer xmlSerializer = new XmlSerializer(type);
		using StringReader textReader = new StringReader(data);
		return xmlSerializer.Deserialize(textReader);
	}

	public static void Save(string path, object value)
	{
		XmlSerializer xmlSerializer = new XmlSerializer(value.GetType());
		using MemoryStream memoryStream = new MemoryStream();
		xmlSerializer.Serialize(memoryStream, value);
		memoryStream.Flush();
		memoryStream.Position = 0L;
		string toEncrypt = new StreamReader(memoryStream).ReadToEnd();
		FileStream fileStream = new FileStream(path, FileMode.Create);
		StreamWriter streamWriter = new StreamWriter(fileStream);
		streamWriter.WriteLine(EncryptData(toEncrypt));
		streamWriter.Close();
		fileStream.Close();
	}

	private static string EncryptData(string toEncrypt)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(toEncrypt);
		byte[] array = CreateRijndaelManaged().CreateEncryptor().TransformFinalBlock(bytes, 0, bytes.Length);
		return Convert.ToBase64String(array, 0, array.Length);
	}

	private static string DecryptData(string toDecrypt)
	{
		byte[] array = Convert.FromBase64String(toDecrypt);
		byte[] bytes = CreateRijndaelManaged().CreateDecryptor().TransformFinalBlock(array, 0, array.Length);
		return Encoding.UTF8.GetString(bytes);
	}

	private static RijndaelManaged CreateRijndaelManaged()
	{
		byte[] bytes = Encoding.UTF8.GetBytes(PrivateKey);
		RijndaelManaged rijndaelManaged = new RijndaelManaged();
		byte[] array = new byte[16];
		Array.Copy(bytes, 0, array, 0, 16);
		rijndaelManaged.Key = array;
		rijndaelManaged.Mode = CipherMode.ECB;
		rijndaelManaged.Padding = PaddingMode.PKCS7;
		return rijndaelManaged;
	}
}
