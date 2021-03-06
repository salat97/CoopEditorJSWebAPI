﻿using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using CoopEditorJsServices.Interfaces;
using CoopEditorJSEnitites.Messages;
using CoOpEditor.WebAPI.Configuration;
using Microsoft.AspNetCore.Http;

namespace CoOpEditor.WebAPI.Middlewares
{
	public sealed class EditorWebSocketMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly IWebSocketsService _webSocketsService;
		private readonly IMessageService _messageService;
		private readonly IMessageProcessor _messageProcessor;

		public EditorWebSocketMiddleware(RequestDelegate next)
		{
			_next = next;
			_webSocketsService = DependencyInjectionConfiguration.GetContainer().GetInstance<IWebSocketsService>();
			_messageService = DependencyInjectionConfiguration.GetContainer().GetInstance<IMessageService>();
			_messageProcessor = DependencyInjectionConfiguration.GetContainer().GetInstance<IMessageProcessor>();
		}

		public async Task InvokeAsync(HttpContext context)
		{
			if (!context.WebSockets.IsWebSocketRequest)
			{
				await _next.Invoke(context);
				return;
			}

			var requestToken = context.RequestAborted;
			var currentSocket = await context.WebSockets.AcceptWebSocketAsync();
            var userId = Guid.NewGuid().ToString();

            while (currentSocket.State == WebSocketState.Open && !requestToken.IsCancellationRequested)
			{
				try
				{
                    var rawMessage = await _webSocketsService.ExtractMessage(currentSocket, requestToken);

                    if (!string.IsNullOrEmpty(rawMessage))
                    {
                         var extractedMessage = _messageService.DeserializeMessage(rawMessage);
                         extractedMessage.User.WebSocket = currentSocket;

                         if (string.IsNullOrEmpty(extractedMessage.User.Id))
                            extractedMessage.User.Id = userId;

                        _messageProcessor.ProcessMessage(extractedMessage);
                    }
				}
				catch (WebSocketException ex)
				{
					_messageProcessor.ProcessMessage(new ErrorMessage(ex.Message));
				}
				catch (Exception ex)
				{
					_messageProcessor.ProcessMessage(new ErrorMessage(ex.Message));
				}
			}

            await currentSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure", requestToken);
		}
	}
}
