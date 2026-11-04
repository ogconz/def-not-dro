using System.Collections.Generic;
using Customization;
using UnityEngine;

[CreateAssetMenu]
public class CarPartsAsset : ScriptableObject
{
	public List<CarPart> partsDB = new List<CarPart>();

	public int slotIdCounter;

	public StringIntDictionary slotMap = SerializableDictionary<string, int>.New<StringIntDictionary>();
}
