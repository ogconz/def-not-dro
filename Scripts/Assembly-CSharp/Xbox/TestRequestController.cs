using System.Collections;
using UnityEngine;

namespace Xbox
{
	public class TestRequestController : SimpleRequestController
	{
		public TestRequestController()
			: base(100, 300)
		{
		}

		public IEnumerator SetPresence(bool isUserActiveInTitle)
		{
			yield return DoWait();
			IncRequest();
			yield return new WaitForSecondsRealtime(Random.value);
			Finish();
		}
	}
}
