using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace FrozenForge.Hubs
{
	public interface IHubClient : IAsyncDisposable
    {
		Task ConnectAsync(CancellationToken cancellationToken = default);

		string ConnectionId { get; }

		IDisposable On<TResult>(Func<TResult, Task> handler);
	}

    public interface IHubClient<THubMethods> : IHubClient
	{
		Task InvokeAsync<TParameters>(Expression<Func<THubMethods, Func<TParameters, Task>>> expression, TParameters parameters);
		
		Task<TResult> InvokeAsync<TResult>(Expression<Func<THubMethods, Func<Task<TResult>>>> methodFunc);

		Task<TResult> InvokeAsync<TParameters, TResult>(Expression<Func<THubMethods, Func<TParameters, Task<TResult>>>> methodFunc, TParameters parameters);
	}
}
