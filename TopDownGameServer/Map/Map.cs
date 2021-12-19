using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TopDownGameServer
{
    public class Map
    {
        public List<Ground> _grounds = new List<Ground>();
        public List<Wall> _walls = new List<Wall>();
        public int _commandsCount = 0;
        public List<RectangleF> _spawnZones = new List<RectangleF>();

        public Map() { }
    }
}
