using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace TopDownGameServer
{
    public class Input
    {
        public int DirX { get; set; }
        public int DirY { get; set; }
        public float GlobalMousePosX { get; set; }
        public float GlobalMousePosY { get; set; }
        public bool LeftMouse { get; set; }
        public bool RightMouse { get; set; }
        public int SimulationTime { get; set; }
        public long Time { get; set; }
    }

    public class Player : Entity
    {
        private int _hp = Constants.PlayerMaxHp;
        private int _gunType;
        private int _curBulletsCount;
        public int Hp { get => _hp; set => _hp = value; }
        public int GunType { get => _gunType; set => _gunType = value; }
        public int CurBulletsCount { get => _curBulletsCount; set => _curBulletsCount = value; }
        public bool Used { get; set; }

        public List<Input> Inputs { get; set; } = new List<Input>();

        public Player(
            int team,
            Circle hitCircle,
            RectangleF rectangle = new RectangleF(),
            Vector2 speed = new Vector2()) : base(hitCircle, rectangle, speed)
        {
            Used = false;
            Team = team;
            var rand = new Random();
            _gunType = rand.Next(1, 4);
        }
    }
}
