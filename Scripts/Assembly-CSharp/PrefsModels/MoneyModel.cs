using System;
using CodeStage.AntiCheat.ObscuredTypes;
using DB;
using DB.Meta;
using DI;
using GameOverlay;
using SimpleJSON;

namespace PrefsModels
{
	public class MoneyModel : BasePrefsModel
	{
		public bool isDLCMoneyApplied;

		private ObscuredInt m_moneyCoins;

		private ObscuredInt m_ratingPoints;

		public int coins
		{
			get
			{
				return m_moneyCoins;
			}
			set
			{
				if (value >= 0 && (int)m_moneyCoins != value)
				{
					m_moneyCoins = value;
					this.OnMoneyChanged.Call(value, ECurrencyType.Coins);
					Save();
				}
			}
		}

		public int ratingPoints
		{
			get
			{
				return m_ratingPoints;
			}
			set
			{
				if ((int)m_ratingPoints != value)
				{
					m_ratingPoints = value;
					this.OnMoneyChanged.Call(value, ECurrencyType.RatingPoints);
					Save();
				}
			}
		}

		[JsonIgnore]
		public event Action<int, ECurrencyType> OnMoneyChanged;

		public MoneyModel()
		{
			m_moneyCoins.ApplyNewCryptoKey();
			GlobalGameSettings single = DependencyInjector.Resolve<BaseModel>().GetSingle<GlobalGameSettings>();
			m_moneyCoins = single.startCoins;
			m_ratingPoints = single.startRatingPoints;
		}

		public override void PostLoad()
		{
			base.PostLoad();
			ApplyDLCMoney();
		}

		protected override bool CompareRecovePrefs(BasePrefsModel reserveModel)
		{
			if (!(reserveModel is MoneyModel moneyModel))
			{
				return true;
			}
			if (coins == moneyModel.coins)
			{
				return (int)m_ratingPoints == (int)moneyModel.m_ratingPoints;
			}
			return false;
		}

		public void ApplyDLCMoney()
		{
			if (!isDLCMoneyApplied && Overlay.instance.IsDLCInstalled(DLCCode.Gold))
			{
				GlobalGameSettings single = DependencyInjector.Resolve<BaseModel>().GetSingle<GlobalGameSettings>();
				isDLCMoneyApplied = true;
				coins += single.goldMoney;
				Save();
			}
		}

		public int GetMoney(ECurrencyType type)
		{
			return m_moneyCoins;
		}

		public void SetMoney(ECurrencyType type, int money)
		{
			coins = money;
		}
	}
}
