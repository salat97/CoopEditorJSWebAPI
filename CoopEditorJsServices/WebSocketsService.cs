﻿using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoopEditorJsServices.Interfaces;

namespace CoopEditorJsServices
{
	public class WebSocketsService : IWebSocketsService
    {
        private readonly IMessageService _messageService;

        public WebSocketsService(IMessageService messageService)
        {
            _messageService = messageService;
        }

		public async Task<string> ExtractMessage(WebSocket socket, CancellationToken cancellationToken = default(CancellationToken))
		{
			var buffer = new ArraySegment<byte>(new byte[8192]);
			using (var memoryStream = new MemoryStream())
			{
				WebSocketReceiveResult result;
				do
				{
					cancellationToken.ThrowIfCancellationRequested();

					result = await socket.ReceiveAsync(buffer, cancellationToken);
					memoryStream.Write(buffer.Array, buffer.Offset, result.Count);
				}
				while (!result.EndOfMessage);

				memoryStream.Seek(0, SeekOrigin.Begin);
				if (result.MessageType != WebSocketMessageType.Text)
				{
					return null;
				}

				using (var reader = new StreamReader(memoryStream, Encoding.UTF8))
				{
					return await reader.ReadToEndAsync();
				}
			}
		}

		public void SendMessage(dynamic message, WebSocket socket, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (message != null && socket != null)
			{
				try
                {
                    var stringMessage = _messageService.ParseMessage(message);

					lock (socket)
					{
						var segmentedMessage = new ArraySegment<byte>(Encoding.UTF8.GetBytes(stringMessage));
					    socket.SendAsync(segmentedMessage, WebSocketMessageType.Text, true, cancellationToken).Wait(cancellationToken);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			}
		}
	}
}
