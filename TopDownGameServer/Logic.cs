using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownGameServer
{
    public static class Logic
    {
        private static Dictionary<string, Player> _players;

        public static void Initialize()
        {
            _players = new Dictionary<string, Player>();
            _players.Add(Guid.NewGuid().ToString(), new Player(
                new Circle(new Vector2(Constants.EntitySize.X / 2, Constants.EntitySize.Y - Constants.EntitySize.X / 2),
                Constants.EntitySize.X / 2),
                new RectangleF(Vector2.Zero, Constants.EntitySize)));
        }

        public static void UpdatePosition(
            bool left,
            bool right,
            bool up,
            bool down,
            float globalMousePosX,
            float globalMousePosY,
            bool leftMouse,
            bool rightMouse)
        {
            //var player = ;
            var dirrection = Vector2.Zero;
            if (up)
            {
                dirrection.Y -= 1.0f;
            }
            if (down)
            {
                dirrection.Y += 1.0f;
            }
            if (left)
            {
                dirrection.X -= 1.0f;
            }
            if (right)
            {
                dirrection.X += 1.0f;
            }
            if (dirrection.X != 0 && dirrection.Y != 0)
            {
                dirrection.Normalize();
            }
            _players.First().Value.Rectangle += dirrection * Constants.MaxMoveSpeed;
        }
   
        public static List<(float, float)> GetPositions()
        {
            return _players.Select(p => (p.Value.Rectangle.Min.X, p.Value.Rectangle.Min.Y)).ToList();
        }
    }
}
