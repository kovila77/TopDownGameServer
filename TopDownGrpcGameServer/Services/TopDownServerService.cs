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
                var _player = Logic.UpdatePosition(
                    request.DirX,
                    request.DirY,
                    request.InputId,
                    request.Id);

                Logic.CheckShoots(
                    request.GlobalMousePosX,
                    request.GlobalMousePosY,
                    request.LeftMouse,
                    request.RightMouse,
                    request.Id);

                await responseStream.WriteAsync(new PlayerDataResponse()
                {
                    LastInputId = _player.LastInputId,
                    Position = new Vector2() { X = _player.Rectangle.Min.X, Y = _player.Rectangle.Min.Y }
                });
            }
        }

        public override async Task RetrieveUpdate(Empty request, IServerStreamWriter<UpdateResponse> responseStream, ServerCallContext context)
        {
            while (true)
            {
                var entitiesResponse = new UpdateResponse();
                var positions = Logic.GetPositions();
                entitiesResponse.Entities.AddRange(positions.Select(p => new Entity()
                {
                    Id = p.Item1,
                    Position = new Vector2() { X = p.Item2, Y = p.Item3 },
                    IsDead = p.Item4,
                }));
                lock (Logic.Bullets)
                {
                    Logic.CheckHitsAndDeadBullets();
                    entitiesResponse.Bullets.AddRange(Logic.Bullets.Select(b => new Bullet()
                    {
                        CreationTime = Timestamp.FromDateTime(b.CreationTime.ToUniversalTime()),
                        StartPos = new Vector2() { X = b.StartPoint.X, Y = b.StartPoint.Y },
                        EndPos = new Vector2() { X = b.EndPoint.X, Y = b.EndPoint.Y },
                        Team = b.Team,
                        Speed = b.Speed,
                        Id = b.Id,
                    }));
                }

                await responseStream.WriteAsync(entitiesResponse);
                await Task.Delay(TimeSpan.FromMilliseconds(16));
            }
        }

        public override async Task<Map> GetMap(Empty request, ServerCallContext context)
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

        public override async Task<UpdateResponse> GetEntities(Empty request, ServerCallContext context)
        {
            var entitiesResponse = new UpdateResponse();
            entitiesResponse.Entities.AddRange(Logic.Players.Select(p => new Entity()
            {
                Id = p.Key,
                Team = p.Value.Team,
                Position = new Vector2() { X = p.Value.Rectangle.Min.X, Y = p.Value.Rectangle.Min.Y }
            }));
            return entitiesResponse;
        }

        public override async Task<Entity> GetPlayerId(Empty request, ServerCallContext context)
        {

            return new Entity() { Id = Logic.GetPlayerId() };
        }
    }
}
