using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FrozenForge.Hubs
{
    public abstract class HubClient : IHubClient, IAsyncDisposable
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
}
