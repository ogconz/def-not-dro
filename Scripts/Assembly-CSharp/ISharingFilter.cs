public interface ISharingFilter
{
	int carId { get; set; }

	ISharingFilter GetDefault();

	void SetFilterValue<K, T>(K filterType, T value);

	T GetFilterValue<K, T>(K filterType);
}
