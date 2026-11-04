using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using BestHTTP.Decompression.Zlib;
using CarX;
using CarX.Platform;
using GameOverlay;
using SimpleJSON;
using SyncMultiplayer.Objects;
using UnityEngine;
using VinylSystem;

namespace SyncMultiplayer
{
	public static class CustomTypes
	{
		[Flags]
		private enum ClutchFlag
		{
			None = 0,
			AutoClutch = 1,
			ClutchKick = 2
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct BytetoFloatConverter
		{
			[FieldOffset(0)]
			public int Int;

			[FieldOffset(0)]
			public float Float;
		}

		private static MemoryStream m_cachedMemoryStream = new MemoryStream(10240);

		private static byte[] m_bytes = new byte[4096];

		private static void WriteInt(this MemoryStream stream, int value)
		{
			stream.WriteByte((byte)(value & 0xFF));
			stream.WriteByte((byte)((value >> 8) & 0xFF));
			stream.WriteByte((byte)((value >> 16) & 0xFF));
			stream.WriteByte((byte)((value >> 24) & 0xFF));
		}

		private static void WriteFloat(this MemoryStream stream, float value)
		{
			BytetoFloatConverter bytetoFloatConverter = new BytetoFloatConverter
			{
				Float = value
			};
			stream.WriteInt(bytetoFloatConverter.Int);
		}

		private static void WriteULong(this MemoryStream stream, ulong value)
		{
			stream.WriteInt((int)(value & 0xFFFFFFFFu));
			stream.WriteInt((int)((value >> 32) & 0xFFFFFFFFu));
		}

		private static void WriteInt16(this MemoryStream stream, short value)
		{
			stream.WriteByte((byte)(value & 0xFF));
			stream.WriteByte((byte)((value >> 8) & 0xFF));
		}

		public static byte[] ToByteArray<T>(this T structure) where T : struct
		{
			int num = Marshal.SizeOf(structure);
			byte[] array = new byte[num];
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			try
			{
				Marshal.StructureToPtr(structure, intPtr, fDeleteOld: true);
				Marshal.Copy(intPtr, array, 0, num);
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message);
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr);
			}
			return array;
		}

		public static T ToStructure<T>(this byte[] byteArray, int start) where T : struct
		{
			int num = Marshal.SizeOf<T>();
			if (byteArray.Length - start < num)
			{
				Debug.LogError("Marshal out of range" + typeof(T).Name);
				return default(T);
			}
			T result = default(T);
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			try
			{
				Marshal.Copy(byteArray, start, intPtr, num);
				result = Marshal.PtrToStructure<T>(intPtr);
				return result;
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message);
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr);
			}
			return result;
		}

		private static MemoryStream GetMemoryStream(int capacity)
		{
			m_cachedMemoryStream.SetLength(0L);
			return m_cachedMemoryStream;
		}

		public static byte[] SerializePlayerIdList(List<PlayerId> vo)
		{
			MemoryStream memoryStream = GetMemoryStream((4 + Marshal.SizeOf<AccountId>()) * vo.Count + 4);
			memoryStream.WriteInt(vo.Count);
			foreach (PlayerId item in vo)
			{
				memoryStream.WriteInt((int)item.platform);
				byte[] array = item.accountId.ToByteArray();
				for (int i = 0; i < array.Length; i++)
				{
					memoryStream.WriteByte(array[i]);
				}
			}
			return memoryStream.ToArray();
		}

		public static List<PlayerId> DeserializePlayerIdList(byte[] bytes)
		{
			int num = BitConverter.ToInt32(bytes, 0);
			if (num > 1000 || num < 0)
			{
				num = 0;
			}
			int num2 = 4 + Marshal.SizeOf<AccountId>();
			List<PlayerId> list = new List<PlayerId>(num);
			for (int i = 0; i < num; i++)
			{
				int num3 = 4 + i * num2;
				int platform = BitConverter.ToInt32(bytes, num3);
				AccountId id = bytes.ToStructure<AccountId>(num3 + 4);
				list.Add(new PlayerId((UserPlatform.Id)platform, id));
			}
			return list;
		}

		public static byte[] SerializeCarxInputControls(RaceCar car, float mileage)
		{
			MemoryStream memoryStream = GetMemoryStream(61);
			if (car == null)
			{
				memoryStream.WriteInt16(0);
				memoryStream.WriteInt16(0);
				memoryStream.WriteInt16(0);
				memoryStream.WriteInt16(0);
				memoryStream.WriteByte(0);
				memoryStream.WriteByte(0);
				SerializeVector3(Vector3.zero, memoryStream);
				SerializeQuaternion(Quaternion.identity, memoryStream);
				SerializeVector3(Vector3.zero, memoryStream);
				SerializeVector3(Vector3.zero, memoryStream);
			}
			else
			{
				Car carX = car.carX;
				Rigidbody getRigidbody = car.getRigidbody;
				memoryStream.WriteInt16((short)(carX.accelerate * 10000f));
				memoryStream.WriteInt16((short)(carX.GetSteer() * 10000f));
				memoryStream.WriteInt16((short)(carX.brake * 10000f));
				memoryStream.WriteInt16((short)(carX.handbrake * 10000f));
				memoryStream.WriteByte((byte)carX.gear);
				memoryStream.WriteByte((byte)(((bool)car.flares && car.flares.flares.activeSelf) ? 1u : 0u));
				SerializeVector3(getRigidbody.position, memoryStream);
				SerializeQuaternion(getRigidbody.rotation, memoryStream);
				SerializeVector3(getRigidbody.velocity, memoryStream);
				SerializeVector3(getRigidbody.angularVelocity, memoryStream);
				memoryStream.WriteFloat(carX.longAccel);
				memoryStream.WriteFloat(carX.sideAccel);
				memoryStream.WriteFloat(carX.angAccel);
				memoryStream.WriteByte((byte)(car.engineController.isIgnitionOn ? 1u : 0u));
				memoryStream.WriteByte((byte)(car.engineController.isStarterOn ? 1u : 0u));
				memoryStream.WriteFloat(car.engineController.engineStartTime);
				memoryStream.WriteInt16((short)(car.raceCarClutchInputVal * 10000f));
				memoryStream.WriteInt16((short)carX.rpm);
				memoryStream.WriteByte(GetClutchState(carX.autoClutch, car.isClutchKick));
				memoryStream.WriteFloat(mileage);
			}
			return memoryStream.ToArray();
		}

		public static byte[] SerializeCarxDefaultData(RaceCar car, float mileage)
		{
			MemoryStream memoryStream = GetMemoryStream(61);
			Car carX = car.carX;
			Rigidbody getRigidbody = car.getRigidbody;
			memoryStream.WriteInt16(0);
			memoryStream.WriteInt16(0);
			memoryStream.WriteInt16(0);
			memoryStream.WriteInt16(0);
			memoryStream.WriteByte(0);
			memoryStream.WriteByte(0);
			SerializeVector3(getRigidbody.position, memoryStream);
			SerializeQuaternion(getRigidbody.rotation, memoryStream);
			SerializeVector3(Vector3.zero, memoryStream);
			SerializeVector3(Vector3.zero, memoryStream);
			memoryStream.WriteFloat(0f);
			memoryStream.WriteFloat(0f);
			memoryStream.WriteFloat(0f);
			memoryStream.WriteByte(0);
			memoryStream.WriteByte(0);
			memoryStream.WriteFloat(0f);
			memoryStream.WriteInt16(0);
			memoryStream.WriteInt16(0);
			memoryStream.WriteByte(GetClutchState(carX.autoClutch, car.isClutchKick));
			memoryStream.WriteFloat(mileage);
			return memoryStream.ToArray();
		}

		public static PosDataFrame DeserializeCarxInputControls(byte[] bytes)
		{
			PosDataFrame result = PosDataFrame.Default;
			result.modelTime = 0L;
			result.posGeneration = 0;
			result.flags = DataFrameFlags.None;
			result.accelerate = (float)BitConverter.ToInt16(bytes, 0) / 10000f;
			result.steer = (float)BitConverter.ToInt16(bytes, 2) / 10000f;
			result.brake = (float)BitConverter.ToInt16(bytes, 4) / 10000f;
			result.handbrake = (float)BitConverter.ToInt16(bytes, 6) / 10000f;
			result.gear = bytes[8];
			result.flares = TryFindDataByIndex(bytes, 9);
			result.position = DeserializeVector3(bytes, 10);
			result.rotation = DeserializeQuaternion(bytes, 22);
			result.velocity = DeserializeVector3(bytes, 38);
			result.angularVelocity = DeserializeVector3(bytes, 50);
			if (bytes.Length > 62)
			{
				result.longAccel = BitConverter.ToSingle(bytes, 62);
				result.sideAccel = BitConverter.ToSingle(bytes, 66);
				result.angAccel = BitConverter.ToSingle(bytes, 70);
			}
			if (bytes.Length > 74)
			{
				result.processEngineStart = true;
				result.ignitionEnabled = TryFindDataByIndex(bytes, 74);
				result.starterEnabled = TryFindDataByIndex(bytes, 75);
				result.engineStartTime = BitConverter.ToSingle(bytes, 76);
				result.clutch = (float)BitConverter.ToInt16(bytes, 80) / 10000f;
				result.rpm = BitConverter.ToInt16(bytes, 82);
				result.autoClutch = true;
				result.clutchKick = false;
			}
			else
			{
				result.processEngineStart = false;
			}
			if (bytes.Length > 84)
			{
				ParseClutchState(bytes[84], out result.autoClutch, out result.clutchKick);
			}
			if (bytes.Length > 85)
			{
				result.mileage = BitConverter.ToSingle(bytes, 85);
			}
			return result;
		}

		private static byte GetClutchState(bool autoClutch, bool clutchKick)
		{
			ClutchFlag clutchFlag = ClutchFlag.None;
			if (autoClutch)
			{
				clutchFlag |= ClutchFlag.AutoClutch;
			}
			if (clutchKick)
			{
				clutchFlag |= ClutchFlag.ClutchKick;
			}
			return (byte)clutchFlag;
		}

		private static void ParseClutchState(byte state, out bool autoClutch, out bool clutchKick)
		{
			autoClutch = false;
			clutchKick = false;
			if (((ClutchFlag)state).HasFlag(ClutchFlag.AutoClutch))
			{
				autoClutch = true;
			}
			if (((ClutchFlag)state).HasFlag(ClutchFlag.ClutchKick))
			{
				clutchKick = true;
			}
		}

		private static bool TryFindDataByIndex(byte[] bytes, int infoIdx)
		{
			if (infoIdx < 0 || infoIdx >= bytes.Length)
			{
				return false;
			}
			return bytes[infoIdx] != 0;
		}

		public static byte[] SerializeSubroom(NetGameSyncTimeSystem syncTime, SubroomInfo customobject)
		{
			float createTime = customobject.createTime;
			float launchTime = customobject.launchTime;
			customobject.createTime = syncTime.ConvertToModel(createTime);
			customobject.launchTime = ((launchTime == 0f) ? 0f : syncTime.ConvertToModel(launchTime));
			byte[] result = SerializeByJSON<SubroomInfo>(customobject);
			customobject.createTime = createTime;
			customobject.launchTime = launchTime;
			return result;
		}

		public static SubroomInfo DeserializeSubroom(NetGameSyncTimeSystem syncTime, byte[] bytes)
		{
			try
			{
				SubroomInfo subroomInfo = null;
				subroomInfo = ((!GameConsole.Bool["sv_crazyNetworkChallenges"]) ? DeserializeByJSON<SubroomInfo>(bytes) : JSON.Parse<SubroomInfo>(Unzip(bytes).Replace("TandemXDS", "BlaZazaTor")));
				subroomInfo.createTime = syncTime.ConvertToLocal(subroomInfo.createTime);
				subroomInfo.launchTime = ((subroomInfo.launchTime == 0f) ? 0f : syncTime.ConvertToLocal(subroomInfo.launchTime));
				return subroomInfo;
			}
			catch
			{
				return null;
			}
		}

		public static byte[] SerializeCarProfile(ProfilesInfo.Profile customobject)
		{
			return SerializeByJSON<ProfilesInfo.Profile>(customobject);
		}

		public static ProfilesInfo.Profile DeserializeCarProfile(byte[] bytes)
		{
			return DeserializeByJSON<ProfilesInfo.Profile>(bytes);
		}

		public static byte[] SerializeLiveryLayers(List<VinylLayer> layers)
		{
			return Encoding.UTF8.GetBytes(JSON.Serialize(layers));
		}

		public static List<VinylLayer> DeserializeLiveryLayers(byte[] data)
		{
			return JSON.Parse<List<VinylLayer>>(Encoding.UTF8.GetString(data));
		}

		public static byte[] SerializeCarVS(CarVisualManager.VisualSettings customobject)
		{
			return SerializeByJSON<CarVisualManager.VisualSettings>(customobject);
		}

		public static CarVisualManager.VisualSettings DeserializeCarVS(byte[] bytes)
		{
			return DeserializeByJSON<CarVisualManager.VisualSettings>(bytes);
		}

		public static byte[] SerializeNetworkObject(this NetworkObject netObj, SyncPacketType type)
		{
			MemoryStream memoryStream = GetMemoryStream(0);
			foreach (BaseObjectSyncModel syncModel in netObj.syncModels)
			{
				syncModel.WritePackage(memoryStream, type);
			}
			return memoryStream.ToArray();
		}

		public static void SerializeVector3(Vector3 vo, MemoryStream ms)
		{
			ms.WriteFloat(vo.x);
			ms.WriteFloat(vo.y);
			ms.WriteFloat(vo.z);
		}

		public static Vector3 DeserializeVector3(byte[] bytes, int offset = 0)
		{
			return new Vector3
			{
				x = BitConverter.ToSingle(bytes, offset),
				y = BitConverter.ToSingle(bytes, offset + 4),
				z = BitConverter.ToSingle(bytes, offset + 8)
			};
		}

		public static void SerializeQuaternion(Quaternion o, MemoryStream ms)
		{
			ms.WriteFloat(o.w);
			ms.WriteFloat(o.x);
			ms.WriteFloat(o.y);
			ms.WriteFloat(o.z);
		}

		public static Quaternion DeserializeQuaternion(byte[] bytes, int offset = 0)
		{
			return new Quaternion
			{
				w = BitConverter.ToSingle(bytes, offset),
				x = BitConverter.ToSingle(bytes, offset + 4),
				y = BitConverter.ToSingle(bytes, offset + 8),
				z = BitConverter.ToSingle(bytes, offset + 12)
			};
		}

		public static byte[] SerializeByJSON<T>(object customobject) where T : class
		{
			return Zip(JSON.Serialize(customobject as T));
		}

		public static T DeserializeByJSON<T>(byte[] bytes) where T : class
		{
			return JSON.Parse<T>(Unzip(bytes));
		}

		public static void CopyTo(Stream src, Stream dest)
		{
			int count;
			while ((count = src.Read(m_bytes, 0, m_bytes.Length)) != 0)
			{
				dest.Write(m_bytes, 0, count);
			}
		}

		public static byte[] Zip(string str)
		{
			byte[] array = new byte[str.Length * 2];
			Buffer.BlockCopy(str.ToCharArray(), 0, array, 0, array.Length);
			using MemoryStream src = new MemoryStream(array);
			using MemoryStream memoryStream = new MemoryStream();
			using (GZipStream dest = new GZipStream(memoryStream, CompressionMode.Compress))
			{
				CopyTo(src, dest);
			}
			return memoryStream.ToArray();
		}

		public static string Unzip(byte[] bytes)
		{
			using MemoryStream stream = new MemoryStream(bytes);
			using MemoryStream memoryStream = new MemoryStream();
			using (GZipStream src = new GZipStream(stream, CompressionMode.Decompress))
			{
				CopyTo(src, memoryStream);
			}
			bytes = memoryStream.ToArray();
			char[] array = new char[bytes.Length / 2];
			Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
			return new string(array);
		}
	}
}
