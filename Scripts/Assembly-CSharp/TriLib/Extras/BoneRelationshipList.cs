using System;
using System.Collections;
using System.Collections.Generic;

namespace TriLib.Extras
{
	[Serializable]
	public class BoneRelationshipList : IEnumerable<BoneRelationship>, IEnumerable
	{
		private readonly List<BoneRelationship> _relationships;

		public BoneRelationshipList()
		{
			_relationships = new List<BoneRelationship>();
		}

		public void Add(string humanBone, string boneName, bool optional)
		{
			_relationships.Add(new BoneRelationship(humanBone, boneName, optional));
		}

		public IEnumerator<BoneRelationship> GetEnumerator()
		{
			return _relationships.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
