using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using WindowsDesktop;

namespace Virtual_Desktop_Manager
{
    public partial class MainForm : Form
    {
        private KeyHandler[] ghks = new KeyHandler[10];
        private VirtualDesktop[] virtualDesktops;
        private bool activated = false;

        public MainForm()
        {
            InitializeComponent();
            EnableButton.BackColor = Color.Red;

			InitializeComObjects();
            for (int i = 0; i < 10; i++)
                ghks[i] = new KeyHandler(Constants.WIN + Constants.CTRL + Constants.ALT, Keys.D0 + i, this);
            InitializeKeyHandlers();

			RefreshVirtualDesktops();
		}

		private static async void InitializeComObjects()
		{
			try
			{
				await VirtualDesktopProvider.Default.Initialize(TaskScheduler.FromCurrentSynchronizationContext());
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Failed to initialize.");
			}

			VirtualDesktop.CurrentChanged += (sender, args) => System.Diagnostics.Debug.WriteLine($"Desktop changed: {args.NewDesktop.Id}");
		}

        private void InitializeKeyHandlers()
        {
            for (int i = 0; i < 10; i++)
                ghks[i].Register();
            EnableButton.BackColor = Color.Green;
            EnableButton.Text = "Disable";
            activated = true;
        }

        private void RemoveAllKeyHandlers()
        {
            for (int i = 0; i < ghks.Length; i++)
                if (!ghks[i].Unregiser())
                    MessageBox.Show("Hotkey failed to unregister!");
            EnableButton.BackColor = Color.Red;
            EnableButton.Text = "Enable";
            activated = false;
        }

        private void HandleHotkey(Keys key, int modifier)
        {
            if (key == Keys.D0)
                virtualDesktops.Last().Switch();
            else if (key - Keys.D1 < virtualDesktops.Length)
                virtualDesktops[key - Keys.D1].Switch();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Constants.WM_HOTKEY_MSG_ID)
            {
                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                int modifier = (int)m.LParam & 0xFFFF;
                HandleHotkey(key, modifier);
            }
            
            base.WndProc(ref m);
        }

        private void RefreshVirtualDesktops()
        {
			virtualDesktops = VirtualDesktop.GetDesktops();
			virtualDesktopCountLabel.Text = virtualDesktops.Length.ToString();
		}

        private void virtualDesktopCountLabel_Click(object sender, EventArgs e)
        {
			RefreshVirtualDesktops();
		}

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.Visible = true;
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
			RefreshVirtualDesktops();
		}

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            RemoveAllKeyHandlers();
        }

        private void EnableButton_Click(object sender, EventArgs e)
        {
            if (activated)
                RemoveAllKeyHandlers();
            else
                InitializeKeyHandlers();
        }
    }

    public class KeyHandler
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private int modifier;
        private int key;
        private IntPtr hWnd;
        private int id;

        public KeyHandler(int modifier, Keys key, Form form)
        {
            this.modifier = modifier;
            this.key = (int)key;
            this.hWnd = form.Handle;
            id = this.GetHashCode();
        }

        public override int GetHashCode()
        {
            return modifier ^ key ^ hWnd.ToInt32();
        }

        public bool Register()
        {
            return RegisterHotKey(hWnd, id, modifier, key);
        }

        public bool Unregiser()
        {
            return UnregisterHotKey(hWnd, id);
        }
    }

    public static class Constants
	{
        // modifiers
        public const int NOMOD = 0x0000;
        public const int ALT = 0x0001;
        public const int CTRL = 0x0002;
        public const int SHIFT = 0x0004;
        public const int WIN = 0x0008;

        // windows message id for hotkey
        public const int WM_HOTKEY_MSG_ID = 0x0312;
	}
}