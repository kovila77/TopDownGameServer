using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace TopDownGameServer
{
    public class UpdateUserStateEventArgs : EventArgs
    {
        public string Id { get; set; }
    }

    public static class Logic
    {
        public static Dictionary<string, Player> Players;

        public static int State { get; private set; } = 1;

        private static Map Map { get; set; }

        private static Dictionary<string, List<(DateTime, Vector2)>> Positions;


        //public delegate void UpdateUserStateDelegate(UpdateUserStateEventArgs e);
        //public static event UpdateUserStateDelegate UpdateUserEvent;
        public static Dictionary<string, CancellationTokenSource> canSendUserUpdate =
            new Dictionary<string, CancellationTokenSource>();

        public static DateTime startFrameTime = DateTime.Now;
        private static TimeSpan _lastFrameDuration = TimeSpan.Zero;


        public static void Initialize()
        {
            LoadMap(ConfigurationManager.AppSettings.Get("MapPath"));
            Players = new Dictionary<string, Player>();
            Positions = new Dictionary<string, List<(DateTime, Vector2)>>();

            for (int i = 0; i < 4; i++)
            {
                var rand = new Random();
                var fZone = Map._spawnZones[0];
                var sZone = Map._spawnZones[1];
                var x = (float)(fZone.X + rand.NextDouble() * fZone.Width);
                var y = (float)(fZone.Y + rand.NextDouble() * fZone.Height);
                var guid1 = Guid.NewGuid().ToString();
                var guid2 = Guid.NewGuid().ToString();
                Players.Add(guid1, CreatePlayer(new Vector2(x, y), 1));
                x = (float)(sZone.X + rand.NextDouble() * sZone.Width);
                y = (float)(sZone.Y + rand.NextDouble() * sZone.Height);
                Players.Add(guid2, CreatePlayer(new Vector2(x, y), 2));
                Positions.Add(guid1, new List<(DateTime, Vector2)>());
                Positions.Add(guid2, new List<(DateTime, Vector2)>());
            }

            UpdateGameState();
        }

        public static void UpdateGameState()
        {
            while (true)
            {

                lock (Players)
                {
                    _lastFrameDuration = DateTime.Now - startFrameTime;
                    startFrameTime = DateTime.Now;
                    foreach (var player in Players)
                    {
                        foreach (var input in player.Value.Inputs.OrderBy(x => x.Time))
                        {
                            UpdatePosition(player.Key, input);
                        }
                        player.Value.Inputs.Clear();
                    }
                }

                foreach (var player in Players)
                {
                    if (canSendUserUpdate.ContainsKey(player.Key))
                    {
                        canSendUserUpdate[player.Key].Cancel();
                    }
                }

                var ty = 16 - (startFrameTime - DateTime.Now).TotalMilliseconds;
                Thread.Sleep(Convert.ToInt32(ty));
            }
        }

        public static string GetPlayerId()
        {
            var fTeamCount = Players.Count(p => p.Value.Used && p.Value.Team == 1);
            var sTeamCount = Players.Count(p => p.Value.Used && p.Value.Team == 2);
            var _player = Players.Where(p => !p.Value.Used &&
                (fTeamCount <= sTeamCount ? p.Value.Team == 1 : p.Value.Team == 2)).FirstOrDefault();
            _player.Value.Used = true;
            return _player.Key;
        }

        private static void LoadMap(string path)
        {
            var formatter = new XmlSerializer(typeof(Map));
            using (var reader = XmlReader.Create(path))
            {
                Map = (Map)formatter.Deserialize(reader);
            }
        }

        private static Player CreatePlayer(Vector2 position, int team)
        {
            return new Player(team,
                new Circle(new Vector2(Constants.EntitySize.X / 2, Constants.EntitySize.Y - Constants.EntitySize.X / 2),
                Constants.EntitySize.X / 2),
                new RectangleF(Vector2.Zero, Constants.EntitySize) + position);
        }

        public static void UpdatePosition(string pId, Input input)
        {
            var direction = Vector2.Zero;
            direction.X += input.DirX;
            direction.Y += input.DirY;
            if (direction.X != 0 && direction.Y != 0)
            {
                direction.Normalize();
            }

            // TODO assert (_lastFrameDuration < sum of all input.SimulationTime)
            Players[pId].Rectangle += direction * Constants.MaxMoveSpeed * input.SimulationTime;
            FixCollisions(Players[pId]);
            Positions[pId].Add((DateTime.Now, Players[pId].Rectangle.Min));
        }

        public static void FixCollisions(Player player)
        {
            var moveToCorrect = Vector2.Zero;
            var interWalls = Map._walls.FindAll(w => player.HitCircle.Intersects(w.Rectangle));
            foreach (var interWall in interWalls)
            {
                var potentCircPos = player.HitCircle.Location;
                var nearestPoint = new Vector2(
                    Math.Max(interWall.Rectangle.Min.X, Math.Min(potentCircPos.X, interWall.Rectangle.Max.X)),
                    Math.Max(interWall.Rectangle.Min.Y, Math.Min(potentCircPos.Y, interWall.Rectangle.Max.Y)));
                var rayToNearest = nearestPoint - potentCircPos;
                var overlap = player.HitCircle.Radius - rayToNearest.Length();
                if (float.IsNaN(overlap))
                {
                    overlap = 0;
                }
                if (overlap > 0)
                {
                    rayToNearest.Normalize();
                    if (float.IsNaN(rayToNearest.X) || float.IsNaN(rayToNearest.Y))
                    {
                        rayToNearest = Vector2.Zero;
                    }
                    moveToCorrect += -rayToNearest * overlap;
                    player.Rectangle += (-rayToNearest * overlap);
                }
            }
        }

        public static List<(string, float, float)> GetPositions()
        {
            foreach (var plPositions in Positions)
            {
                plPositions.Value.RemoveAll(p => (DateTime.Now - p.Item1).TotalMilliseconds > 5000);
            }
            return Players.Select(p => (p.Key, p.Value.Rectangle.Min.X, p.Value.Rectangle.Min.Y)).ToList();
        }
    }
}
