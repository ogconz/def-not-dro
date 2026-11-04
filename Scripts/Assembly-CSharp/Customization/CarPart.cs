using System;
using System.Collections.Generic;
using DB;
using DB.Meta;
using GameOverlay;
using Services.UGC.Interfaces;
using UnityEngine;

namespace Customization
{
	[Serializable]
	public class CarPart : IEquatable<CarPart>
	{
		[Serializable]
		public class SubPart
		{
			public string prefab;

			public string type;
		}

		[Serializable]
		public class SlotPart
		{
			public int slotId;

			public int partId;

			public SlotPart(int slotId, int carPartId)
			{
				this.slotId = slotId;
				partId = carPartId;
			}
		}

		public int id;

		public string vendor;

		public string description;

		public int nativeid;

		public ulong packetid;

		public IUGCManager.ServiceProvider ugcServiceProvider;

		public bool vinylsNotSupport;

		public int price;

		public int overrideEmptyBonnetId = -1;

		public string prefab;

		public bool external;

		public DLCCode DLCCode;

		public List<SubPart> submodels = new List<SubPart>();

		public string type;

		public string group;

		public int carId;

		public List<SlotPart> enable = new List<SlotPart>();

		public List<int> disable = new List<int>();

		public List<int> force = new List<int>();

		public List<int> include = new List<int>();

		public List<int> exclude = new List<int>();

		public List<int> take = new List<int>();

		public List<CarPartCommand> cmds;

		public List<CarPartColorOverride> colorOverrides;

		public override string ToString()
		{
			return $"Part: {nativeid}({id}) type:'{type}' vendor:'{vendor}' prefab:'{prefab}' desc:'{description}'";
		}

		public void ApplyPrice(BaseModel baseModel)
		{
			if (type == "wheel")
			{
				price = -1;
				return;
			}
			PlayerCar byId = baseModel.GetById<PlayerCar>(carId);
			if (byId == null)
			{
				price = -1;
			}
			else
			{
				price = Mathf.RoundToInt((float)(price * byId.cost) / 100f);
			}
		}

		public bool Equals(CarPart other)
		{
			return id == other.id;
		}

		public override bool Equals(object ob)
		{
			if (ob is CarPart other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return id.GetHashCode();
		}
	}
}
