using System.Collections.Generic;
using DB.Meta;

public class DynamicDictionariesHolder : BaseXMLDictionaryHolder
{
	public List<PlayerCarLock> playerCarLocks = new List<PlayerCarLock>();

	public List<PlayerCarProfileLock> playerCarProfileLocks = new List<PlayerCarProfileLock>();

	public List<RaceResult> timeTrialResults = new List<RaceResult>();

	public List<TrackLock> trackLocks = new List<TrackLock>();

	public DynamicDictionariesHolder()
	{
	}

	public DynamicDictionariesHolder(List<PlayerCarLock> _playerCarLocks, List<PlayerCarProfileLock> _playerCarProfileLocks, List<RaceResult> _timeTrialResults, List<TrackLock> _trackLocks)
	{
		playerCarLocks = _playerCarLocks;
		playerCarProfileLocks = _playerCarProfileLocks;
		timeTrialResults = _timeTrialResults;
		trackLocks = _trackLocks;
		Initialize();
	}

	protected override void OnInitialize()
	{
		allLists.Clear();
		allLists.Add(new ListToDictionary<PlayerCarLock>(playerCarLocks));
		allLists.Add(new ListToDictionary<PlayerCarProfileLock>(playerCarProfileLocks));
		allLists.Add(new ListToDictionary<RaceResult>(timeTrialResults));
		allLists.Add(new ListToDictionary<TrackLock>(trackLocks));
	}

	protected override void OnSave()
	{
		GameLog.Print(AlertLevel.Notify, "Debug", "DynamicDictionariesHolder.SaveDictionary fileName = " + fileName);
		SaveXMLData(this, fileName);
	}
}
