using System.Text.Json.Serialization;

namespace BlazorJSProxy
{
	public class JSObjectReferenceWrapper
	{
		[JsonPropertyName("__jsObjectId")] 
		public long Id { get; private set; }

		public JSObjectReferenceWrapper(long id)
		{
			Id = id;
		}
	}
}