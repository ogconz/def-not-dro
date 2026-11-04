namespace Utils.Filter
{
	public readonly struct CharMap
	{
		public readonly int originalIndex;

		public readonly int cleanIndex;

		public CharMap(int originalIndex, int cleanIndex)
		{
			this.originalIndex = originalIndex;
			this.cleanIndex = cleanIndex;
		}
	}
}
