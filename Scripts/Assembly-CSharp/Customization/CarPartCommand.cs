using System;
using System.Collections.Generic;
using SimpleXML;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Customization
{
	[Serializable]
	public class CarPartCommand
	{
		public enum CarPartCommandTarget
		{
			Unknown = 0,
			baseY = 1,
			frontBaseWidth = 2,
			frontBaseZ = 3,
			rearBaseWidth = 4,
			rearBaseZ = 5,
			frontWheel = 6,
			rearWheel = 7,
			texture = 8,
			lockChangeWheels = 9
		}

		public enum CarPartCommandType
		{
			Set = 0,
			Add = 1,
			Mul = 2
		}

		public float valueF;

		public string valueS;

		public CarPartCommandType type;

		public CarPartCommandTarget target;

		public CarPartCommand(CarPartCommandType type, CarPartCommandTarget target, string value)
		{
			this.type = type;
			this.target = target;
			valueS = value;
			GeneralExtensions.TryParseInvariant(value, out valueF);
		}

		public void Apply(RaceCar car, CarVisualManager.VisualSettings settings, BaseCar.Desc desc)
		{
			switch (target)
			{
			case CarPartCommandTarget.baseY:
				Calculate(ref desc.baseY);
				break;
			case CarPartCommandTarget.frontBaseWidth:
				Calculate(ref desc.frontBaseWidth);
				break;
			case CarPartCommandTarget.frontBaseZ:
				Calculate(ref desc.frontBaseZ);
				break;
			case CarPartCommandTarget.rearBaseWidth:
				Calculate(ref desc.rearBaseWidth);
				break;
			case CarPartCommandTarget.rearBaseZ:
				Calculate(ref desc.rearBaseZ);
				break;
			case CarPartCommandTarget.frontWheel:
				Calculate(ref settings.kitFrontWheels);
				break;
			case CarPartCommandTarget.rearWheel:
				Calculate(ref settings.kitRearWheels);
				break;
			case CarPartCommandTarget.lockChangeWheels:
				Calculate(ref settings.lockChangeWheels);
				break;
			case CarPartCommandTarget.texture:
			{
				AsyncOperationHandle<Texture> handle = ResourcesWrapper.LoadAsync(valueS.ToLower(), delegate(Texture texture)
				{
					if (car != null && car.carModel != null)
					{
						car.carModel.OverrideVinylWrapTexture(texture, null);
					}
				});
				car.carModel.onDispose += delegate
				{
					ResourcesWrapper.UnloadAsset(handle);
				};
				break;
			}
			}
		}

		public void ApplyOnlyKitWheel(CarVisualManager.VisualSettings settings)
		{
			switch (target)
			{
			case CarPartCommandTarget.frontWheel:
				Calculate(ref settings.kitFrontWheels);
				break;
			case CarPartCommandTarget.rearWheel:
				Calculate(ref settings.kitRearWheels);
				break;
			}
		}

		private void Calculate(ref bool target)
		{
			target = valueS == "1";
		}

		private void Calculate(ref float target)
		{
			switch (type)
			{
			case CarPartCommandType.Add:
				target += valueF;
				break;
			case CarPartCommandType.Mul:
				target *= valueF;
				break;
			case CarPartCommandType.Set:
				target = valueF;
				break;
			}
		}

		private void Calculate(ref int target)
		{
			switch (type)
			{
			case CarPartCommandType.Add:
				target += Mathf.RoundToInt(valueF);
				break;
			case CarPartCommandType.Mul:
				target *= Mathf.RoundToInt(valueF);
				break;
			case CarPartCommandType.Set:
				target = Mathf.RoundToInt(valueF);
				break;
			}
		}

		public static List<CarPartCommand> Parse(XMLDoc node)
		{
			List<CarPartCommand> list = null;
			foreach (XMLDoc child in node.childs)
			{
				CarPartCommandType carPartCommandType;
				switch (child.nodeName)
				{
				case "set":
					carPartCommandType = CarPartCommandType.Set;
					break;
				case "add":
					carPartCommandType = CarPartCommandType.Add;
					break;
				case "mul":
					carPartCommandType = CarPartCommandType.Mul;
					break;
				default:
					continue;
				}
				foreach (KeyValuePair<string, string> attribute in child.attributes)
				{
					CarPartCommandTarget carPartCommandTarget = ParseAtr(attribute.Key);
					if (carPartCommandTarget != CarPartCommandTarget.Unknown)
					{
						if (list == null)
						{
							list = new List<CarPartCommand>();
						}
						list.Add(new CarPartCommand(carPartCommandType, carPartCommandTarget, attribute.Value));
					}
				}
			}
			return list;
		}

		private static CarPartCommandTarget ParseAtr(string src)
		{
			return src.ToLower() switch
			{
				"basey" => CarPartCommandTarget.baseY, 
				"frontbasewidth" => CarPartCommandTarget.frontBaseWidth, 
				"frontbasez" => CarPartCommandTarget.frontBaseZ, 
				"rearbasewidth" => CarPartCommandTarget.rearBaseWidth, 
				"rearbasez" => CarPartCommandTarget.rearBaseZ, 
				"frontwheel" => CarPartCommandTarget.frontWheel, 
				"rearwheel" => CarPartCommandTarget.rearWheel, 
				"texture" => CarPartCommandTarget.texture, 
				"lockchangewheels" => CarPartCommandTarget.lockChangeWheels, 
				_ => CarPartCommandTarget.Unknown, 
			};
		}
	}
}
