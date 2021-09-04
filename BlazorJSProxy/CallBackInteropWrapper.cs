using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorJSProxy
{
	public class CallBackInteropWrapper
	{
		[JsonPropertyName("__isCallBackWrapper")]
		public bool IsCallBackWrapper { get; private set; } = true;

		public object CallbackRef { get; private set; }

		private CallBackInteropWrapper()
		{
		}

		public static CallBackInteropWrapper Create(IJSRuntime jsRuntime, Delegate callback)
		{
			var wrapper = new CallBackInteropWrapper
			{
				CallbackRef = DotNetObjectReference.Create(new JSInteropActionCallback(jsRuntime, callback))
			};
			return wrapper;
		}

		private class JSInteropActionCallback
		{
			private readonly Delegate toDo;
			private readonly Type[] argumentTypes;
			private readonly IJSRuntime jsRuntime;

			internal JSInteropActionCallback(IJSRuntime jsRuntime, Delegate toDo)
			{
				this.toDo = toDo;
				var method = toDo.Method.ToString();
				argumentTypes = toDo.GetType().GetGenericArguments();
				this.jsRuntime = jsRuntime;
			}
			[JSInvokable]
			public async Task Invoke(System.Text.Json.JsonElement[] args)
			{
				var newArgs = new List<object>();
				int count = argumentTypes.Length;
				if (argumentTypes.Length > 0 && argumentTypes[count-1] == typeof(Task))
					count = argumentTypes.Length - 1;
				for (int i = 0; i < count; i++)
				{
					Type type = argumentTypes[i];
					if (type.GetCustomAttribute<JSTargetAttribute>() == null)
					{
						var txt = args[i].GetRawText();
						var typeName = type.Name;
						newArgs.Add(System.Text.Json.JsonSerializer.Deserialize(txt, type));
					}
					else
					{
						var id = args[i].GetProperty("__jsObjectId").GetInt64();
						var c = new JSObjectReferenceWrapper(id);
						var typeName = type.Name;
						var methodInfo = CreateAsyncMethodInfoCache.GetCreateAsyncMethodInfo(type);
						var proxy = await (dynamic)methodInfo.Invoke(null, new object[] { jsRuntime, new ValueTask<JSObjectReferenceWrapper>(c), null });
						newArgs.Add(proxy);
					}
				}
				toDo.DynamicInvoke(newArgs.ToArray());
			}
		}
	}
}