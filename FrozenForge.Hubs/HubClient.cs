using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FrozenForge.Hubs
{

    public abstract class HubClient<THubClientMethods> : IHubClient<THubClientMethods>
	{
		protected HubClient(Uri hubUri)
			: this(hubUri, cookie: default)
        {
        }

		protected HubClient(
			Uri hubUri, 
			HttpContext httpContext, 
			string cookieName)
			: this(hubUri, new Cookie
			{
				Name = cookieName,
				Value = httpContext.Request.Cookies.TryGetValue(cookieName, out var cookie) ? cookie : default,
				Domain = httpContext.Request.Host.Host
			})
        {
        }

		private HubClient(Uri hubUri, Cookie cookie)
		{
			HubConnection = new HubConnectionBuilder()
				.AddJsonProtocol(config => config.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve)
				.WithUrl(hubUri,
                config =>
                {
                    config.Cookies.Add(cookie);
                })
				.Build();
		}

		public HubConnection HubConnection { get; }

		public IDisposable On<TResult>(Func<TResult, Task> handler)
			=> HubConnection.On(handler.Method.Name, handler);

        public Task ConnectAsync() => HubConnection.StartAsync();

        public ValueTask DisposeAsync()
			=> HubConnection.DisposeAsync();
	}
}
