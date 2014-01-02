using Assistant;
using System;
using System.Windows.Forms;

namespace RazorEx.Fixes
{
    public static class Dressing
    {
        //private static readonly ConcurrentDictionary<Item, bool> actions = new ConcurrentDictionary<Item, bool>();

        public static void OnInit()
        {
            DressList.OnAdd = DressList_OnAdd;
            //PacketHandler.RemoveServerToClientViewer(0x27, (PacketViewerCallback)Delegate.CreateDelegate(typeof(PacketViewerCallback), typeof(PacketHandlers), "LiftReject"));
            // PacketHandler.RegisterServerToClientViewer(0x27, LiftReject);

            Command.Register("dress", args => Dress());
            Core.AddHotkey("Dress", Dress);
            Engine.MainWindow.dressNow.Click += button_Click;
        }

        private static void DressList_OnAdd()
        {
            TreeNode dressNode = HotKey.FindParent(HKCategory.Dress, HKSubCat.None);
            foreach (TreeNode listNode in dressNode.Nodes)
                if (listNode.Text.StartsWith("Dress:"))
                {
                    DressList list = ((KeyData)listNode.Tag).m_Callback.Target as DressList;
                    if (list != null)
                        ((KeyData)listNode.Tag).m_Callback = () => Dress(list);
                }
        }

        private static void Dress() { Targeting.OneTimeTarget((l, s, p, g) => Dress(s)); }

        private static void button_Click(object sender, EventArgs e)
        {
            DressList selectedItem = Engine.MainWindow.dressList.SelectedItem as DressList;
            if (selectedItem != null && World.Player != null)
                Dress(selectedItem);
        }

        /* private static void LiftReject(PacketReader p, PacketHandlerEventArgs args)
         {
             Item item = World.FindItem(DragDropManager.m_Holding);
             if (!DragDropManager.LiftReject())
             {
                 args.Block = true;
                 bool dress;
                 if (item != null && actions.TryGetValue(item, out dress))
                     if (dress)
                         DragDropManager.DragDrop(item, World.Player, GetLayer(item));
                     else
                         DragDropManager.DragDrop(item, FindBag(item));
             }
         }*/

        private static void Dress(DressList list)
        {
            //actions.Clear();
            if (list.Name.StartsWith("_"))
                foreach (DressList dressList in DressList.m_List)
                    if (dressList.Name == "base")
                        foreach (Serial item in dressList.Items)
                            DressInternal(item);
            foreach (Serial item in list.Items)
                DressInternal(item);
        }

        private static void Dress(Serial serial)
        {
            //actions.Clear();
            DressInternal(serial);
        }

        private static void DressInternal(Serial serial)
        {
            Item item = World.FindItem(serial);
            if (item == null || (item.IsInBank && !Bank.Opened))
                return;
            Layer layer = GetLayer(item);
            Item original = World.Player.GetItemOnLayer(layer);

            if (original != null)
                if (original.Serial == item.Serial)
                    return;
                else
                    Undress(original);

            if (layer == Layer.LeftHand && !item.IsShield())
            {
                original = World.Player.GetItemOnLayer(Layer.FirstValid);
                if (original != null)
                    Undress(original);
            }

            if (layer == Layer.FirstValid)
            {
                original = World.Player.GetItemOnLayer(Layer.LeftHand);
                if (original != null && !original.IsShield())
                    Undress(original);
            }
            Dress(item);
        }

        private static void Undress(Item item)
        {
            //if (actions.TryAdd(item, false))
            DragDrop.Move(item, FindBag(item));
        }

        private static void Dress(Item item)
        {
            //if (actions.TryAdd(item, true))
            DragDrop.Dress(item, GetLayer(item));
        }

        private static Layer GetLayer(Item item)
        {
            if (item.IsQuiver())
                return Layer.MiddleTorso;
            if (item.ItemID == 0x2D25)
                return Layer.LeftHand;
            return (Layer)item.ItemID.ItemData.Quality;
        }

        private static Item FindBag(UOEntity item)
        {
            if (item != null)
                foreach (DressList list in DressList.m_List)
                    if (list.Items.Contains(item.Serial))
                    {
                        Item bag = World.FindItem(list.m_UndressBag);
                        if (bag != null && bag.RootContainer == World.Player)
                            return bag;
                    }
            return World.Player.Backpack;
        }
    }
}