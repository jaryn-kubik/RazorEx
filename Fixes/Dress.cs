using Assistant;
using System;
using System.Windows.Forms;

namespace RazorEx.Fixes
{
    public static class Dressing
    {
        public static void OnInit()
        {
            DressList.OnAdd = DressList_OnAdd;

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

        private static void Dress(DressList list)
        {
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
            DressInternal(serial);
        }

        private static void DressInternal(Serial serial)
        {
            Item item = World.FindItem(serial);
            if (item == null || (item.IsInBank && !Bank.Opened))
                return;
            Layer layer = GetLayer(item);
            Item original = World.Player.GetItemOnLayer(layer);

            if (PositionCheck.InMages && !IsAllowedInMages(item, layer))
            {
                WorldEx.SendMessage("Dress blocked.");
                return;
            }

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
            DragDrop.Move(item, FindBag(item));
        }

        private static void Dress(Item item)
        {
            DragDrop.Dress(item, GetLayer(item));
        }

        private static Layer GetLayer(Item item)
        {
            if (item.IsQuiver())
                return Layer.MiddleTorso;
            if (item.ItemID == 0x2D25 || item.ItemID == 0x0F4F || item.ItemID == 0x0F50 || item.ItemID == 0x26CE ||
                item.ItemID == 0x13FC || item.ItemID == 0x13FD)
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

        private static bool IsAllowedInMages(Item item, Layer layer)
        {
            if (layer == Layer.LeftHand)
                return item.ItemID == 0x2D25 || item.ItemID == 0x0DF0 || item.IsShield();
            if (layer == Layer.FirstValid)
                return item.ItemID == 0x0DF4 || item.ItemID == 0x2253 || item.ItemID == 0x0EFA || item.ItemID == 0x2D50;
            return true;
        }
    }
}