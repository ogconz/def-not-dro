public interface IKissTheWallChecker
{
	bool CheckCondition();

	void InitChecker(DriftController driftController, KissTheWallCarConfig config);
}
