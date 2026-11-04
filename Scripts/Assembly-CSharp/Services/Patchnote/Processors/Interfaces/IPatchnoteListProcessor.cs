using System.Collections.Generic;

namespace Services.Patchnote.Processors.Interfaces
{
	public interface IPatchnoteListProcessor<T>
	{
		void Process(List<T> keysList);
	}
}
