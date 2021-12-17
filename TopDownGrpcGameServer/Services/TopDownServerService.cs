using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public override async Task<Empty> UpdateUserState(IAsyncStreamReader<ControlStateRequest> requestStream, ServerCallContext context)
        {
            await foreach (var request in requestStream.ReadAllAsync())
            {
                Logic.UpdatePosition(
                    request.Left,
                    request.Right,
                    request.Up,
                    request.Down,
                    request.GlobalMousePosX,
                    request.GlobalMousePosY,
                    request.LeftMouse,
                    request.RightMouse);
            }
            return new Empty();
        }

        public override Task<VectorsResponce> RetrieveEntites(Empty request, ServerCallContext context)
        {
            var vectors = new VectorsResponce();
            var positions = Logic.GetPositions();
            vectors.Vectors.AddRange(positions.Select(p => new Vector() { X = p.Item1, Y = p.Item2 }));
            return Task.FromResult(vectors);
        }
    }
}
