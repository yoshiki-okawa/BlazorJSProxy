using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorJSProxy
{
	internal static class BlazorJSProxyInitializer
	{
		private static bool init = false;
		private const string code = @"
window.BlazorJSProxy = window.BlazorJSProxy || {};
window.BlazorJSProxy.eval = (...arguments) => new Function(arguments[0]).apply(null, arguments[1]);
window.BlazorJSProxy.getProperty = (x, y) => x[y];
window.BlazorJSProxy.setProperty = (x, y, z) => x[y] = z;
window.BlazorJSProxy.cloneObjWithFilter = (obj, filter) =>
{
	if (Object(obj) !== obj)
	{
		return obj;
	}
	else if (Array.isArray(obj))
	{
		return obj.reduce((previousValue, currentValue) =>
		{
			if (filter(undefined, currentValue))
				return previousValue.push(window.BlazorJSProxy.cloneObjWithFilter(currentValue, filter));
			return previousValue;
		}, []);
	}

	let newObj = {};
	for (let k in obj)
	{
		let v = obj[k];
		if (filter(k, v))
			newObj[k] = window.BlazorJSProxy.cloneObjWithFilter(obj[k], filter);
	}
	return newObj;
};
window.BlazorJSProxy.setEventHandler = (x, y, z) => x[y] = (...arguments) =>
{
	z.invokeMethodAsync('InvokeAsync', window.BlazorJSProxy.cloneObjWithFilter(arguments[0], (k, v) => v !== window && typeof v !== 'function' && !(v instanceof Element) && !(v instanceof HTMLDocument)));
};
";
		internal static async Task InitializeAsync(IJSRuntime jsRuntime)
		{
			if (!init)
				await jsRuntime.InvokeVoidAsync("eval", code);
			init = true;
		}
	}
}