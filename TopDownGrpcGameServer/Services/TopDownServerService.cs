using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

        public override async Task<Empty> UpdateUserState(IAsyncStreamReader<ControlStateRequest> requestStream, ServerCallContext context)
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
            }
            return new Empty();
        }

        public override async Task RetrieveEntites(Empty request, IServerStreamWriter<VectorsResponce> responseStream, ServerCallContext context)
        {
            while (true)
            {
                var vectors = new VectorsResponce();
                var positions = Logic.GetPositions();
                vectors.Vectors.AddRange(positions.Select(p => new Vector() { LastInputId = p.Item1, X = p.Item2, Y = p.Item3 }));

                await responseStream.WriteAsync(vectors);
                await Task.Delay(TimeSpan.FromMilliseconds(2));
            }
        }
    }
}
