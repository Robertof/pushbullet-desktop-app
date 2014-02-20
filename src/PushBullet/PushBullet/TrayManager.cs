using System;
using System.Windows.Forms;

namespace PushBullet
{
    class TrayManager
    {
        private NotifyIcon icn = new NotifyIcon();
        private ContextMenu ctx = new ContextMenu();
        private MenuItem selectedItem;

        public TrayManager(Robertof.PushBulletAPI.PushBulletAPI.DevicesResponse response)
        {
            using (System.IO.Stream icnStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Images/pb_ubersmall.ico")).Stream)
                icn.Icon = new System.Drawing.Icon(icnStream);
            if (response.devices.Length > 0)
            {
                ctx.MenuItems.Add("Your devices").Enabled = false;
                ctx.MenuItems.Add("-").Enabled = false;
                for (int i = 0; i < response.devices.Length; i++)
                    ctx.MenuItems.Add(response.devices[i].extras.model, CtxItemClicked);
            }
            if (response.shared_devices.Length > 0)
            {
                if (response.devices.Length > 0)
                    ctx.MenuItems.Add("-").Enabled = false;
                ctx.MenuItems.Add("Shared devices").Enabled = false;
                ctx.MenuItems.Add("-").Enabled = false;
                for (int i = 0; i < response.shared_devices.Length; i++)
                    ctx.MenuItems.Add(response.shared_devices[i].extras.model + " (" + response.shared_devices[i].owner_name + ")", CtxItemClicked);
            }
            if (ctx.MenuItems.Count > 0)
            {
                ctx.MenuItems[2].Checked = true;
                this.selectedItem = ctx.MenuItems[2];
                ctx.MenuItems.Add("-");
            }
            ctx.MenuItems.Add("&Configure APIKey", CtxItemClicked);
            ctx.MenuItems.Add("E&xit", CtxItemClicked);
            icn.Visible = true;
            icn.Text = "PushBullet";
            icn.ContextMenu = ctx;
        }

        private void CtxItemClicked(object sender, EventArgs e)
        {
            var elm = sender as MenuItem;
            switch (elm.Text)
            {
                case "E&xit":
                    ctx.Dispose();
                    icn.Dispose();
                    //PushBullet.GetInstance().app.Shutdown();
                    break;
                case "&Configure APIKey":
                    ctx.Dispose();
                    icn.Dispose();
                    new APIGUI2().Show();
                    break;
                default:
                    if (elm != selectedItem)
                    {
                        elm.Checked = !elm.Checked;
                        selectedItem.Checked = false;
                        selectedItem = elm;
                    }
                    break;
            }
        }
    }
}
