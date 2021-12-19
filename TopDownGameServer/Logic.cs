using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace TopDownGameServer
{
    public static class Logic
    {
        public static Dictionary<string, Player> Players;

        public static int State { get; private set; } = 1;

        private static Map Map { get; set; }

        private static Dictionary<string, List<(DateTime, Vector2)>> Positions;


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

        public static Player UpdatePosition(
            int dirX,
            int dirY,
            float globalMousePosX,
            float globalMousePosY,
            bool leftMouse,
            bool rightMouse,
            int inputId,
            string id)
        {
            var dirrection = Vector2.Zero;
            dirrection.X += dirX;
            dirrection.Y += dirY;
            if (dirrection.X != 0 && dirrection.Y != 0)
            {
                dirrection.Normalize();
            }

            Players[id].Rectangle += dirrection * Constants.MaxMoveSpeed;
            Players[id].LastInputId = inputId;
            FixCollisions(Players[id]);
            Positions[id].Add((DateTime.Now, Players[id].Rectangle.Min));
            return Players[id];
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
            foreach(var plPositions in Positions)
            {
                plPositions.Value.RemoveAll(p => (DateTime.Now - p.Item1).TotalMilliseconds > 5000);
            }
            return Players.Select(p => (p.Key, p.Value.Rectangle.Min.X, p.Value.Rectangle.Min.Y)).ToList();
        }
    }
}
