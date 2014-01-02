using System;
using Assistant;

namespace RazorEx
{
    public static class Honor
    {
        public static Serial Current { get; private set; }
        public static int Perfection { get; private set; }

        public static void OnInit()
        {
            Event.LocalizedMessage += Event_LocalizedMessage;
            Event.RemoveObject += Event_RemoveObject;
            Event.PlayerMoved += Event_PlayerMoved;
        }

        private static void Event_PlayerMoved()
        {
            if (!World.Mobiles.ContainsKey(Current))
            {
                Current = Serial.Zero;
                Perfection = 0;
                if (end != null)
                    end();
            }
        }

        private static void Event_RemoveObject(Serial serial)
        {
            if (serial == Current)
            {
                Current = Serial.Zero;
                Perfection = 0;
                if (end != null)
                    end();
            }
        }

        private static void Event_LocalizedMessage(Serial serial, int num, string args)
        {
            switch (num)
            {
                case 1063231: // I honor you
                    if (serial == World.Player.Serial && Targeting.m_LastTarget != null)
                    {
                        Current = Targeting.m_LastTarget.Serial;
                        if (start != null)
                            start();
                    }
                    return;
                case 1063254: // You have Achieved Perfection in inflicting damage to this opponent!
                    Perfection = 100;
                    break;
                case 1063255: // You gain in Perfection as you precisely strike your opponent.
                    Perfection = Math.Min(Perfection + (int)World.Player.Skills[(int)SkillName.Bushido].Value / 10, 100);
                    break;
                case 1063256: // You have lost all Perfection in fighting this opponent.
                    Perfection = 0;
                    break;
                case 1063257: // You have lost some Perfection in fighting this opponent.
                    Perfection -= 25;
                    break;
                case 1063225: // You cannot gain more Honor.
                case 1063226: // You have gained a path in Honor!
                case 1063228: // You have gained in Honor.
                    Current = Serial.Zero;
                    Perfection = 0;
                    if (end != null)
                        end();
                    return;
                default:
                    return;
            }
            if (change != null)
                change();
        }

        private static Action start;
        public static event Action Start
        {
            add { start += value; }
            remove { start -= value; }
        }

        private static Action end;
        public static event Action End
        {
            add { end += value; }
            remove { end -= value; }
        }

        private static Action change;
        public static event Action Change
        {
            add { change += value; }
            remove { change -= value; }
        }
    }
}