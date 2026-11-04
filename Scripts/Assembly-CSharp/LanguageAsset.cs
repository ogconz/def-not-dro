using UnityEngine;

[CreateAssetMenu]
public class LanguageAsset : ScriptableObject
{
	public StringStringDictionary data = SerializableDictionary<string, string>.New<StringStringDictionary>();
}
