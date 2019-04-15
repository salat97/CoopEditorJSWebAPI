﻿using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using CoopEditorJsServices.Interfaces;
using CoopEditorJSEnitites.Messages;
using CoopEditorJSWebAPI.Configuration;
using Microsoft.AspNetCore.Http;

namespace CoopEditorJsServices.Middleware
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

			CancellationToken requesdToken = context.RequestAborted;
			WebSocket currentSocket = await context.WebSockets.AcceptWebSocketAsync();

			while (currentSocket.State == WebSocketState.Open && !requesdToken.IsCancellationRequested)
			{
				try
				{
					string rawMessage = await _webSocketsService.ExtractMessage(currentSocket, requesdToken);
					var extractedMessage = _messageService.DeserializeMessage(rawMessage);
					_messageProcessor.ProcessMessage(extractedMessage);
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

			await currentSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure", requesdToken);
		}
	}
}
