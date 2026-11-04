using System;
using System.Collections.Generic;
using System.IO;
using CodeStage.AntiCheat.ObscuredTypes;
using GameOverlay;
using SyncMultiplayer;
using UnityEngine;

public class GhostData
{
	public enum ReplayStreamType : byte
	{
		Position = 0,
		Rotation = 1,
		Steer = 2,
		RPM = 3,
		Velocity = 4,
		AngVelocity = 5,
		InputStates = 6,
		Lights = 7,
		Ignition = 8,
		Accel = 9,
		Starter = 10,
		Clutch = 11
	}

	private const int GhostFileVersion = 9;

	private const float PosEpsilon = 0.1f;

	private const float RotEpsilon = 0.01f;

	private const float SteerEpsilon = 0.1f;

	private const float RpmEpsilon = 100f;

	private const float VeloEpsilon = 0.5f;

	private const float AngVeloEpsilon = 0.5f;

	private static GhostDataReaderV3 m_defaultReader = new GhostDataReaderV3();

	private static Dictionary<int, GhostDataReader> m_loaders = new Dictionary<int, GhostDataReader>
	{
		{ 0, m_defaultReader },
		{ 1, m_defaultReader },
		{ 2, m_defaultReader },
		{ 3, m_defaultReader },
		{
			4,
			new GhostDataReaderV4()
		},
		{
			5,
			new GhostDataReaderV5()
		},
		{
			6,
			new GhostDataReaderV6()
		},
		{
			7,
			new GhostDataReaderV7()
		},
		{
			8,
			new GhostDataReaderV8()
		},
		{
			9,
			new GhostDataReaderV9()
		}
	};

	private readonly Vector3ReplayStream m_positionStream = new Vector3ReplayStream();

	private readonly QuaternionReplayStream m_rotationStream = new QuaternionReplayStream();

	protected readonly FloatReplayStream m_steerStream = new FloatReplayStream();

	protected readonly FloatReplayStream m_rpmStream = new FloatReplayStream();

	private readonly Vector3ReplayStream m_velocityStream = new Vector3ReplayStream();

	private readonly Vector3ReplayStream m_angVelocityStream = new Vector3ReplayStream();

	protected readonly InputStatesReplayStream m_inputStatesStream = new InputStatesReplayStream();

	protected readonly FloatReplayStream m_lightsStream = new FloatReplayStream();

	protected readonly FloatReplayStream m_ignitionStream = new FloatReplayStream();

	protected readonly FloatReplayStream m_accelStream = new FloatReplayStream();

	protected readonly StarterStatesReplayStream m_starterStream = new StarterStatesReplayStream();

	protected readonly FloatReplayStream m_clutchStream = new FloatReplayStream();

	private ObscuredInt m_carId;

	private ObscuredInt m_profileId;

	private ObscuredInt m_swapId;

	private ObscuredInt m_trackId;

	private ObscuredFloat m_greenLightTime;

	public ProfilesInfo.Profile profile;

	public CarVisualManager.VisualSettings visual;

	public int tempSlotId;

	public float greenLightTime
	{
		get
		{
			return m_greenLightTime.Get();
		}
		set
		{
			m_greenLightTime.Set(value);
		}
	}

	public int carId
	{
		get
		{
			return m_carId;
		}
		set
		{
			m_carId = value;
		}
	}

	public int profileId
	{
		get
		{
			return m_profileId;
		}
		set
		{
			m_profileId = value;
		}
	}

	public int swapId
	{
		get
		{
			return m_swapId;
		}
		set
		{
			m_swapId = value;
		}
	}

	public int trackId
	{
		get
		{
			return m_trackId;
		}
		set
		{
			m_trackId = value;
		}
	}

	public void Clear()
	{
		m_positionStream.Clear();
		m_rotationStream.Clear();
		m_steerStream.Clear();
		m_rpmStream.Clear();
		m_velocityStream.Clear();
		m_angVelocityStream.Clear();
		m_inputStatesStream.Clear();
		m_lightsStream.Clear();
		m_ignitionStream.Clear();
		m_accelStream.Clear();
		m_starterStream.Clear();
		m_clutchStream.Clear();
	}

	public void CopyFrom(GhostData source)
	{
		if (source == null)
		{
			Clear();
			return;
		}
		m_positionStream.CopyFrom(source.m_positionStream);
		m_rotationStream.CopyFrom(source.m_rotationStream);
		m_steerStream.CopyFrom(source.m_steerStream);
		m_rpmStream.CopyFrom(source.m_rpmStream);
		m_velocityStream.CopyFrom(source.m_velocityStream);
		m_angVelocityStream.CopyFrom(source.m_angVelocityStream);
		m_inputStatesStream.CopyFrom(source.m_inputStatesStream);
		m_lightsStream.CopyFrom(source.m_lightsStream);
		m_ignitionStream.CopyFrom(source.m_ignitionStream);
		m_accelStream.CopyFrom(source.m_accelStream);
		m_starterStream.CopyFrom(source.m_starterStream);
		m_clutchStream.CopyFrom(source.m_clutchStream);
	}

	public bool HasPlaybackData()
	{
		if (m_positionStream.IsEmpty() && m_rotationStream.IsEmpty() && m_steerStream.IsEmpty() && m_rpmStream.IsEmpty() && m_velocityStream.IsEmpty() && m_angVelocityStream.IsEmpty() && m_inputStatesStream.IsEmpty() && m_lightsStream.IsEmpty() && m_ignitionStream.IsEmpty() && m_accelStream.IsEmpty() && m_starterStream.IsEmpty())
		{
			return !m_clutchStream.IsEmpty();
		}
		return true;
	}

	public void WriteBefore(GhostData parent, float time)
	{
		m_positionStream.WriteBefore(parent.m_positionStream, time, 0.1f);
		m_rotationStream.WriteBefore(parent.m_rotationStream, time, 0.01f);
		m_steerStream.WriteBefore(parent.m_steerStream, time, 0.1f);
		m_rpmStream.WriteBefore(parent.m_rpmStream, time, 100f);
		m_velocityStream.WriteBefore(parent.m_velocityStream, time, 0.5f);
		m_angVelocityStream.WriteBefore(parent.m_angVelocityStream, time, 0.5f);
		m_inputStatesStream.WriteBefore(parent.m_inputStatesStream, time);
		m_lightsStream.WriteBefore(parent.m_lightsStream, time);
		m_ignitionStream.WriteBefore(parent.m_ignitionStream, time);
		m_accelStream.WriteBefore(parent.m_accelStream, time);
		m_starterStream.WriteBefore(parent.m_starterStream, time);
		m_clutchStream.WriteBefore(parent.m_clutchStream, time);
	}

	public void RecordRaceCar(float time, RaceCar car)
	{
		if (!car.IsPhysicsBroken())
		{
			InputStatesReplay val = default(InputStatesReplay);
			val.isHandbrake = car.carX.handbrake > 0f || car.lockState;
			val.isBrake = car.carX.brake > 0f;
			val.isFlares = (bool)car.flares && car.flares.flares.activeSelf;
			val.gear = (byte)car.carX.gear;
			m_positionStream.WriteValue(time, car.getRigidbody.position, 0.1f);
			m_rotationStream.WriteValue(time, car.getRigidbody.rotation, 0.01f);
			m_steerStream.WriteValue(time, car.carX.GetSteer(), 0.1f);
			m_rpmStream.WriteValue(time, car.carX.rpm, 100f);
			m_velocityStream.WriteValue(time, car.getRigidbody.velocity, 0.5f);
			m_angVelocityStream.WriteValue(time, car.getRigidbody.angularVelocity, 0.5f);
			m_inputStatesStream.WriteValue(time, val);
			m_lightsStream.WriteValue(time, car.headlightsEnabled ? 1f : 0f);
			m_ignitionStream.WriteValue(time, car.engineController.isIgnitionOn ? 1f : 0f);
			m_accelStream.WriteValue(time, car.carX.accelerate);
			StarterStatesReplay val2 = default(StarterStatesReplay);
			val2.isStarterPressed = car.engineController.isStarterOn;
			val2.isIgnitionOn = car.engineController.isIgnitionOn;
			m_starterStream.WriteValue(time, val2);
			m_clutchStream.WriteValue(time, car.carX.clutch);
		}
	}

	private void PlaybackRaceCarInternal(float time, RaceCar car)
	{
		Transform getTransform = car.getTransform;
		car.getRigidbody.MovePosition(m_positionStream.ReadValue(time, getTransform.position));
		car.getRigidbody.MoveRotation(m_rotationStream.ReadValue(time, getTransform.rotation));
		car.carX.SetSteer(m_steerStream.ReadValue(time, car.carX.GetSteer()), useAssistent: false, Time.deltaTime);
		car.carX.rpm = (m_rpmStream.HasDataFor(time) ? m_rpmStream.ReadValue(time, car.carX.rpm) : 0f);
		car.getRigidbody.velocity = (m_velocityStream.HasDataFor(time) ? m_velocityStream.ReadValue(time, car.getRigidbody.velocity) : Vector3.zero);
		car.getRigidbody.angularVelocity = (m_angVelocityStream.HasDataFor(time) ? m_angVelocityStream.ReadValue(time, car.getRigidbody.angularVelocity) : Vector3.zero);
		if (m_inputStatesStream.HasDataFor(time))
		{
			InputStatesReplay inputStatesReplay = m_inputStatesStream.ReadValue(time, default(InputStatesReplay));
			car.carX.handbrake = (inputStatesReplay.isHandbrake ? 1f : 0f);
			car.carX.brake = (inputStatesReplay.isBrake ? 1f : 0f);
			if (inputStatesReplay.gear == byte.MaxValue)
			{
				car.carX.gear = -1;
			}
			else if (inputStatesReplay.gear != car.carX.gear)
			{
				car.carX.gear = inputStatesReplay.gear;
			}
			if ((bool)car.flares && car.flares.flares.activeSelf != inputStatesReplay.isFlares)
			{
				car.flares.flares.SetActive(inputStatesReplay.isFlares);
			}
		}
		if (m_lightsStream.HasDataFor(time))
		{
			bool flag = false;
			if (DayNightController.instance != null)
			{
				flag = !DayNightController.instance.IsDayLight;
			}
			float num = m_lightsStream.ReadValue(time, flag ? 1f : 0f);
			car.SetHeadlightsEnabled(num > 0f);
		}
		if (m_ignitionStream.HasDataFor(time))
		{
			m_ignitionStream.ReadValue(time, 1f);
		}
		if (m_accelStream.HasDataFor(time))
		{
			car.carX.accelerate = m_accelStream.ReadValue(time, car.carX.accelerate);
		}
		if (m_starterStream.HasDataFor(time))
		{
			StarterStatesReplay starterStatesReplay = m_starterStream.ReadValue(time, default(StarterStatesReplay));
			car.engineController.SetNetworkIgnition(starterStatesReplay.isIgnitionOn);
			car.engineController.StarterNetwork(starterStatesReplay.isStarterPressed, 0.75f);
		}
		if (m_clutchStream.HasDataFor(time))
		{
			car.carX.clutch = m_clutchStream.ReadValue(time, car.carX.clutch);
		}
	}

	public int PlaybackDriftPoints(float time)
	{
		return 0;
	}

	public long GetSizeInBytes()
	{
		long num = 0L;
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter writer = new BinaryWriter(memoryStream);
		SerializeInternal(writer);
		return memoryStream.Length;
	}

	private void SerializeInternal(BinaryWriter writer)
	{
		ReplayStreamType[] array = (ReplayStreamType[])Enum.GetValues(typeof(ReplayStreamType));
		foreach (ReplayStreamType replayStreamType in array)
		{
			IReplayStream streamOfType = GetStreamOfType(replayStreamType);
			if (streamOfType == null || streamOfType.IsEmpty())
			{
				continue;
			}
			byte[] array2;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using BinaryWriter writer2 = new BinaryWriter(memoryStream);
				streamOfType.Serialize(writer2);
				array2 = memoryStream.ToArray();
			}
			writer.Write((byte)replayStreamType);
			writer.Write(array2.Length);
			writer.Write(array2);
		}
	}

	public IReplayStream GetStreamOfType(ReplayStreamType type)
	{
		return type switch
		{
			ReplayStreamType.Position => m_positionStream, 
			ReplayStreamType.Rotation => m_rotationStream, 
			ReplayStreamType.Steer => m_steerStream, 
			ReplayStreamType.RPM => m_rpmStream, 
			ReplayStreamType.Velocity => m_velocityStream, 
			ReplayStreamType.AngVelocity => m_angVelocityStream, 
			ReplayStreamType.InputStates => m_inputStatesStream, 
			ReplayStreamType.Lights => m_lightsStream, 
			ReplayStreamType.Ignition => m_ignitionStream, 
			ReplayStreamType.Accel => m_accelStream, 
			ReplayStreamType.Starter => m_starterStream, 
			ReplayStreamType.Clutch => m_clutchStream, 
			_ => null, 
		};
	}

	public void GetTimeInterval(out float begTime, out float endTime)
	{
		begTime = float.MaxValue;
		endTime = float.MinValue;
		m_positionStream.GetTimeInterval(ref begTime, ref endTime);
		m_rotationStream.GetTimeInterval(ref begTime, ref endTime);
		m_steerStream.GetTimeInterval(ref begTime, ref endTime);
		m_rpmStream.GetTimeInterval(ref begTime, ref endTime);
		m_velocityStream.GetTimeInterval(ref begTime, ref endTime);
		m_angVelocityStream.GetTimeInterval(ref begTime, ref endTime);
		m_inputStatesStream.GetTimeInterval(ref begTime, ref endTime);
		m_lightsStream.GetTimeInterval(ref begTime, ref endTime);
		m_ignitionStream.GetTimeInterval(ref begTime, ref endTime);
		m_accelStream.GetTimeInterval(ref begTime, ref endTime);
		m_starterStream.GetTimeInterval(ref begTime, ref endTime);
		m_clutchStream.GetTimeInterval(ref begTime, ref endTime);
		if (endTime < begTime)
		{
			begTime = 0f;
			endTime = 0f;
		}
	}

	public void RecordDriftPoints(float time, int driftPoints)
	{
	}

	public bool PlaybackRaceCar(float time, RaceCar raceCar)
	{
		if (raceCar == null)
		{
			return false;
		}
		if (HasPlaybackData())
		{
			PlaybackRaceCarInternal(time, raceCar);
			return true;
		}
		return false;
	}

	private static string GetGhostFilePath(int carId, int profileId, int trackId, RaceMode raceMode)
	{
		string text = Application.persistentDataPath + "/Ghosts/";
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		object obj;
		if (raceMode != RaceMode.TimeAttack)
		{
			int num = (int)raceMode;
			obj = num.ToString("00");
		}
		else
		{
			obj = "";
		}
		string text2 = (string)obj;
		text2 = text2 + "_" + Overlay.GetPlayerHashString();
		return text + carId.ToString("00") + profileId.ToString("00") + trackId.ToString("00") + text2 + ".gst";
	}

	public static void DeleteAllGhostData()
	{
		string path = Path.Combine(Application.persistentDataPath, "Ghosts");
		if (Directory.Exists(path))
		{
			Directory.Delete(path, recursive: true);
		}
	}

	public bool Load(int trackId, SubroomMode mode, string pointName)
	{
		string fileName = GetFileName(trackId, pointName, (byte)mode);
		string text = Application.persistentDataPath + "/Ghosts/";
		return LoadInternal(0, 0, trackId, text + fileName + ".gst");
	}

	public bool Load(int trackId, RaceMode raceMode, string configName)
	{
		string fileName = GetFileName(trackId, configName);
		string text = string.Concat(Application.persistentDataPath, "/Ghosts/" + raceMode.ToString() + "/");
		return LoadInternal(0, 0, trackId, text + fileName + ".gst");
	}

	private string GetFileName(int trackId, string dataName, byte mode = byte.MaxValue)
	{
		string text = string.Empty;
		if (mode != byte.MaxValue)
		{
			text = $"{mode}_";
		}
		return text + $"{trackId}_{dataName}_{Overlay.GetPlayerHashString()}";
	}

	public bool Load(int carId, int profileId, int trackId, RaceMode raceMode)
	{
		string ghostFilePath = GetGhostFilePath(carId, profileId, trackId, raceMode);
		return LoadInternal(carId, profileId, trackId, ghostFilePath);
	}

	private bool LoadInternal(int carId, int profileId, int trackId, string path)
	{
		if (!File.Exists(path))
		{
			return false;
		}
		try
		{
			using (MemoryStream input = new MemoryStream(File.ReadAllBytes(path)))
			{
				using BinaryReader binaryReader = new BinaryReader(input);
				int key = binaryReader.ReadInt32();
				if (!m_loaders.TryGetValue(key, out var value))
				{
					return false;
				}
				value.Load(binaryReader, this);
			}
			if (carId != 0 && (int)m_carId != carId)
			{
				return false;
			}
			if (profileId != 0 && (int)m_profileId != profileId)
			{
				return false;
			}
			if (trackId != 0 && (int)m_trackId != trackId)
			{
				return false;
			}
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "Ghost", "Error reading ghost: " + ex);
			return false;
		}
		return true;
	}

	public void Save(int trackId, SubroomMode mode, string pointName)
	{
		string fileName = GetFileName(trackId, pointName, (byte)mode);
		string text = Application.persistentDataPath + "/Ghosts/";
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		SaveInternal(trackId, text + fileName + ".gst");
	}

	public void Save(int trackId, RaceMode raceMode, string configName)
	{
		string fileName = GetFileName(trackId, configName);
		string text = string.Concat(Application.persistentDataPath, "/Ghosts/" + raceMode.ToString() + "/");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		SaveInternal(trackId, text + fileName + ".gst");
	}

	public void Save(int trackId, RaceMode raceMode)
	{
		string ghostFilePath = GetGhostFilePath(carId, profileId, trackId, raceMode);
		SaveInternal(trackId, ghostFilePath);
	}

	private void SaveInternal(int trackId, string path)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(9);
		binaryWriter.Write(carId);
		binaryWriter.Write(profileId);
		binaryWriter.Write(swapId);
		binaryWriter.Write(trackId);
		binaryWriter.Write(greenLightTime);
		if (visual != null)
		{
			byte[] array = CustomTypes.SerializeCarVS(visual.Copy().MutateToNetworkParts());
			binaryWriter.Write(array.Length);
			binaryWriter.Write(array);
		}
		if (profile != null)
		{
			byte[] array2 = CustomTypes.SerializeCarProfile(profile);
			binaryWriter.Write(array2.Length);
			binaryWriter.Write(array2);
		}
		SerializeInternal(binaryWriter);
		using FileStream stream = File.Open(path, FileMode.Create);
		memoryStream.WriteTo(stream);
	}

	public GhostData Deserialize(byte[] data)
	{
		if (data == null || data.Length == 0)
		{
			return null;
		}
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader binaryReader = new BinaryReader(input);
		int key = binaryReader.ReadInt32();
		if (!m_loaders.TryGetValue(key, out var value))
		{
			return null;
		}
		value.Load(binaryReader, this);
		return this;
	}

	public byte[] Serialize(int trackId)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(9);
		binaryWriter.Write(carId);
		binaryWriter.Write(profileId);
		binaryWriter.Write(swapId);
		binaryWriter.Write(trackId);
		binaryWriter.Write(greenLightTime);
		if (visual != null)
		{
			byte[] array = CustomTypes.SerializeCarVS(visual.Copy().MutateToNetworkParts());
			binaryWriter.Write(array.Length);
			binaryWriter.Write(array);
		}
		if (profile != null)
		{
			byte[] array2 = CustomTypes.SerializeCarProfile(profile);
			binaryWriter.Write(array2.Length);
			binaryWriter.Write(array2);
		}
		SerializeInternal(binaryWriter);
		return memoryStream.ToArray();
	}

	public void Save(MemoryStream ms)
	{
		using BinaryWriter writer = new BinaryWriter(ms);
		SerializeInternal(writer);
	}
}
