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
        private static Dictionary<string, Player> _players;

        public static int State { get; private set; } = 1;

        private static Map Map { get; set; } 


        public static void Initialize()
        {
            LoadMap(ConfigurationManager.AppSettings.Get("MapPath"));
            _players = new Dictionary<string, Player>();

            for (int i = 0; i < 4; i++)
            {
                var rand = new Random();
                var fZone = Map._spawnZones[0];
                var sZone = Map._spawnZones[1];
                var x = (float)(fZone.X + rand.NextDouble() * fZone.Width);
                var y = (float)(fZone.Y + rand.NextDouble() * fZone.Height);

                _players.Add(Guid.NewGuid().ToString(), CreatePlayer(new Vector2(x, y), 1));
                x = (float)(sZone.X + rand.NextDouble() * sZone.Width);
                y = (float)(sZone.Y + rand.NextDouble() * sZone.Height);
                _players.Add(Guid.NewGuid().ToString(), CreatePlayer(new Vector2(x, y), 2));
            }
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

        public static void UpdatePosition(
            int dirX,
            int dirY,
            float globalMousePosX,
            float globalMousePosY,
            bool leftMouse,
            bool rightMouse,
            int inputId)
        {
            //var player = ;
            var dirrection = Vector2.Zero;
            dirrection.X += dirX;
            dirrection.Y += dirY;
            if (dirrection.X != 0 && dirrection.Y != 0)
            {
                dirrection.Normalize();
            }

            _players.First().Value.Rectangle += dirrection * Constants.MaxMoveSpeed;
            _players.First().Value.LastInputId = inputId;
        }
   
        public static List<(int, float, float)> GetPositions()
        {
            return _players.Select(p => (p.Value.LastInputId, p.Value.Rectangle.Min.X, p.Value.Rectangle.Min.Y)).ToList();
        }
    }
}
