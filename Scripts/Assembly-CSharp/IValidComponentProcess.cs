using UnityEngine;

public interface IValidComponentProcess
{
	bool isSuccess { get; }

	string processMessage { get; }

	void ValidProcess(Component comp);

	void Reset();
}
