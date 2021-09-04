
using System;
using System.Threading.Tasks;

namespace BlazorJSProxy
{
	[JSTarget(ConstructorFunction = "Array")]
	public interface IArray<T> : IAsyncDisposable
	{
		ValueTask<int> Length { get; }

		ValueTask<T> this[int index] { get; set; }
	}
}