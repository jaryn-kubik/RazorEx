using Assistant;
using System;
using System.IO;

namespace RazorEx
{
    public static class PositionCheck
    {
        private static readonly Point2D heroticUL = new Point2D(5120, 400);
        private static readonly Point2D heroticBR = new Point2D(5250, 511);
        private static readonly Point2D khaldunUL = new Point2D(5383, 1287);
        private static readonly Point2D khaldunBR = new Point2D(5624, 1505);
        private static readonly Point2D fireUL = new Point2D(5639, 1287);
        private static readonly Point2D fireBR = new Point2D(5881, 1516);
        private static readonly Point2D pandaUL = new Point2D(5383, 271);
        private static readonly Point2D pandaBR = new Point2D(5624, 504);
        private static readonly Point2D wispUL = new Point2D(624, 1423);
        private static readonly Point2D wispBR = new Point2D(1014, 1583);

        public static bool InHerotic { get; private set; }
        public static bool InKhaldun { get; private set; }
        public static bool InFire { get; private set; }
        public static bool InPanda { get; private set; }
        public static bool InWisp { get; private set; }
        public static bool IsAuberon { get; private set; }

        public static void OnInit()
        {
            PacketHandler.RegisterServerToClientFilter(0x20, OnMobileUpdate);
            PacketHandler.RegisterClientToServerViewer(0xB1, OnGumpResponse);
        }

        private static void OnGumpResponse(PacketReader p, PacketHandlerEventArgs e)
        {
            p.Seek(7, SeekOrigin.Begin);
            if (p.ReadUInt32() != 0x0322B295)
                return;
            uint button = p.ReadUInt32();
            if (button > 400 && button < 410)
                IsAuberon = true;
            else
                IsAuberon = false;
        }

        private static void OnMobileUpdate(Packet p, PacketHandlerEventArgs args)
        {
            if (p.ReadUInt32() != World.Player.Serial)
                return;

            if (IsAuberon && World.Player.Map != 2)
                IsAuberon = false;

            bool isHerotic = World.Player.Position.InBounds(heroticUL, heroticBR) && World.Player.Map == 0;
            if (isHerotic && !InHerotic)
                enterHerotic();
            else if (!isHerotic && InHerotic)
                leaveHerotic();

            bool isKhaldun = World.Player.Position.InBounds(khaldunUL, khaldunBR) && World.Player.Map == 0;
            if (isKhaldun && !InKhaldun)
                enterKhaldun();
            else if (!isKhaldun && InKhaldun)
                leaveKhaldun();

            bool isFire = World.Player.Position.InBounds(fireUL, fireBR) && World.Player.Map == 0;
            if (isFire && !InFire)
                InFire = true;
            else if (!isFire && InFire)
                InFire = false;

            bool isPanda = World.Player.Position.InBounds(pandaUL, pandaBR) && World.Player.Map == 1;
            if (isPanda && !InPanda)
                InPanda = true;
            else if (!isPanda && InPanda)
                InPanda = false;

            bool isWisp = World.Player.Position.InBounds(wispUL, wispBR) && !IsAuberon;
            if (isWisp && !InWisp)
                InWisp = true;
            else if (!isWisp && InWisp)
                InWisp = false;
        }

        private static bool InBounds(this Point3D p, Point2D upperLeft, Point2D bottomRight)
        { return p.X >= upperLeft.X && p.X <= bottomRight.X & p.Y >= upperLeft.Y && p.Y <= bottomRight.Y; }

        private static Action enterKhaldun = () => InKhaldun = true;
        public static event Action EnterKhaldun
        {
            add { enterKhaldun += value; }
            remove { enterKhaldun -= value; }
        }

        private static Action leaveKhaldun = () => InKhaldun = false;
        public static event Action LeaveKhaldun
        {
            add { leaveKhaldun += value; }
            remove { leaveKhaldun -= value; }
        }

        private static Action enterHerotic = () => InHerotic = true;
        public static event Action EnterHerotic
        {
            add { enterHerotic += value; }
            remove { enterHerotic -= value; }
        }

        private static Action leaveHerotic = () => InHerotic = false;
        public static event Action LeaveHerotic
        {
            add { leaveHerotic += value; }
            remove { leaveHerotic -= value; }
        }
    }
}