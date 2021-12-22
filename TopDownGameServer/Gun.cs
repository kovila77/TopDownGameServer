using System;
using System.Collections.Generic;
using System.Text;

namespace TopDownGameServer
{
    public class Gun
    {
        public double ReloadTime;
        public double ShootDelay;
        public int BulletDamage;
        public int BulletSpeed;
        public float MaxDistance;
        public int Capacity;
        public int BulletsPerShot;

        public Gun(int type)
        {
            switch (type)
            {
                case 1:
                    ReloadTime = 3;
                    ShootDelay = 0.6;
                    BulletDamage = 50;
                    BulletSpeed = 1500;
                    MaxDistance = 1000;
                    Capacity = 8;
                    BulletsPerShot = 1;
                    break;
                case 2:
                    ReloadTime = 2;
                    ShootDelay = 0.2;
                    BulletDamage = 10;
                    BulletSpeed = 1200;
                    MaxDistance = 600;
                    Capacity = 20;
                    BulletsPerShot = 1;
                    break;
                default:
                    ReloadTime = 1;
                    ShootDelay = 0.5;
                    BulletDamage = 10;
                    BulletSpeed = 900;
                    MaxDistance = 300;
                    Capacity = 4;
                    BulletsPerShot = 8;
                    break;
            }
        }
    }
}
