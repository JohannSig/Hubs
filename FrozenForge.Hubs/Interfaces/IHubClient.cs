using System;
using System.Threading;
using System.Threading.Tasks;

namespace FrozenForge.Hubs
{
	public interface IHubClient : IAsyncDisposable
    {
		Task ConnectAsync();

		string ConnectionId { get; }

		IDisposable On<TResult>(Func<TResult, Task> handler);
	}

    public interface IHubClient<THubMethods> : IHubClient
	{
		Task<TResult> InvokeAsync<TResult>(Func<THubMethods, Func<Task<TResult>>> methodFunc);
	}
}
