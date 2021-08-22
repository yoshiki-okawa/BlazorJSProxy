using ClassLibrary1;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Example.Pages
{
	public partial class Counter : ComponentBase
	{
		[Inject]
		private Class1 Class1 { get; set; }

		protected override async Task OnInitializedAsync()
		{
			await base.OnInitializedAsync();
			await Class1.Test();
		}
	}
}
