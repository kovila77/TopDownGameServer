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
        public static List<Bullet> Bullets;
        private static int _bulletId;
        public static List<int> Rounds { get; set; }
        public static int CurrentRound { get; set; }
        public static DateTime StartRoundTime { get; set; }
        public static bool EndGame { get; set; }
        private static bool reInit;


        public static int State { get; private set; } = 1;

        private static Map Map { get; set; }

        private static Dictionary<string, List<(DateTime, Vector2)>> Positions;


        public static void Initialize()
        {
            LoadMap(ConfigurationManager.AppSettings.Get("MapPath"));
            Players = new Dictionary<string, Player>();
            Bullets = new List<Bullet>();
            Positions = new Dictionary<string, List<(DateTime, Vector2)>>();
            Rounds = new List<int>(new[] { 0, 0 });
            CurrentRound = 0;
            EndGame = false;

            for (int i = 0; i < 4; i++)
            {
                var guid1 = Guid.NewGuid().ToString();
                var guid2 = Guid.NewGuid().ToString();
                Players.Add(guid1, CreatePlayer(new Vector2(0, 0), 1));
                Players.Add(guid2, CreatePlayer(new Vector2(0, 0), 2));
                Positions.Add(guid1, new List<(DateTime, Vector2)>());
                Positions.Add(guid2, new List<(DateTime, Vector2)>());
            }
            reInit = false;
        }

        private static void InitializeRound()
        {
            _bulletId = 0;
            StartRoundTime = DateTime.Now;
            var rand = new Random();
            var fZone = Map._spawnZones[0];
            var sZone = Map._spawnZones[1];
            foreach (var player in Players)
            {
                if (player.Value.Team == 1)
                {
                    player.Value.Rectangle = new RectangleF(Vector2.Zero, Constants.EntitySize) +
                        new Vector2(
                        (float)(fZone.X + rand.NextDouble() * fZone.Width),
                        (float)(fZone.Y + rand.NextDouble() * fZone.Height));
                }
                else
                {
                    player.Value.Rectangle = new RectangleF(Vector2.Zero, Constants.EntitySize) +
                        new Vector2(
                        (float)(sZone.X + rand.NextDouble() * sZone.Width),
                        (float)(sZone.Y + rand.NextDouble() * sZone.Height));
                }
                player.Value.Hp = Constants.PlayerMaxHp;
            }
        }

        public static string GetPlayerId()
        {
            lock (Players)
            {
                var fTeamCount = Players.Count(p => p.Value.Used && p.Value.Team == 1);
                var sTeamCount = Players.Count(p => p.Value.Used && p.Value.Team == 2);
                var _player = Players.FirstOrDefault(p => !p.Value.Used &&
                    (fTeamCount <= sTeamCount ? p.Value.Team == 1 : p.Value.Team == 2));
                if (_player.Value is null)
                {
                    return null;
                }
                _player.Value.Used = true;

                if (Players.Where(p => p.Value.Used).Count() >= 2)
                {
                    InitializeRound();
                }
                return _player.Key;
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

        public static Player UpdatePosition(
            int dirX,
            int dirY,
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

        public static void CheckHitsAndDeadBullets()
        {
            Bullets.ForEach(b =>
            {
                var centerPos = b.StartPoint + Vector2.Normalize(b.EndPoint - b.StartPoint)
                 * b.Speed * (float)(DateTime.Now - b.CreationTime).TotalSeconds;
                var bHalfSize = new Vector2(Constants.BulletSize / 2);
                b.Rectangle = new RectangleF(centerPos - bHalfSize, centerPos + bHalfSize);
            });
            var removedBulletsId = Bullets.Where(
                b => b.HitCircle.Intersects(b.IntersectingWall) ||
                (float)(DateTime.Now - b.CreationTime).TotalSeconds >= b.MaxDistance / b.Speed).ToList();


            var interPlayersAndBullets = (from p in Players
                                          from b in Bullets
                                          where p.Value.Team != b.Team && p.Value.Hp > 0 &&
                                          p.Value.HitCircle.Intersects(b.HitCircle)
                                          group (p.Key, b) by b into pb
                                          select pb.First()).ToList();
            interPlayersAndBullets.ForEach(pb =>
            {
                Players[pb.Item1].Hp -= pb.Item2.Damage;
            });

            if (Bullets.Count != 0)
            {
                var aa = (float)(DateTime.Now - Bullets[0].CreationTime).TotalSeconds;
            }

            removedBulletsId.AddRange(interPlayersAndBullets.Select(pb => pb.Item2));
            removedBulletsId = removedBulletsId.Distinct().ToList();
            removedBulletsId.ForEach(rb => Bullets.Remove(rb));


        }

        public static void CheckShoots(
            float mousePosX,
            float mousePosY,
            bool leftMouseButPress,
            bool rightMouseButPress,
            string id)
        {
            if (leftMouseButPress && Players[id].Hp > 0)
            {
                lock (Bullets)
                {
                    CreateBullets(mousePosX, mousePosY, Players[id]);
                }
            }
        }

        private static void CreateBullets(float mousePosX, float mousePosY, Player player)
        {
            if (player.CurBulletsCount != 0)
            {
                if ((DateTime.Now - player.LastShotTime).TotalSeconds >= player.Gun.ShootDelay)
                {
                    var mPos = new Vector2(mousePosX, mousePosY);
                    player.CurBulletsCount--;
                    player.LastShotTime = DateTime.Now;
                    var angle = 0.0f;
                    var rand = new Random();
                    for (int i = 0; i < player.Gun.BulletsPerShot; i++)
                    {
                        if (player.Gun.BulletsPerShot != 1)
                        {
                            angle = ((float)rand.NextDouble() - 0.5f) / 2.0f;
                        }
                        Bullets.Add(Shoot(player.Rectangle.Center, mPos, angle, player));
                    }
                }
            }
            if (!player.IsReload && player.CurBulletsCount == 0)
            {
                player.IsReload = true;
                player.StartReloadTime = DateTime.Now;
            }
            if (player.IsReload && (DateTime.Now - player.StartReloadTime).TotalSeconds > player.Gun.ReloadTime)
            {
                player.IsReload = false;
                player.CurBulletsCount = player.Gun.Capacity;
            }
        }

        private static Bullet Shoot(Vector2 _from, Vector2 _to, float angle, Player player)
        {
            var shootDir = _to - _from;
            shootDir.Normalize();
            if (float.IsNaN(shootDir.X) || float.IsNaN(shootDir.Y))
            {
                shootDir = Vector2.One;
            }
            var cs = (float)Math.Cos(angle);
            var sn = (float)Math.Sin(angle);
            var tempSD = shootDir;
            shootDir = new Vector2(tempSD.X * cs - tempSD.Y * sn, tempSD.X * sn + tempSD.Y * cs);
            var startShootPos = _from;
            var endShootPos = startShootPos + shootDir * player.Gun.MaxDistance;
            var intersectedWall = RectangleF.Empty;
            var intersectedWalls = Map._walls.FindAll(wall => wall.Rectangle.Intersects(startShootPos, endShootPos)).ToList();
            if (intersectedWalls.Count != 0)
            {
                float lsMin = float.MaxValue;
                foreach (var interWall in intersectedWalls)
                {
                    var ls = (interWall.Rectangle.Location - startShootPos).LengthSquared();
                    if (ls < lsMin)
                    {
                        lsMin = ls;
                        intersectedWall = interWall.Rectangle;
                    }
                }
            }
            var bullet = new Bullet(
                new Circle(new Vector2(Constants.BulletSize / 2), Constants.BulletSize / 2),
                player.Gun.BulletDamage,
                player.Gun.BulletSpeed,
                intersectedWall,
                new RectangleF(startShootPos, startShootPos + new Vector2(Constants.BulletSize)) - new Vector2(Constants.BulletSize / 2)
                );
            bullet.StartPoint = startShootPos;
            bullet.EndPoint = endShootPos;
            bullet.Team = player.Team;
            bullet.Id = _bulletId++;
            bullet.MaxDistance = player.Gun.MaxDistance;
            return bullet;
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

        public static List<(string, float, float, bool)> GetPositions()
        {
            foreach (var plPositions in Positions)
            {
                plPositions.Value.RemoveAll(p => (DateTime.Now - p.Item1).TotalMilliseconds > 5000);
            }
            return Players.Select(p => (p.Key, p.Value.Rectangle.Min.X, p.Value.Rectangle.Min.Y, p.Value.Hp <= 0)).ToList();
        }

        public static void CheckRound()
        {
            var roundEnd = false;
            if (Players.Where(p => p.Value.Team == 2).All(p => p.Value.Hp <= 0))
            {
                Rounds[0]++;
                roundEnd = true;
            }
            if (Players.Where(p => p.Value.Team == 1).All(p => p.Value.Hp <= 0))
            {
                Rounds[1]++;
                roundEnd = true;
            }
            if ((DateTime.Now - StartRoundTime).TotalSeconds > Constants.RoundTime)
            {
                roundEnd = true;
                var t1Count = Players.Where(p => p.Value.Team == 1).Count();
                var t2Count = Players.Where(p => p.Value.Team == 2).Count();
                if (t1Count != t2Count)
                {
                    Rounds[t1Count > t2Count ? 0 : 1]++;
                }
            }
            if (roundEnd)
            {
                roundEnd = false;
                CurrentRound++;
                if (CurrentRound < Constants.RoundsCount)
                {
                    lock (Players)
                    {
                        InitializeRound();
                    }
                }
                else
                {
                    EndGame = true;
                    if (reInit)
                    {
                        Initialize();
                        reInit = false;
                    }
                }
            }
        }
    }
}
