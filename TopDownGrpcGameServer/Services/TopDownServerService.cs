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

        public override async Task<Empty> SendUserState(IAsyncStreamReader<ControlStateRequest> requestStream, ServerCallContext context)
        {
            await foreach (var controlStateRequest in requestStream.ReadAllAsync())
            {
                lock (Logic.Players)
                {
                    Logic.Players[controlStateRequest.PlayerMove.Id].Inputs.Add(new Input()
                    {
                        DirX = controlStateRequest.PlayerMove.DirX,
                        DirY = controlStateRequest.PlayerMove.DirY,
                        GlobalMousePosX = controlStateRequest.PlayerMove.GlobalMousePosX,
                        GlobalMousePosY = controlStateRequest.PlayerMove.GlobalMousePosY,
                        LeftMouse = controlStateRequest.PlayerMove.LeftMouse,
                        RightMouse = controlStateRequest.PlayerMove.RightMouse,
                        SimulationTime = controlStateRequest.PlayerMove.MsDuration,
                        Time = controlStateRequest.PlayerMove.Time,
                    });
                }
            }

            return new Empty();
        }

        public override async Task UpdateGameState(PlayerId request, IServerStreamWriter<GameStateResponse> responseStream, ServerCallContext context)
        {
            CancellationTokenSource cancellationTokenSource = Logic.canSendUserUpdate[request.Id];

            while (true)
            {
                cancellationTokenSource.Token.WaitHandle.WaitOne();

                try
                {
                    lock (responseStream)
                    {
                        if (context.CancellationToken.IsCancellationRequested){
                            Logic.canSendUserUpdate.Remove(request.Id);
                            break;
                        }

                        GameStateResponse gameStateResponse = new GameStateResponse();
                        lock (Logic.Players)
                        {
                            gameStateResponse.PlayerServerPosition = new PlayerServerPosition()
                            {
                                Time = Logic.startFrameTime.ToFileTime(),
                                Position = new Vector2()
                                {
                                    X = Logic.Players[request.Id].Rectangle.Min.X,
                                    Y = Logic.Players[request.Id].Rectangle.Min.Y
                                }
                            };
                        }

                        // TODO lock positions
                        var positions = Logic.GetPositions();
                        gameStateResponse.Entities.AddRange(positions.Select(p => new Entity()
                        {
                            Id = p.Item1,
                            Position = new Vector2() { X = p.Item2, Y = p.Item3 }
                        }));

                        responseStream.WriteAsync(gameStateResponse).Wait();
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }

        //public async override Task UpdateUserState(IAsyncStreamReader<ControlStateRequest> requestStream, IServerStreamWriter<GameStateResponse> responseStream, ServerCallContext context)
        //{
        //    await foreach (var controlStateRequest in requestStream.ReadAllAsync())
        //    {
        //        GameStateResponse gameStateResponse = new GameStateResponse();
        //        lock (Logic.Players)
        //        {
        //            Logic.Players[controlStateRequest.PlayerMove.Id].Inputs.Add(new Input()
        //            {
        //                DirX = controlStateRequest.PlayerMove.DirX,
        //                DirY = controlStateRequest.PlayerMove.DirY,
        //                GlobalMousePosX = controlStateRequest.PlayerMove.GlobalMousePosX,
        //                GlobalMousePosY = controlStateRequest.PlayerMove.GlobalMousePosY,
        //                LeftMouse = controlStateRequest.PlayerMove.LeftMouse,
        //                RightMouse = controlStateRequest.PlayerMove.RightMouse,
        //                SimulationTime = controlStateRequest.PlayerMove.MsDuration,
        //                Time = controlStateRequest.PlayerMove.Time,
        //            });

        //            gameStateResponse.PlayerServerPosition = new PlayerServerPosition()
        //            {
        //                Time = Logic.startFrameTime.ToFileTime(),
        //                Position = new Vector2()
        //                {
        //                    X = Logic.Players[controlStateRequest.PlayerMove.Id].Rectangle.Min.X,
        //                    Y = Logic.Players[controlStateRequest.PlayerMove.Id].Rectangle.Min.Y
        //                }
        //            };
        //        }


        //        var positions = Logic.GetPositions();
        //        gameStateResponse.Entities.AddRange(positions.Select(p => new Entity()
        //        {
        //            Id = p.Item1,
        //            Position = new Vector2() { X = p.Item2, Y = p.Item3 }
        //        }));

        //        await responseStream.WriteAsync(gameStateResponse);
        //    }
        //}

        //public async override Task UpdateUserState(IAsyncStreamReader<ControlStateRequest> requestStream, IServerStreamWriter<PlayerDataResponse> responseStream, ServerCallContext context)
        //{
        //    await foreach (var request in requestStream.ReadAllAsync())
        //    {
        //        var _player = Logic.UpdatePosition(
        //            request.DirX,
        //            request.DirY,
        //            request.GlobalMousePosX,
        //            request.GlobalMousePosY,
        //            request.LeftMouse,
        //            request.RightMouse,
        //            request.InputId,
        //            request.Id);
        //        await responseStream.WriteAsync(new PlayerDataResponse()
        //        {
        //            LastInputId = _player.LastInputId,
        //            Position = new Vector2() { X = _player.Rectangle.Min.X, Y = _player.Rectangle.Min.Y }
        //        });
        //    }
        //}

        //public override async Task RetrieveEntities(Empty request, IServerStreamWriter<EntitiesResponse> responseStream, ServerCallContext context)
        //{
        //    while (true)
        //    {
        //        var entitiesResponse = new EntitiesResponse();
        //        var positions = Logic.GetPositions();
        //        entitiesResponse.Entities.AddRange(positions.Select(p => new Entity()
        //        {
        //            Id = p.Item1,
        //            Position = new Vector2() { X = p.Item2, Y = p.Item3 }
        //        }));

        //        await responseStream.WriteAsync(entitiesResponse);
        //        await Task.Delay(TimeSpan.FromMilliseconds(16));
        //    }
        //}

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

        public override async Task<EntitiesResponse> GetEntities(Empty request, ServerCallContext context)
        {
            var entitiesResponse = new EntitiesResponse();
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
            // TODO lock
            var id = Logic.GetPlayerId();

            Logic.canSendUserUpdate.Add(id, new CancellationTokenSource());

            return new Entity() { Id = id };
        }
    }
}
