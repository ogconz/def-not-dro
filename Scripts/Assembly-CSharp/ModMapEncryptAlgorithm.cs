using System.Collections.Generic;
using Unity.Mathematics;

public class ModMapEncryptAlgorithm : EncryptAlgorithm
{
	private const float HASH_KEY = -38512.652f;

	private readonly Dictionary<byte, byte> m_cryptoConstruct = new Dictionary<byte, byte>();

	private readonly Dictionary<byte, byte> m_deCryptoConstruct = new Dictionary<byte, byte>();

	public ModMapEncryptAlgorithm()
	{
		List<byte> list = new List<byte>(new byte[256]);
		for (byte b = 0; b < byte.MaxValue; b++)
		{
			list[b] = b;
		}
		list[255] = byte.MaxValue;
		byte b2 = 0;
		while (list.Count > 0)
		{
			byte index = (byte)math.floor(Hash((int)b2, -38512.652f) * (float)list.Count);
			m_cryptoConstruct.Add(b2, list[index]);
			list.RemoveAt(index);
			b2++;
		}
		foreach (KeyValuePair<byte, byte> item in m_cryptoConstruct)
		{
			m_deCryptoConstruct.Add(item.Value, item.Key);
		}
	}

	private float Hash(float pos, float key)
	{
		return math.frac(math.dot(math.cos(pos), key));
	}

	public override byte Encrypt(byte value)
	{
		return m_cryptoConstruct[value];
	}

	public override byte Decrypt(byte value)
	{
		return m_deCryptoConstruct[value];
	}
}
