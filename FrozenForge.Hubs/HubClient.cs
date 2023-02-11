using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq.Expressions;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace FrozenForge.Hubs
{
    public abstract class HubClient : IHubClient
    {
		protected HubClient(
			ILogger logger,
			Uri hubUri)
			: this(logger, hubUri, cookie: default)
		{
		}

		protected HubClient(
			ILogger logger,
			Uri hubUri,
			Action<HttpConnectionOptions> config)
		{
            this.Id = Guid.NewGuid();
            this.Logger = logger;
            this.HubConnection = new HubConnectionBuilder()
				.AddJsonProtocol(x => x.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve)
                .WithUrl(
                    hubUri,
                    config)
                .Build();

            this.Logger.LogDebug($"HubClient {Id}: Constructed");
        }

        protected HubClient(
			ILogger logger,
			Uri hubUri,
			HttpContext httpContext,
			string cookieName)
			: this(logger, hubUri, new Cookie
			{
				Name = cookieName,
				Value = httpContext.Request.Cookies.TryGetValue(cookieName, out var cookie) ? cookie : default,
				Domain = httpContext.Request.Host.Host
			})
		{
		}

		private HubClient(
			ILogger logger,
			Uri hubUri,
			Cookie cookie)
		{
			Id = Guid.NewGuid();
			this.Logger = logger;
			HubConnection = new HubConnectionBuilder()
				.AddJsonProtocol(config => config.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve)
				.WithUrl(
					hubUri,
					config =>
					{
						if (cookie is not null) { config.Cookies.Add(cookie); }
					})
				.Build();

			this.Logger.LogDebug($"HubClient {Id}: Constructed");

			//Task.Run(ConnectAsync);
		}

		public Guid Id { get; }

		public ILogger Logger { get; }

		public HubConnection HubConnection { get; private set; }

		public string ConnectionId => HubConnection.ConnectionId;

		public IDisposable On<TResult>(Func<TResult, Task> handler)
			=> HubConnection.On(handler.Method.Name, handler);

		public async Task ConnectAsync(CancellationToken cancellationToken = default)
		{
			if (HubConnection.State != HubConnectionState.Disconnected)
            {
				throw new InvalidOperationException("Connection state is " + HubConnection.State);
            }

			try
			{
				await HubConnection.StartAsync(cancellationToken);
				
				this.Logger.LogDebug($"HubClient {Id}: {HubConnection.State}");
			}
			catch (Exception exception)
            {
				this.Logger.LogError(exception, $"HubClient {Id}: Exception while attempting to starting HubConnection (State: {HubConnection.State})");

				throw;
            }
		}

		public async ValueTask DisposeAsync()
		{
			if (HubConnection != null)
			{
				await HubConnection.DisposeAsync();

				HubConnection = null;

				this.Logger.LogDebug($"HubClient {Id}: Disposed");
			}
		}

	}

	public abstract class HubClient<THubMethods> : HubClient, IHubClient<THubMethods>
	{
		protected HubClient(
			ILogger logger,
			Uri hubUri)
			: base(logger, hubUri)
		{
		}

		protected HubClient(
			ILogger logger,
			Uri hubUri,
			HttpContext httpContext,
			string cookieName)
			: base(logger, hubUri, httpContext, cookieName)
		{
		}

		public Task<TResult> InvokeAsync<TResult>(Expression<Func<THubMethods, Func<Task<TResult>>>> methodFunc)
		{
            return this.HubConnection.InvokeAsync<TResult>(GetMethodName(methodFunc));
		}

		public Task InvokeAsync<TParameters>(Expression<Func<THubMethods, Func<TParameters, Task>>> expression, TParameters parameters)
		{
            return this.HubConnection.InvokeAsync(GetMethodName(expression), parameters);
		}

		public async Task<TResult> InvokeAsync<TParameters, TResult>(Expression<Func<THubMethods, Func<TParameters, Task<TResult>>>> expression, TParameters parameters)
		{
			return await this.HubConnection.InvokeAsync<TResult>(GetMethodName(expression), parameters);
		}

		private static string GetMethodName<TTask>(Expression<Func<THubMethods, TTask>> expression)
        {
            return ((((expression.Body as UnaryExpression).Operand as MethodCallExpression).Object as ConstantExpression).Value as System.Reflection.MethodInfo).Name;
		}
	}
}
