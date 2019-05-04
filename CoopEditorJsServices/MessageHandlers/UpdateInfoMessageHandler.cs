﻿using CoopEditorJsServices.Interfaces;
using CoopEditorJSEnitites;
using CoopEditorJSEnitites.Enums;
using CoopEditorJSEnitites.Messages;

namespace CoopEditorJsServices.MessageHandlers
{
    public class UpdateInfoMessageHandler : BaseMessageHandler<ControllMessage>
    {
        private readonly IWebSocketsService _webSocketsService;
        private readonly IMessageService _messageService;

        public UpdateInfoMessageHandler(IRoomService roomService, IWebSocketsService webSocketsService,
            IMessageService messageService) : base(roomService)
        {
            _webSocketsService = webSocketsService;
            _messageService = messageService;
        }

        public bool Handle(ControllMessage message)
        {
            if (message.CommandType == CommandsTypes.UpdateInformation)
            {
                _webSocketsService.SendMessage(new ControllMessage
                {
                    Content = _messageService.ParseMessage(new UserInfo { RoomId = message.RoomId, Rooms = _roomService.GetAllRooms() }),
                    CommandType = CommandsTypes.UpdateInformation,
                    User = null
                }, message.User.WebSocket);

                return true;
            }

            return false;
        }
    }
}