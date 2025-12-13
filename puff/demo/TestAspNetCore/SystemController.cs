using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Puff.NetCore;
using TestAspNetCore.Hubs;
using TestAspNetCore.Hubs.Interfaces;

namespace TestAspNetCore
{
    [Route("[controller]")]
    [ApiController]
    public class SystemController : JmController
    {
        private readonly IHubContext<ChatHub, IChatClient> _chatHub;

        public SystemController(IHubContext<ChatHub, IChatClient> chatHub)
        {
            _chatHub = chatHub;
        }

        [IceApi(Flags = IceApiFlag.JsonIn)]
        IceApiResponse Broadcast(string message)
        {
            _chatHub.Clients.All.ReceiveSystemNotification($"[System Admin]: {message}");
            return IceApiResponse.String("Broadcast sent");
        }
    }
}
