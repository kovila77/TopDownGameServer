using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace TopDownGameServer
{
    public class Entity
    {
        private RectangleF _rectangle;
        private int _team;
        private Vector2 _speed;
        private Circle _hitCircle;

        public RectangleF Rectangle { get => _rectangle; set => _rectangle = value; }
        public int Team1 { get => _team; set => _team = value; }
        public Circle HitCircle { get => _hitCircle + Rectangle.Location; set => _hitCircle = value; }
        public int Team { get => Team1; set => Team1 = value; }

        public Entity(Circle hitCircle, RectangleF rectangle = new RectangleF(), Vector2 speed = new Vector2())
        {
            _rectangle = rectangle;
            _hitCircle = hitCircle;
            _speed = speed;
        }
    }
}
