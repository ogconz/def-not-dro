using System;
using System.Runtime.InteropServices;
using CodeStage.AntiCheat.Detectors;

namespace CodeStage.AntiCheat.ObscuredTypes
{
	[Serializable]
	public struct ObscuredDecimal : IEquatable<ObscuredDecimal>, IFormattable
	{
		[StructLayout(LayoutKind.Explicit)]
		private struct DecimalLongBytesUnion
		{
			[FieldOffset(0)]
			public decimal d;

			[FieldOffset(0)]
			public long l1;

			[FieldOffset(8)]
			public long l2;

			[FieldOffset(0)]
			public byte b1;

			[FieldOffset(1)]
			public byte b2;

			[FieldOffset(2)]
			public byte b3;

			[FieldOffset(3)]
			public byte b4;

			[FieldOffset(4)]
			public byte b5;

			[FieldOffset(5)]
			public byte b6;

			[FieldOffset(6)]
			public byte b7;

			[FieldOffset(7)]
			public byte b8;

			[FieldOffset(8)]
			public byte b9;

			[FieldOffset(9)]
			public byte b10;

			[FieldOffset(10)]
			public byte b11;

			[FieldOffset(11)]
			public byte b12;

			[FieldOffset(12)]
			public byte b13;

			[FieldOffset(13)]
			public byte b14;

			[FieldOffset(14)]
			public byte b15;

			[FieldOffset(15)]
			public byte b16;
		}

		private static long cryptoKey = 209208L;

		private long currentCryptoKey;

		private byte[] hiddenValue;

		private decimal fakeValue;

		private bool inited;

		private ObscuredDecimal(byte[] value)
		{
			currentCryptoKey = cryptoKey;
			hiddenValue = value;
			fakeValue = default(decimal);
			inited = true;
		}

		public static void SetNewCryptoKey(long newKey)
		{
			cryptoKey = newKey;
		}

		public void ApplyNewCryptoKey()
		{
			if (currentCryptoKey != cryptoKey)
			{
				hiddenValue = InternalEncrypt(InternalDecrypt(), cryptoKey);
				currentCryptoKey = cryptoKey;
			}
		}

		public static decimal Encrypt(decimal value)
		{
			return Encrypt(value, cryptoKey);
		}

		public static decimal Encrypt(decimal value, long key)
		{
			DecimalLongBytesUnion decimalLongBytesUnion = new DecimalLongBytesUnion
			{
				d = value
			};
			decimalLongBytesUnion.l1 ^= key;
			decimalLongBytesUnion.l2 ^= key;
			return decimalLongBytesUnion.d;
		}

		private static byte[] InternalEncrypt(decimal value)
		{
			return InternalEncrypt(value, 0L);
		}

		private static byte[] InternalEncrypt(decimal value, long key)
		{
			long num = key;
			if (num == 0L)
			{
				num = cryptoKey;
			}
			DecimalLongBytesUnion decimalLongBytesUnion = new DecimalLongBytesUnion
			{
				d = value
			};
			decimalLongBytesUnion.l1 ^= num;
			decimalLongBytesUnion.l2 ^= num;
			return new byte[16]
			{
				decimalLongBytesUnion.b1, decimalLongBytesUnion.b2, decimalLongBytesUnion.b3, decimalLongBytesUnion.b4, decimalLongBytesUnion.b5, decimalLongBytesUnion.b6, decimalLongBytesUnion.b7, decimalLongBytesUnion.b8, decimalLongBytesUnion.b9, decimalLongBytesUnion.b10,
				decimalLongBytesUnion.b11, decimalLongBytesUnion.b12, decimalLongBytesUnion.b13, decimalLongBytesUnion.b14, decimalLongBytesUnion.b15, decimalLongBytesUnion.b16
			};
		}

		public static decimal Decrypt(decimal value)
		{
			return Decrypt(value, cryptoKey);
		}

		public static decimal Decrypt(decimal value, long key)
		{
			DecimalLongBytesUnion decimalLongBytesUnion = new DecimalLongBytesUnion
			{
				d = value
			};
			decimalLongBytesUnion.l1 ^= key;
			decimalLongBytesUnion.l2 ^= key;
			return decimalLongBytesUnion.d;
		}

		public decimal GetEncrypted()
		{
			ApplyNewCryptoKey();
			DecimalLongBytesUnion decimalLongBytesUnion = new DecimalLongBytesUnion
			{
				b1 = hiddenValue[0],
				b2 = hiddenValue[1],
				b3 = hiddenValue[2],
				b4 = hiddenValue[3],
				b5 = hiddenValue[4],
				b6 = hiddenValue[5],
				b7 = hiddenValue[6],
				b8 = hiddenValue[7],
				b9 = hiddenValue[8],
				b10 = hiddenValue[9],
				b11 = hiddenValue[10],
				b12 = hiddenValue[11],
				b13 = hiddenValue[12],
				b14 = hiddenValue[13],
				b15 = hiddenValue[14],
				b16 = hiddenValue[15]
			};
			return decimalLongBytesUnion.d;
		}

		public void SetEncrypted(decimal encrypted)
		{
			inited = true;
			DecimalLongBytesUnion decimalLongBytesUnion = new DecimalLongBytesUnion
			{
				d = encrypted
			};
			hiddenValue = new byte[16]
			{
				decimalLongBytesUnion.b1, decimalLongBytesUnion.b2, decimalLongBytesUnion.b3, decimalLongBytesUnion.b4, decimalLongBytesUnion.b5, decimalLongBytesUnion.b6, decimalLongBytesUnion.b7, decimalLongBytesUnion.b8, decimalLongBytesUnion.b9, decimalLongBytesUnion.b10,
				decimalLongBytesUnion.b11, decimalLongBytesUnion.b12, decimalLongBytesUnion.b13, decimalLongBytesUnion.b14, decimalLongBytesUnion.b15, decimalLongBytesUnion.b16
			};
			if (ObscuredCheatingDetector.isRunning)
			{
				fakeValue = InternalDecrypt();
			}
		}

		private decimal InternalDecrypt()
		{
			if (!inited)
			{
				currentCryptoKey = cryptoKey;
				hiddenValue = InternalEncrypt(0m);
				fakeValue = default(decimal);
				inited = true;
			}
			long num = cryptoKey;
			if (currentCryptoKey != cryptoKey)
			{
				num = currentCryptoKey;
			}
			DecimalLongBytesUnion decimalLongBytesUnion = new DecimalLongBytesUnion
			{
				b1 = hiddenValue[0],
				b2 = hiddenValue[1],
				b3 = hiddenValue[2],
				b4 = hiddenValue[3],
				b5 = hiddenValue[4],
				b6 = hiddenValue[5],
				b7 = hiddenValue[6],
				b8 = hiddenValue[7],
				b9 = hiddenValue[8],
				b10 = hiddenValue[9],
				b11 = hiddenValue[10],
				b12 = hiddenValue[11],
				b13 = hiddenValue[12],
				b14 = hiddenValue[13],
				b15 = hiddenValue[14],
				b16 = hiddenValue[15]
			};
			decimalLongBytesUnion.l1 ^= num;
			decimalLongBytesUnion.l2 ^= num;
			decimal d = decimalLongBytesUnion.d;
			if (ObscuredCheatingDetector.isRunning && fakeValue != 0m && d != fakeValue)
			{
				ObscuredCheatingDetector.Instance.OnCheatingDetected();
			}
			return d;
		}

		public static implicit operator ObscuredDecimal(decimal value)
		{
			ObscuredDecimal result = new ObscuredDecimal(InternalEncrypt(value));
			if (ObscuredCheatingDetector.isRunning)
			{
				result.fakeValue = value;
			}
			return result;
		}

		public static implicit operator decimal(ObscuredDecimal value)
		{
			return value.InternalDecrypt();
		}

		public static ObscuredDecimal operator ++(ObscuredDecimal input)
		{
			decimal value = input.InternalDecrypt() + 1m;
			input.hiddenValue = InternalEncrypt(value, input.currentCryptoKey);
			if (ObscuredCheatingDetector.isRunning)
			{
				input.fakeValue = value;
			}
			return input;
		}

		public static ObscuredDecimal operator --(ObscuredDecimal input)
		{
			decimal value = input.InternalDecrypt() - 1m;
			input.hiddenValue = InternalEncrypt(value, input.currentCryptoKey);
			if (ObscuredCheatingDetector.isRunning)
			{
				input.fakeValue = value;
			}
			return input;
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

		public override bool Equals(object obj)
		{
			if (!(obj is ObscuredDecimal obscuredDecimal))
			{
				return false;
			}
			return obscuredDecimal.InternalDecrypt().Equals(InternalDecrypt());
		}

		public bool Equals(ObscuredDecimal obj)
		{
			return obj.InternalDecrypt().Equals(InternalDecrypt());
		}

		public override int GetHashCode()
		{
			return InternalDecrypt().GetHashCode();
		}
	}
}
