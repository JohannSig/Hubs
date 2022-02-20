using System;
using System.Threading.Tasks;

namespace FrozenForge.Hubs
{
    public interface IHubClient : IAsyncDisposable
	{
		Task ConnectAsync();
		
		IDisposable On<TResult>(Func<TResult, Task> handler);
	}
}
