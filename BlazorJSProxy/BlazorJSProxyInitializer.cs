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
DotNet.attachReviver((key, value) => {
    if (value &&
        typeof value === 'object' &&
        value.hasOwnProperty('__isCallBackWrapper')) {

        var netObjectRef = value.callbackRef;
        return (...arguments) => {
            netObjectRef.invokeMethodAsync('Invoke', arguments.reduce((previousValue, currentValue) =>
		{
			if (typeof currentValue === 'object')
				previousValue.push(DotNet.createJSObjectReference(currentValue));
			else
				previousValue.push(currentValue);
			return previousValue;
		}, []));
        };
    } else {
        return value;
    }
});
";
		internal static async Task InitializeAsync(IJSRuntime jsRuntime)
		{
			if (!init)
				await jsRuntime.InvokeVoidAsync("eval", code);
			init = true;
		}
	}
}