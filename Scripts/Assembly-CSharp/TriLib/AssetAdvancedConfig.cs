using System;
using UnityEngine;

namespace TriLib
{
	[Serializable]
	public class AssetAdvancedConfig
	{
		public string Key;

		public int IntValue;

		public float FloatValue;

		public bool BoolValue;

		public string StringValue;

		public Vector3 TranslationValue;

		public Vector3 RotationValue;

		public Vector3 ScaleValue;

		public static AssetAdvancedConfig CreateConfig(AssetAdvancedPropertyClassNames className, int value)
		{
			return new AssetAdvancedConfig(AssetAdvancedPropertyMetadata.GetConfigKey(className), value);
		}

		public static AssetAdvancedConfig CreateConfig(AssetAdvancedPropertyClassNames className, float value)
		{
			return new AssetAdvancedConfig(AssetAdvancedPropertyMetadata.GetConfigKey(className), value);
		}

		public static AssetAdvancedConfig CreateConfig(AssetAdvancedPropertyClassNames className, bool value)
		{
			return new AssetAdvancedConfig(AssetAdvancedPropertyMetadata.GetConfigKey(className), value);
		}

		public static AssetAdvancedConfig CreateConfig(AssetAdvancedPropertyClassNames className, string value)
		{
			return new AssetAdvancedConfig(AssetAdvancedPropertyMetadata.GetConfigKey(className), value);
		}

		public static AssetAdvancedConfig CreateConfig(AssetAdvancedPropertyClassNames className, AiComponent value)
		{
			return new AssetAdvancedConfig(AssetAdvancedPropertyMetadata.GetConfigKey(className), value);
		}

		public static AssetAdvancedConfig CreateConfig(AssetAdvancedPropertyClassNames className, AiPrimitiveType value)
		{
			return new AssetAdvancedConfig(AssetAdvancedPropertyMetadata.GetConfigKey(className), value);
		}

		public static AssetAdvancedConfig CreateConfig(AssetAdvancedPropertyClassNames className, AiUVTransform value)
		{
			return new AssetAdvancedConfig(AssetAdvancedPropertyMetadata.GetConfigKey(className), value);
		}

		public static AssetAdvancedConfig CreateConfig(AssetAdvancedPropertyClassNames className, Vector3 translation, Vector3 rotation, Vector3 scale)
		{
			return new AssetAdvancedConfig(AssetAdvancedPropertyMetadata.GetConfigKey(className), translation, rotation, scale);
		}

		public AssetAdvancedConfig()
		{
		}

		public AssetAdvancedConfig(string key)
		{
			Key = key;
		}

		public AssetAdvancedConfig(string key, int defaultValue)
		{
			Key = key;
			IntValue = defaultValue;
		}

		public AssetAdvancedConfig(string key, float defaultValue)
		{
			Key = key;
			FloatValue = defaultValue;
		}

		public AssetAdvancedConfig(string key, bool defaultValue)
		{
			Key = key;
			BoolValue = defaultValue;
		}

		public AssetAdvancedConfig(string key, string defaultValue)
		{
			Key = key;
			StringValue = defaultValue;
		}

		public AssetAdvancedConfig(string key, AiComponent defaultValue)
		{
			Key = key;
			IntValue = (int)defaultValue >> 1;
		}

		public AssetAdvancedConfig(string key, AiPrimitiveType defaultValue)
		{
			Key = key;
			IntValue = (int)defaultValue >> 1;
		}

		public AssetAdvancedConfig(string key, AiUVTransform defaultValue)
		{
			Key = key;
			IntValue = (int)defaultValue >> 1;
		}

		public AssetAdvancedConfig(string key, Vector3 translation, Vector3 rotation, Vector3 scale)
		{
			Key = key;
			TranslationValue = translation;
			RotationValue = rotation;
			ScaleValue = scale;
		}
	}
}
