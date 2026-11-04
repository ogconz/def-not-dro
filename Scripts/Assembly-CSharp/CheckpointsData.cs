using UnityEngine;

public class CheckpointsData : MonoBehaviour
{
	public int laps = 1;

	public Checkpoint[] GetPoints()
	{
		return GetComponentsInChildren<Checkpoint>();
	}

	public StartLine GetStartLine()
	{
		return GetComponentInChildren<StartLine>();
	}
}
