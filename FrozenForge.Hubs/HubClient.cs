using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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
						config.Cookies.Add(cookie);
					})
				.Build();

			this.Logger.LogDebug($"HubClient {Id}: Constructed");

			Task.Run(ConnectAsync);
		}

		public Guid Id { get; }

		public ILogger Logger { get; }

		public HubConnection HubConnection { get; private set; }

		public string ConnectionId => HubConnection.ConnectionId;

		public IDisposable On<TResult>(Func<TResult, Task> handler)
			=> HubConnection.On(handler.Method.Name, handler);

		public async Task ConnectAsync()
		{
			await HubConnection.StartAsync();

			this.Logger.LogDebug($"HubClient {Id}: {HubConnection.State}");
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

	public abstract class HubClient<THubMethods> : HubClient
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

		public Task<TResult> InvokeAsync<TResult>(Func<THubMethods, Func<Task<TResult>>> methodFunc)
		{
			return this.HubConnection.InvokeAsync<TResult>(GetMethodName(methodFunc));
		}

		public async Task<TResult> InvokeAsync<TParameters, TResult>(Func<THubMethods, Func<TParameters, Task<TResult>>> methodFunc, TParameters parameters)
		{
			var methodName = GetMethodName(methodFunc);

			return await this.HubConnection.InvokeAsync<TResult>(methodName, parameters);
		}

		private static string GetMethodName<TIn,TResult>(Func<TIn, TResult> methodFunc)
        {
			return Regex.Match(methodFunc.Method.Name, "<(.*?)>").Value.Trim('<', '>');
		}
	}
}
