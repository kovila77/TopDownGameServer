using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using TopDownGameServer;

namespace TopDownGrpcGameServer
{
    public class TopDownServerService : TopDownServer.TopDownServerBase
    {
        private readonly ILogger<TopDownServerService> _logger;
        public TopDownServerService(ILogger<TopDownServerService> logger)
        {
            _logger = logger;
        }

        public async override Task UpdateUserState(IAsyncStreamReader<ControlStateRequest> requestStream, IServerStreamWriter<PlayerDataResponse> responseStream, ServerCallContext context)
        {
            await foreach (var request in requestStream.ReadAllAsync())
            {
                Logic.UpdatePosition(
                    request.DirX,
                    request.DirY,
                    request.GlobalMousePosX,
                    request.GlobalMousePosY,
                    request.LeftMouse,
                    request.RightMouse,
                    request.InputId);
                await responseStream.WriteAsync(new PlayerDataResponse() { LastInputId = 1, Position = new Vector2() { X = 2, Y = 3 } });
            }
        }

        public override async Task RetrieveEntities(Empty request, IServerStreamWriter<EntitiesResponse> responseStream, ServerCallContext context)
        {
            while (true)
            {
                var entitiesResponse = new EntitiesResponse();
                var positions = Logic.GetPositions();
                entitiesResponse.Entities.AddRange(positions.Select(p => new Entity() { Id = "p.Item1", Position = new Vector2() { X = p.Item2, Y = p.Item3 } }));

                await responseStream.WriteAsync(entitiesResponse);
                await Task.Delay(TimeSpan.FromMilliseconds(2));
            }
        }

        public async override Task<Map> GetMap(Empty request, ServerCallContext context)
        {
            string map = null;

            try
            {
                map = File.ReadAllText(ConfigurationManager.AppSettings.Get("MapPath"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new Map() { MapStr = map };
        }


    }
}
