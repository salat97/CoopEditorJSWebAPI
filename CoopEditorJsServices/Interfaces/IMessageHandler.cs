﻿using CoopEditorJSEnitites.Messages;

namespace CoopEditorJsServices.Interfaces
{
	public interface IMessageHandler<T> where T : BaseMessage
	{
		bool Handle(T message);
	}
}
