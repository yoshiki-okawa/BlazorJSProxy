using System;

namespace BlazorJSProxy
{
	/// <summary>
	/// Specifies how the JavaScript object is constructed or referenced.
	/// </summary>
	[AttributeUsage(AttributeTargets.Interface)]
	public class JSTargetAttribute : Attribute
	{
		/// <summary>
		/// The constructure function name. e.g. Date
		/// When GlobalVariable is specified this is ignored.
		/// </summary>
		public string ConstructorFunction { get; set; }

		/// <summary>
		/// The global variable. e.g. window
		/// </summary>
		public string GlobalVariable { get; set; }
	}
}