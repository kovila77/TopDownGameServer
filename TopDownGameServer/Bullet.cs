using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace TopDownGameServer
{
    public class Bullet : Entity
    {
        private int _damage;
        private RectangleF _intersectingWall;
        private Vector2 _endPoint;
        private Vector2 _startPoint;
        public RectangleF IntersectingWall { get => _intersectingWall; set => _intersectingWall = value; }
        public Vector2 EndPoint { get => _endPoint; set => _endPoint = value; }
        public Vector2 StartPoint { get => _startPoint; set => _startPoint = value; }
        public int Damage { get => _damage; set => _damage = value; }
        public float Speed { get; set; }
        public DateTime CreationTime { get; set; }
        public int Id { get; set; }

        public Bullet(
            Circle hitCircle,
            int damage,
            float speed,
            RectangleF intersectingWall = new RectangleF(),
            RectangleF rectangle = new RectangleF()) : base(hitCircle, rectangle)
        {
            _damage = damage;
            _intersectingWall = intersectingWall;
            CreationTime = DateTime.Now;
            Speed = speed;
        }        
    }
}
