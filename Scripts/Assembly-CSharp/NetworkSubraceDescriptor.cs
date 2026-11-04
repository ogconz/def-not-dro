using System;
using UnityEngine;

public class NetworkSubraceDescriptor : MonoBehaviour
{
	[Serializable]
	public class Descriptor
	{
		public float raceTime = DefaultRaceDuration;
	}

	public static readonly float DefaultRaceDuration = 60f;

	public Descriptor descriptor = new Descriptor();

	public static Descriptor GetDefault()
	{
		return new Descriptor();
	}
}
