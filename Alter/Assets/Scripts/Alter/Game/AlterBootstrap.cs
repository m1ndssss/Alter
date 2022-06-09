using System;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine.Scripting;

namespace Alter.Game
{
    [Preserve]
    public class AlterBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            AutoConnectPort = 42126;

            // ReSharper disable once InvertIf
            if (RequestedPlayType == PlayType.Client)
            {
                var connectAddress = Environment.GetEnvironmentVariable("ALTER_CONNECT_ADDRESS");

                if (
                    connectAddress is not null &&
                    NetworkEndPoint.TryParse(connectAddress, 0, out var connectEndpoint)
                )
                {
                    DefaultConnectAddress = connectEndpoint;
                }
            }

            return base.Initialize(defaultWorldName);
        }
    }
}
