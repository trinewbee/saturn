using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Puff.NetCore;
using Nano.Logs;
using TestAspNetCore.Hubs.Interfaces;
using TestAspNetCore.Services;

namespace TestAspNetCore.Hubs
{
    public class ChatHub : Hub<IChatClient>
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// WebSocket 连接建立时触发
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            System.Console.WriteLine($"[ChatHub] Client connected: {connectionId}");
            
            // 通知所有客户端有新用户连接
            await Clients.All.ReceiveSystemNotification($"User {connectionId} connected");
            
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// WebSocket 连接断开时触发
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var connectionId = Context.ConnectionId;
            var reason = exception?.Message ?? "Normal disconnect";
            System.Console.WriteLine($"[ChatHub] Client disconnected: {connectionId}, Reason: {reason}");
            
            // 通知所有客户端有用户断开
            await Clients.All.ReceiveSystemNotification($"User {connectionId} disconnected");
            
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// 发送消息到所有客户端
        /// </summary>
        public async Task SendMessage(string user, string message)
        {
            var env = WebGlobal.curEnv;
            System.Console.WriteLine($"[ChatHub] [{env?.reqId}] User {user} sending message: {message}");

            _chatService.ProcessMessage(user, message);
            await Clients.All.ReceiveMessage(user, message);
        }

        /// <summary>
        /// 加入指定房间
        /// </summary>
        public async Task JoinRoom(string roomName)
        {
            var connectionId = Context.ConnectionId;
            System.Console.WriteLine($"[ChatHub] User {connectionId} joining room: {roomName}");
            
            await Groups.AddToGroupAsync(connectionId, roomName);
            await Clients.Group(roomName).ReceiveSystemNotification($"User {connectionId} joined {roomName}");
        }

        /// <summary>
        /// 离开指定房间
        /// </summary>
        public async Task LeaveRoom(string roomName)
        {
            var connectionId = Context.ConnectionId;
            System.Console.WriteLine($"[ChatHub] User {connectionId} leaving room: {roomName}");
            
            await Groups.RemoveFromGroupAsync(connectionId, roomName);
            await Clients.Group(roomName).ReceiveSystemNotification($"User {connectionId} left {roomName}");
        }

        /// <summary>
        /// 发送消息到指定房间
        /// </summary>
        public async Task SendMessageToRoom(string roomName, string user, string message)
        {
            System.Console.WriteLine($"[ChatHub] User {user} sending message to room {roomName}: {message}");
            
            await Clients.Group(roomName).ReceiveMessage(user, message);
        }
    }
}
