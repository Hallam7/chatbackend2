using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebSockets;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Text;

namespace chatbackend2
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class Middleware
    {
        private readonly RequestDelegate _next;
        private static readonly ConcurrentBag<WebSocket> WebSockets = new ConcurrentBag<WebSocket>();

        public Middleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {

            if (httpContext.WebSockets.IsWebSocketRequest)
            {
                var socket = await httpContext.WebSockets.AcceptWebSocketAsync();

                WebSockets.Add(socket);

                while (socket.State == WebSocketState.Open)
                {
                    var token = CancellationToken.None;
                    var buffer = new ArraySegment<byte>(new byte[4096]);
                    var received = await socket.ReceiveAsync(buffer, token);
                    switch (received.MessageType)
                    {
                        case WebSocketMessageType.Close:
                            break;

                        case WebSocketMessageType.Text:
                            var incoming = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
                            incoming = incoming.Replace("\0", "");
                            var data = Encoding.UTF8.GetBytes("data from server: " + DateTime.Now.ToLocalTime() +
                                " " + incoming);

                            //send to all open sockets
                            await Task.WhenAll(WebSockets.Where(s => s.State == WebSocketState.Open)
                            .Select(s => s.SendAsync(buffer, WebSocketMessageType.Text, true, token)));
                            break;
                    }
                }
            }
            else
            {
                await _next.Invoke(httpContext);
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<Middleware>();
        }
    }
}
