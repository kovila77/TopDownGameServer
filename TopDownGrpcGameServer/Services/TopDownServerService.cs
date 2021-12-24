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
            int curGameCount = Logic.GamesCount;

            try
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
                        Position = new Vector2() { X = _player.Rectangle.Min.X, Y = _player.Rectangle.Min.Y },
                        HpPercent = _player.Hp < 0 ? 0 : (float)_player.Hp / (float)Constants.PlayerMaxHp,
                        ReloadPercent = (float)(_player.IsReload ? (DateTime.Now - _player.StartReloadTime).TotalSeconds / _player.Gun.ReloadTime : 0),
                        BulletsCount = _player.CurBulletsCount,
                        Capacity = _player.Gun.Capacity,
                    });

                    if (curGameCount != Logic.GamesCount)
                    {
                        await Task.Delay(3000);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public override async Task RetrieveUpdate(PlayerId request, IServerStreamWriter<UpdateResponse> responseStream, ServerCallContext context)
        {
            lock (Logic.Players)
            {
                if (!Logic.Players.ContainsKey(request.Id))
                {
                    throw new Exception("Unknown player id");
                }
            }

            int curGameCount = Logic.GamesCount;
            bool endgame = false;
            var rounds = Logic.Rounds;

            try
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
                        if (curGameCount == Logic.GamesCount)
                        {
                            Logic.CheckHitsAndDeadBullets();
                            Logic.CheckRound();
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
                    }

                    lock (Logic.Players)
                    {
                        if (!Logic.Players.ContainsKey(request.Id))
                        {
                            endgame = true;
                        }
                    }

                    entitiesResponse.RoundData = new RoundResponse()
                    {
                        FirstTeamScore = rounds[0],
                        SecondTeamScore = rounds[1],
                        IsEndGame = endgame,
                        CurrentRound = rounds[0] + rounds[1],
                        RoundTimeLeft = Duration.FromTimeSpan(TimeSpan.FromSeconds(Constants.RoundTime) - (DateTime.Now - Logic.StartRoundTime))
                    };

                    await responseStream.WriteAsync(entitiesResponse);

                    await Task.Delay(16);

                    if (endgame)
                    {
                        break;
                    }

                    lock (Logic.EndTimer)
                    {
                        Logic.EndTimer.Stop();
                        Logic.EndTimer.Start();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            //lock (Logic.ActivePlayers)
            //{
            //    if (Logic.ActivePlayers.Contains(Logic.Players[request.Id]))
            //        Logic.ActivePlayers.Remove(Logic.Players[request.Id]);
            //}
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

        public override async Task<UpdateResponse> GetEntities(PlayerId request, ServerCallContext context)
        {
            if (!Logic.Players.ContainsKey(request.Id))
            {
                throw new Exception("Unknown player id");
            }

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
            var playerId = Logic.GetPlayerId();
            return new Entity() { Id = string.IsNullOrEmpty(playerId) ? "" : playerId };
        }

        public override async Task<Empty> SendGunType(GunType request, ServerCallContext context)
        {
            if (Logic.Players.ContainsKey(request.PlayerId))
            {
                Logic.Players[request.PlayerId].Gun = new Gun(request.Type);
                Logic.Players[request.PlayerId].CurBulletsCount = Logic.Players[request.PlayerId].Gun.Capacity;
            }
            return new Empty();
        }
    }
}
