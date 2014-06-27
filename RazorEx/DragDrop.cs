using Assistant;
using System;
using System.Collections.Generic;

namespace RazorEx
{
    public static class DragDrop
    {
        private static readonly List<DragDropInfo> items = new List<DragDropInfo>();

        public static void OnInit()
        {
            PacketHandler.RemoveServerToClientViewer(0x27, (PacketViewerCallback)Delegate.CreateDelegate(typeof(PacketViewerCallback), typeof(PacketHandlers), "LiftReject"));
            PacketHandler.RegisterServerToClientViewer(0x27, LiftReject);
        }

        public static void Move(Item item, Serial to) { Move(item, World.FindItem(to)); }
        public static void Move(Item item, Item to)
        {
            if (item == null || to == null)
                return;
            items.Add(new DragDropInfo(item.Serial, to.Serial));
            DragDropManager.DragDrop(item, to);
        }

        public static void Move(Item item, Item to, Point3D position)
        {
            if (item == null || to == null)
                return;
            items.Add(new DragDropInfo(item.Serial, to.Serial, position));
            DragDropManager.Drag(item, item.Amount);
            DragDropManager.Drop(item, to, position);
        }

        public static void Dress(Item item, Layer layer)
        {
            if (item == null)
                return;
            items.Add(new DragDropInfo(item.Serial, layer));
            DragDropManager.DragDrop(item, World.Player, layer);
        }

        private static void LiftReject(PacketReader p, PacketHandlerEventArgs args)
        {
            Item item = World.FindItem(DragDropManager.m_Holding);
            if (DragDropManager.LiftReject() || item == null)
                return;
            DragDropInfo info = items.Find(i => i.Drag == item.Serial);
            if (info == null)
                return;
            args.Block = true;
            if (World.FindItem(info.Drop) == item.Container)
                return;

            if (info.Layer != Layer.Invalid)
                DragDropManager.DragDrop(item, World.Player, info.Layer);
            else if (info.Position != Point3D.Zero)
            {
                DragDropManager.Drag(item, item.Amount);
                DragDropManager.Drop(item, info.Drop, info.Position);
            }
            else
                DragDropManager.DragDrop(item, info.Drop);
        }

        private class DragDropInfo
        {
            private readonly int requested;
            public Serial Drag { get; private set; }
            public Serial Drop { get; private set; }
            public Layer Layer { get; private set; }
            public Point3D Position { get; private set; }

            private DragDropInfo(Serial drag)
            {
                requested = Environment.TickCount;
                Drag = drag;
                items.RemoveAll(i => Environment.TickCount - i.requested > 60000 || i.Drag == drag);
            }

            public DragDropInfo(Serial drag, Layer layer)
                : this(drag)
            {
                Drag = drag;
                Layer = layer;
            }

            public DragDropInfo(Serial drag, Serial drop) : this(drag, drop, Point3D.Zero) { }
            public DragDropInfo(Serial drag, Serial drop, Point3D position)
                : this(drag)
            {
                Drag = drag;
                Drop = drop;
                Position = position;
            }
        }
    }
}