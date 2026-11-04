using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public abstract class BaseSharingConsole
{
	protected Dictionary<string, Action> commandsDict = new Dictionary<string, Action>();

	protected virtual string loadFailureMessage => "Load error";

	protected virtual string saveFailureMessage => "Save error";

	protected abstract string GetFileExtension();

	protected abstract string GetDirectory();

	public static T Create<T>() where T : BaseSharingConsole
	{
		return Activator.CreateInstance<T>();
	}

	public void RegisterConsoleCommands()
	{
	}

	public void UnregisterConsoleCommands()
	{
	}

	protected T LoadFile<T>(string fileName) where T : class
	{
		RemoveFileExtIfNeed(ref fileName);
		string text = GetDirectory() + "/" + fileName + GetFileExtension();
		if (!File.Exists(text))
		{
			GameConsole.Print(loadFailureMessage + ". File '" + fileName + "' doesn't exist");
			return null;
		}
		try
		{
			byte[] array = File.ReadAllBytes(text);
			using MemoryStream stream = new MemoryStream(array);
			return Deserialize<T>(stream, array);
		}
		catch (Exception)
		{
		}
		GameConsole.Print(loadFailureMessage + " at path " + text);
		return null;
	}

	protected bool SaveFile<T>(out string saveActionStr, string fileName, T saveData) where T : class
	{
		saveActionStr = string.Empty;
		if (saveData == null)
		{
			Debug.LogError(saveFailureMessage + ", no save data");
			return false;
		}
		RemoveFileExtIfNeed(ref fileName);
		string directory = GetDirectory();
		string text = directory + "/" + fileName + GetFileExtension();
		if (!Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}
		saveActionStr = (File.Exists(text) ? "overwritten" : "saved");
		try
		{
			GetSaveMemoryStream(saveData, text);
			return true;
		}
		catch (Exception)
		{
			GameConsole.Print(saveFailureMessage + " at path " + text);
			return false;
		}
	}

	public MemoryStream GetSaveMemoryStream<T>(T saveData, string path = "") where T : class
	{
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
		{
			byte[] array = Serialize(memoryStream, saveData);
			binaryWriter.Write(array.Length);
			binaryWriter.Write(array);
			if (!string.IsNullOrEmpty(path))
			{
				using FileStream stream = File.Open(path, FileMode.Create);
				memoryStream.WriteTo(stream);
			}
		}
		memoryStream.Flush();
		memoryStream.Close();
		return memoryStream;
	}

	protected abstract byte[] Serialize<T>(Stream stream, T saveData) where T : class;

	protected abstract T Deserialize<T>(Stream stream, byte[] rawData) where T : class;

	protected void RemoveFileExtIfNeed(ref string fileName)
	{
		fileName = fileName.Replace(GetFileExtension(), string.Empty);
	}

	protected string GetFileNameForCmd(string cmdName)
	{
		if (GameConsole.cmdArgs.Length < 2)
		{
			GameConsole.Print("Invalid arguments, use: " + cmdName + " <fileName>");
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		string[] cmdArgs = GameConsole.cmdArgs;
		for (int i = 1; i < cmdArgs.Length; i++)
		{
			stringBuilder.Append(cmdArgs[i]);
			stringBuilder.Append(' ');
		}
		return stringBuilder.ToString().Trim();
	}

	protected T ObtainContext<T>() where T : BaseContext
	{
		BaseGameState baseGameState = States.mainCurrent as BaseGameState;
		if (baseGameState != null)
		{
			return baseGameState.contextOwner.Get<T>();
		}
		return null;
	}
}
