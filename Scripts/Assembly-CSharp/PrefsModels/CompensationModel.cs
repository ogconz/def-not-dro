using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using SimpleJSON;

namespace PrefsModels
{
	public class CompensationModel : BasePrefsModel
	{
		public enum CompensationType
		{
			None = 0,
			Car = 1,
			Part = 2,
			VinylCategory = 3,
			Vinyl = 4
		}

		private class Compensation
		{
			public CompensationType type;

			public string title;

			public string description;

			public int id;

			public int moneyReward;
		}

		private const int QuarantaCarId = 129;

		private const int QuarantaMoneyCompensation = 10000000;

		private const int CompensationDelay = 2000;

		[JsonIgnore]
		private Queue<Compensation> m_compsQueue = new Queue<Compensation>();

		[JsonSerialize]
		private Dictionary<int, bool> m_receivedCompensations = new Dictionary<int, bool>();

		public override void PostLoad()
		{
			base.PostLoad();
			CompensateQuaranta();
		}

		public override void Save()
		{
			isSaveAllowed = true;
			base.Save();
		}

		public void ClearAll()
		{
			m_receivedCompensations.Clear();
			Save();
		}

		public bool IsNeedCompensateQuaranta(int id)
		{
			return id == 129;
		}

		public async UniTaskVoid ObtainCompensation(bool showUIMessage, GamePrefs prefs, BaseModel model)
		{
			await UniTask.Delay(2000);
			while (m_compsQueue.Count > 0)
			{
				Compensation c = m_compsQueue.Dequeue();
				bool received = false;
				Compensate(c, prefs, model, delegate(Action callback)
				{
					if (showUIMessage)
					{
						UIMessageBox.ShowWarning(c.title, delegate
						{
							callback.SafeCall();
							received = true;
						}, c.description);
					}
					else
					{
						callback.SafeCall();
						received = true;
					}
				});
				await UniTask.WaitUntil(() => received);
			}
		}

		private void CompensateQuaranta()
		{
			if (!(m_receivedCompensations.TryGetValue(129, out var value) && value))
			{
				m_compsQueue.Enqueue(new Compensation
				{
					type = CompensationType.Car,
					title = "Car Compensation",
					description = "Here is your compensation",
					moneyReward = 10000000,
					id = 129
				});
			}
		}

		private void Compensate(Compensation compensation, GamePrefs prefs, BaseModel model, Action<Action> action)
		{
			if (compensation.type != CompensationType.Car)
			{
				return;
			}
			PlayerCar meta = model.GetById<PlayerCar>(compensation.id);
			if (meta != null && prefs.locks.availableCars.Contains(compensation.id))
			{
				action.SafeCall(delegate
				{
					meta.enabled = false;
					int id = compensation.id;
					m_receivedCompensations[id] = true;
					prefs.locks.availableCars.Remove(id);
					prefs.favoriteCars.RemoveCar(id);
					prefs.money.coins += compensation.moneyReward;
					Save();
				});
			}
		}
	}
}
