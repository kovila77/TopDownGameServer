using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace TopDownGameServer
{
    public static class Constants
    {
        private readonly static Vector2 entitySize = new Vector2(20, 40);
        private readonly static float bulletSize = 20.0f;
        private readonly static float weaponLength = 12.0f;
        private readonly static float maxMoveSpeed = 6.0f;
        private readonly static float maxMoveSpeedWS = 3.0f;
        private readonly static float maxCameraOffset = 0.2f;
        private readonly static int playerMaxHp = 50;
        private readonly static int hpBarWidth = 300;
        private readonly static int roundsCount = 10;
        private readonly static double roundTime = 180;
        private readonly static double startTime = 5;
        private readonly static int maxPlayersCount = 8;

        public static Vector2 EntitySize => entitySize;
        public static float BulletSize => bulletSize;
        public static float WeaponLength => weaponLength;
        public static float MaxMoveSpeed => maxMoveSpeed;
        public static float MaxMoveSpeedWS => maxMoveSpeedWS;
        public static float MaxCameraOffset => maxCameraOffset;
        public static int PlayerMaxHp => playerMaxHp;
        public static int HpBarWidth => hpBarWidth;
        public static int RoundsCount => roundsCount;
        public static double RoundTime => roundTime;
        public static double StartTime => startTime;
        public static int MaxPlayersCount => maxPlayersCount;
    }
}
