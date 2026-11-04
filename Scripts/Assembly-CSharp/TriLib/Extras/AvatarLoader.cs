using System;
using System.Collections.Generic;
using UnityEngine;

namespace TriLib.Extras
{
	public class AvatarLoader : MonoBehaviour
	{
		public GameObject CurrentAvatar;

		public RuntimeAnimatorController RuntimeAnimatorController;

		public float ArmStretch = 0.05f;

		public float FeetSpacing;

		public bool HasTranslationDof;

		public float LegStretch = 0.5f;

		public float LowerArmTwist = 0.5f;

		public float LowerLegTwist = 0.5f;

		public float UpperArmTwist = 0.5f;

		public float UpperLegTwist = 0.5f;

		public float Scale = 0.01f;

		public float HeightOffset = 0.01f;

		public BoneRelationshipList CustomBoneNames;

		private static readonly BoneRelationshipList BipedBoneNames = new BoneRelationshipList
		{
			{ "Head", "Head", false },
			{ "Neck", "Neck", true },
			{ "Chest", "Spine3", true },
			{ "UpperChest", "Spine1", true },
			{ "Spine", "Spine", false },
			{ "Hips", "Bip01", false },
			{ "LeftShoulder", "L Clavicle", true },
			{ "LeftUpperArm", "L UpperArm", false },
			{ "LeftLowerArm", "L Forearm", false },
			{ "LeftHand", "L Hand", false },
			{ "RightShoulder", "R Clavicle", true },
			{ "RightUpperArm", "R UpperArm", false },
			{ "RightLowerArm", "R Forearm", false },
			{ "RightHand", "R Hand", false },
			{ "LeftUpperLeg", "L Thigh", false },
			{ "LeftLowerLeg", "L Calf", false },
			{ "LeftFoot", "L Foot", false },
			{ "LeftToes", "L Toe0", true },
			{ "RightUpperLeg", "R Thigh", false },
			{ "RightLowerLeg", "R Calf", false },
			{ "RightFoot", "R Foot", false },
			{ "RightToes", "R Toe0", true },
			{ "Left Thumb Proximal", "L Finger0", true },
			{ "Left Thumb Intermediate", "L Finger01", true },
			{ "Left Thumb Distal", "L Finger02", true },
			{ "Left Index Proximal", "L Finger1", true },
			{ "Left Index Intermediate", "L Finger11", true },
			{ "Left Index Distal", "L Finger12", true },
			{ "Left Middle Proximal", "L Finger2", true },
			{ "Left Middle Intermediate", "L Finger21", true },
			{ "Left Middle Distal", "L Finger22", true },
			{ "Left Ring Proximal", "L Finger3", true },
			{ "Left Ring Intermediate", "L Finger31", true },
			{ "Left Ring Distal", "L Finger32", true },
			{ "Left Little Proximal", "L Finger4", true },
			{ "Left Little Intermediate", "L Finger41", true },
			{ "Left Little Distal", "L Finger42", true },
			{ "Right Thumb Proximal", "R Finger0", true },
			{ "Right Thumb Intermediate", "R Finger01", true },
			{ "Right Thumb Distal", "R Finger02", true },
			{ "Right Index Proximal", "R Finger1", true },
			{ "Right Index Intermediate", "R Finger11", true },
			{ "Right Index Distal", "R Finger12", true },
			{ "Right Middle Proximal", "R Finger2", true },
			{ "Right Middle Intermediate", "R Finger21", true },
			{ "Right Middle Distal", "R Finger22", true },
			{ "Right Ring Proximal", "R Finger3", true },
			{ "Right Ring Intermediate", "R Finger31", true },
			{ "Right Ring Distal", "R Finger32", true },
			{ "Right Little Proximal", "R Finger4", true },
			{ "Right Little Intermediate", "R Finger41", true },
			{ "Right Little Distal", "R Finger42", true }
		};

		private static readonly BoneRelationshipList MixamoBoneNames = new BoneRelationshipList
		{
			{ "Head", "Head", false },
			{ "Neck", "Neck", true },
			{ "Chest", "Spine1", true },
			{ "UpperChest", "Spine2", true },
			{ "Spine", "Spine", false },
			{ "Hips", "Hips", false },
			{ "LeftShoulder", "LeftShoulder", true },
			{ "LeftUpperArm", "LeftArm", false },
			{ "LeftLowerArm", "LeftForeArm", false },
			{ "LeftHand", "LeftHand", false },
			{ "RightShoulder", "RightShoulder", true },
			{ "RightUpperArm", "RightArm", false },
			{ "RightLowerArm", "RightForeArm", false },
			{ "RightHand", "RightHand", false },
			{ "LeftUpperLeg", "LeftUpLeg", false },
			{ "LeftLowerLeg", "LeftLeg", false },
			{ "LeftFoot", "LeftFoot", false },
			{ "LeftToes", "LeftToeBase", true },
			{ "RightUpperLeg", "RightUpLeg", false },
			{ "RightLowerLeg", "RightLeg", false },
			{ "RightFoot", "RightFoot", false },
			{ "RightToes", "RightToeBase", true },
			{ "Left Thumb Proximal", "LeftHandThumb1", true },
			{ "Left Thumb Intermediate", "LeftHandThumb2", true },
			{ "Left Thumb Distal", "LeftHandThumb3", true },
			{ "Left Index Proximal", "LeftHandIndex1", true },
			{ "Left Index Intermediate", "LeftHandIndex2", true },
			{ "Left Index Distal", "LeftHandIndex3", true },
			{ "Left Middle Proximal", "LeftHandMiddle1", true },
			{ "Left Middle Intermediate", "LeftHandMiddle2", true },
			{ "Left Middle Distal", "LeftHandMiddle3", true },
			{ "Left Ring Proximal", "LeftHandRing1", true },
			{ "Left Ring Intermediate", "LeftHandRing2", true },
			{ "Left Ring Distal", "LeftHandRing3", true },
			{ "Left Little Proximal", "LeftHandPinky1", true },
			{ "Left Little Intermediate", "LeftHandPinky2", true },
			{ "Left Little Distal", "LeftHandPinky3", true },
			{ "Right Thumb Proximal", "RightHandThumb1", true },
			{ "Right Thumb Intermediate", "RightHandThumb2", true },
			{ "Right Thumb Distal", "RightHandThumb3", true },
			{ "Right Index Proximal", "RightHandIndex1", true },
			{ "Right Index Intermediate", "RightHandIndex2", true },
			{ "Right Index Distal", "RightHandIndex3", true },
			{ "Right Middle Proximal", "RightHandMiddle1", true },
			{ "Right Middle Intermediate", "RightHandMiddle2", true },
			{ "Right Middle Distal", "RightHandMiddle3", true },
			{ "Right Ring Proximal", "RightHandRing1", true },
			{ "Right Ring Intermediate", "RightHandRing2", true },
			{ "Right Ring Distal", "RightHandRing3", true },
			{ "Right Little Proximal", "RightHandPinky1", true },
			{ "Right Little Intermediate", "RightHandPinky2", true },
			{ "Right Little Distal", "RightHandPinky3", true }
		};

		private AssetLoaderOptions _loaderOptions;

		protected void Start()
		{
			_loaderOptions = AssetLoaderOptions.CreateInstance();
			_loaderOptions.UseLegacyAnimations = false;
			_loaderOptions.DontGenerateAvatar = true;
			_loaderOptions.AnimatorController = RuntimeAnimatorController;
		}

		public bool LoadAvatar(string filename, GameObject templateAvatar)
		{
			throw new Exception("Unity no longer supports runtime avatar creation.");
		}

		private bool BuildAvatar()
		{
			throw new Exception("Unity no longer supports runtime avatar creation.");
		}

		private Dictionary<string, Transform> FindOutBoneTransforms(GameObject loadedObject)
		{
			Dictionary<string, Transform> dictionary = new Dictionary<string, Transform>();
			List<BoneRelationshipList> list = new List<BoneRelationshipList>();
			list.Add(BipedBoneNames);
			list.Add(MixamoBoneNames);
			if (CustomBoneNames != null)
			{
				list.Add(CustomBoneNames);
			}
			bool flag = false;
			foreach (BoneRelationshipList item in list)
			{
				if (flag)
				{
					break;
				}
				flag = true;
				foreach (BoneRelationship item2 in item)
				{
					Transform transform = loadedObject.transform.FindDeepChild(item2.BoneName, endsWith: true);
					if (transform == null)
					{
						if (!item2.Optional)
						{
							dictionary.Clear();
							flag = false;
							break;
						}
					}
					else
					{
						dictionary.Add(item2.HumanBone, transform);
					}
				}
			}
			return dictionary;
		}

		private void SetupCapsuleCollider()
		{
			CapsuleCollider component = CurrentAvatar.GetComponent<CapsuleCollider>();
			if (!(component == null))
			{
				Bounds bounds = CurrentAvatar.transform.EncapsulateBounds();
				float num = 1f / Scale;
				float num2 = bounds.extents.x * num;
				float num3 = bounds.extents.y * num;
				float num4 = bounds.extents.z * num;
				component.height = (float)Math.Round(num3 * 2f, 1);
				component.radius = (float)Math.Round(Mathf.Sqrt(num2 * num2 + num4 * num4) * 0.5f, 1);
				component.center = new Vector3(0f, (float)Math.Round(num3, 1) + HeightOffset, 0f);
			}
		}

		private static SkeletonBone CreateSkeletonBone(Transform boneTransform)
		{
			return new SkeletonBone
			{
				name = boneTransform.name,
				position = boneTransform.localPosition,
				rotation = boneTransform.localRotation,
				scale = boneTransform.localScale
			};
		}

		private static HumanBone CreateHumanBone(string humanName, string boneName)
		{
			return new HumanBone
			{
				boneName = boneName,
				humanName = humanName,
				limit = 
				{
					useDefaultValues = true
				}
			};
		}
	}
}
