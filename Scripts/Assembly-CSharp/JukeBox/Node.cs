using System;
using FMODUnity;

namespace JukeBox
{
	[Serializable]
	public class Node
	{
		public MusicSignal Signal;

		[EventRef]
		public string[] Samples;

		public bool Looped;

		[NonSerialized]
		public int LastPos;
	}
}
