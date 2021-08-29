
using System;
using System.Threading.Tasks;

namespace BlazorJSProxy
{
	[JSTarget(ConstructorFunction = "Array")]
	public interface IArray<T> : IAsyncDisposable
	{
		ValueTask<int> Length { get; }

		ValueTask<T> GetValue(int index);

		ValueTask SetValue(int index, T value);
	}
}