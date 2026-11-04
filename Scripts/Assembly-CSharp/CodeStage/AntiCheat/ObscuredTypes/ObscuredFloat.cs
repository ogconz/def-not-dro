using System;
using System.Runtime.InteropServices;
using CodeStage.AntiCheat.Detectors;
using UnityEngine;

namespace CodeStage.AntiCheat.ObscuredTypes
{
	[Serializable]
	public struct ObscuredFloat : IEquatable<ObscuredFloat>, IFormattable
	{
		[StructLayout(LayoutKind.Explicit)]
		private struct FloatIntBytesUnion
		{
			[FieldOffset(0)]
			public float f;

			[FieldOffset(0)]
			public int i;

			[FieldOffset(0)]
			public byte b1;

			[FieldOffset(1)]
			public byte b2;

			[FieldOffset(2)]
			public byte b3;

			[FieldOffset(3)]
			public byte b4;
		}

		private static int cryptoKey = 546748334;

		[SerializeField]
		private int currentCryptoKey;

		[SerializeField]
		private byte[] hiddenValue;

		[SerializeField]
		private float fakeValue;

		[SerializeField]
		private bool inited;

		private ObscuredFloat(byte[] value)
		{
			currentCryptoKey = cryptoKey;
			hiddenValue = value;
			fakeValue = 0f;
			inited = true;
		}

		public ObscuredFloat(float value)
		{
			currentCryptoKey = cryptoKey;
			hiddenValue = null;
			fakeValue = 0f;
			inited = false;
			InternalEncrypt(ref hiddenValue, value);
		}

		public static void SetNewCryptoKey(int newKey)
		{
			cryptoKey = newKey;
		}

		public void ApplyNewCryptoKey()
		{
			if (currentCryptoKey != cryptoKey)
			{
				InternalEncrypt(ref hiddenValue, InternalDecrypt(), cryptoKey);
				currentCryptoKey = cryptoKey;
			}
		}

		public static int Encrypt(float value)
		{
			return Encrypt(value, cryptoKey);
		}

		public static int Encrypt(float value, int key)
		{
			FloatIntBytesUnion floatIntBytesUnion = new FloatIntBytesUnion
			{
				f = value
			};
			floatIntBytesUnion.i ^= key;
			return floatIntBytesUnion.i;
		}

		private static void InternalEncrypt(ref byte[] bytes, float value)
		{
			InternalEncrypt(ref bytes, value, 0);
		}

		private static void InternalEncrypt(ref byte[] bytes, float value, int key)
		{
			int num = key;
			if (num == 0)
			{
				num = cryptoKey;
			}
			FloatIntBytesUnion floatIntBytesUnion = new FloatIntBytesUnion
			{
				f = value
			};
			floatIntBytesUnion.i ^= num;
			if (bytes == null)
			{
				bytes = new byte[4];
			}
			bytes[0] = floatIntBytesUnion.b1;
			bytes[1] = floatIntBytesUnion.b2;
			bytes[2] = floatIntBytesUnion.b3;
			bytes[3] = floatIntBytesUnion.b4;
		}

		public static float Decrypt(int value)
		{
			return Decrypt(value, cryptoKey);
		}

		public static float Decrypt(int value, int key)
		{
			FloatIntBytesUnion floatIntBytesUnion = new FloatIntBytesUnion
			{
				i = (value ^ key)
			};
			return floatIntBytesUnion.f;
		}

		public int GetEncrypted()
		{
			ApplyNewCryptoKey();
			FloatIntBytesUnion floatIntBytesUnion = new FloatIntBytesUnion
			{
				b1 = hiddenValue[0],
				b2 = hiddenValue[1],
				b3 = hiddenValue[2],
				b4 = hiddenValue[3]
			};
			return floatIntBytesUnion.i;
		}

		public void SetEncrypted(int encrypted)
		{
			inited = true;
			FloatIntBytesUnion floatIntBytesUnion = new FloatIntBytesUnion
			{
				i = encrypted
			};
			hiddenValue = new byte[4] { floatIntBytesUnion.b1, floatIntBytesUnion.b2, floatIntBytesUnion.b3, floatIntBytesUnion.b4 };
			if (ObscuredCheatingDetector.isRunning)
			{
				fakeValue = InternalDecrypt();
			}
		}

		private float InternalDecrypt()
		{
			if (!inited)
			{
				currentCryptoKey = cryptoKey;
				InternalEncrypt(ref hiddenValue, 0f);
				fakeValue = 0f;
				inited = true;
			}
			int num = cryptoKey;
			if (currentCryptoKey != cryptoKey)
			{
				num = currentCryptoKey;
			}
			FloatIntBytesUnion floatIntBytesUnion = new FloatIntBytesUnion
			{
				b1 = hiddenValue[0],
				b2 = hiddenValue[1],
				b3 = hiddenValue[2],
				b4 = hiddenValue[3]
			};
			floatIntBytesUnion.i ^= num;
			float f = floatIntBytesUnion.f;
			if (ObscuredCheatingDetector.isRunning && fakeValue != 0f && Math.Abs(f - fakeValue) > ObscuredCheatingDetector.Instance.floatEpsilon)
			{
				ObscuredCheatingDetector.Instance.OnCheatingDetected();
			}
			return f;
		}

		public float Get()
		{
			return InternalDecrypt();
		}

		public void Set(float value)
		{
			if (!inited)
			{
				currentCryptoKey = cryptoKey;
				fakeValue = 0f;
				inited = true;
			}
			InternalEncrypt(ref hiddenValue, value);
		}

		public void Add(float value)
		{
			Set(InternalDecrypt() + value);
		}

		public static ObscuredFloat operator ++(ObscuredFloat input)
		{
			float value = input.InternalDecrypt() + 1f;
			InternalEncrypt(ref input.hiddenValue, value, input.currentCryptoKey);
			if (ObscuredCheatingDetector.isRunning)
			{
				input.fakeValue = value;
			}
			return input;
		}

		public static ObscuredFloat operator --(ObscuredFloat input)
		{
			float value = input.InternalDecrypt() - 1f;
			InternalEncrypt(ref input.hiddenValue, value, input.currentCryptoKey);
			if (ObscuredCheatingDetector.isRunning)
			{
				input.fakeValue = value;
			}
			return input;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is ObscuredFloat obscuredFloat))
			{
				return false;
			}
			float num = obscuredFloat.InternalDecrypt();
			float num2 = InternalDecrypt();
			if ((double)num == (double)num2)
			{
				return true;
			}
			if (float.IsNaN(num))
			{
				return float.IsNaN(num2);
			}
			return false;
		}

		public bool Equals(ObscuredFloat obj)
		{
			float num = obj.InternalDecrypt();
			float num2 = InternalDecrypt();
			if ((double)num == (double)num2)
			{
				return true;
			}
			if (float.IsNaN(num))
			{
				return float.IsNaN(num2);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return InternalDecrypt().GetHashCode();
		}

		public override string ToString()
		{
			return InternalDecrypt().ToString();
		}

		public string ToString(string format)
		{
			return InternalDecrypt().ToString(format);
		}

		public string ToString(IFormatProvider provider)
		{
			return InternalDecrypt().ToString(provider);
		}

		public string ToString(string format, IFormatProvider provider)
		{
			return InternalDecrypt().ToString(format, provider);
		}
	}
}
