using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace TopDownGameServer
{
    public class Player : Entity
    {
        private int _hp = Constants.PlayerMaxHp;
        private Gun _gun;
        private int _curBulletsCount;
        public int Hp { get => _hp; set => _hp = value; }
        public Gun Gun { get => _gun; set => _gun = value; }
        public int CurBulletsCount { get => _curBulletsCount; set => _curBulletsCount = value; }
        public int LastInputId { get; set; }
        public bool Used { get; set; }
        public DateTime LastShotTime { get; set; }
        public bool IsReload { get; set; }
        public DateTime StartReloadTime { get; set; }

        public Player(
            int team,
            Circle hitCircle,
            RectangleF rectangle = new RectangleF(),
            Vector2 speed = new Vector2()) : base(hitCircle, rectangle, speed)
        {
            Used = false;
            Team = team;
            LastShotTime = DateTime.Now;
            IsReload = false;
            StartReloadTime = DateTime.Now;
            var rand = new Random();
            //_gun = new Gun(rand.Next(1, 4));
            _gun = new Gun(2);
            _curBulletsCount = _gun.Capacity;
        }
    }
}
