using System;
using System.Threading.Tasks;

namespace FrozenForge.Hubs
{
    public interface IHubClient<THubClientMethods> : IAsyncDisposable
	{
		Task ConnectAsync();
		
		IDisposable On<TResult>(Func<TResult, Task> handler);
	}
}
