using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace TopDownGameServer
{
    public struct Circle
    {
        private float _radius;
        public Vector2 Location;

        public Circle(Vector2 location = new Vector2(), float radius = 0.0f)
        {
            _radius = radius;
            Location = location;
        }

        public float X { get => Location.X; set => Location.X = value; }
        public float Y { get => Location.Y; set => Location.Y = value; }

        public float Radius { get => _radius; set => _radius = value; }

        public bool Contain(Vector2 pos)
        {
            return Radius * Radius > (Location - pos).LengthSquared();
        }

        public bool Intersects(Circle circ)
        {
            return (Location - circ.Location).LengthSquared() <= Radius * Radius + circ.Radius * circ.Radius;
        }

        public bool Intersects(RectangleF rect)
        {
            var exRect1 = new RectangleF(rect.Min - new Vector2(0, Radius), rect.Max + new Vector2(0, Radius));
            var exRect2 = new RectangleF(rect.Min - new Vector2(Radius, 0), rect.Max + new Vector2(Radius, 0));
            return exRect1.Contains(Location) || exRect2.Contains(Location) ||
                Contain(rect.Max) || Contain(rect.Min) ||
                Contain(new Vector2(rect.Max.X, rect.Min.Y)) || Contain(new Vector2(rect.Min.X, rect.Max.Y));
        }

        public static Circle operator +(Circle circ, Vector2 v)
        {
            return new Circle(circ.Location + v, circ.Radius);
        }
        public static Circle operator -(Circle circ, Vector2 v)
        {
            return new Circle(circ.Location - v, circ.Radius);
        }
    }
}
