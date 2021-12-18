using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace TopDownGameServer
{
    public class Map
    {
        public List<RectangleF> _grounds = new List<RectangleF>();
        public List<RectangleF> _walls = new List<RectangleF>();
        public int _commandsCount = 0;
        public List<RectangleF> _spawnZones = new List<RectangleF>();

        public Map() { }
    }
}
