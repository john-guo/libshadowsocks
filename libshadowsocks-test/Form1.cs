using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shadowsocks;
using libshadowsocks_test.sites;

namespace libshadowsocks_test
{
    public partial class Form1 : Form
    {
        string title;
        int count = 0;
        public Form1()
        {
            InitializeComponent();
            title = this.Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (var l in listeners)
            {
                l.Stop();
            }

            listeners.Clear();
            listView1.Groups.Clear();
            listView1.Items.Clear();

            foreach (var site in sites)
            {
                var servers = site.GetServers();


                var grp = new ListViewGroup(site.GetType().Name);
                listView1.Groups.Add(grp);

                foreach (var svc in servers)
                {
                    var item = new ListViewItem(new string[] { svc.server, svc.server_port.ToString(), svc.password, svc.method }, grp);
                    item.Tag = null;
                    listView1.Items.Add(item);
                }
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                button2.Enabled = false;
                return;
            }

            var item = listView1.SelectedItems[0];
            if (item.Tag == null)
            {
                button2.Text = "Start";
            }
            else
            {
                button2.Text = "Stop";
            }

            textBox1.Text = item.SubItems[0].Text;
            textBox2.Text = item.SubItems[1].Text;
            textBox3.Text = item.SubItems[2].Text;
            textBox4.Text = item.SubItems[3].Text;
            button2.Enabled = true;
        }

        private List<Listener> listeners = new List<Listener>();
        private int lastSelected = 0;
        private void button2_Click(object sender, EventArgs e)
        {
            var item = listView1.SelectedItems[0];
            lastSelected = listView1.SelectedIndices[0];

            if (item.Tag != null)
            {
                var ss = item.Tag as Listener;
                ss.Stop();
                listeners.Remove(ss);

                item.Tag = null;
                item.BackColor = SystemColors.Window;
                button2.Text = "Start";
            }
            else
            {
                var ss = new Listener();
                ss.OnBroken += ss_Broken;
                try
                {
                    ss.Start(textBox1.Text,
                        Int32.Parse(textBox2.Text),
                        textBox3.Text,
                        textBox4.Text,
                        Int32.Parse(textBox5.Text),
                        checkBox1.Checked);
                }
                catch (SSPortAlreadyInUseException)
                {
                    return;
                }
                listeners.Add(ss);

                this.Text = String.Format("{0}({1})", title, ++count);

                item.SubItems.AddRange(new string[] { textBox5.Text, checkBox1.Checked ? "Y" : "N" });
                item.Tag = ss;
                item.BackColor = Color.LightGreen;
                button2.Text = "Stop";
            }
        }

        void ss_Broken()
        {
            Action invoker = () =>
            {
                try
                {
                    button1.PerformClick();
                    if (listView1.Items.Count <= 0)
                    {
                        Task.Delay(3000).ContinueWith(t => ss_Broken());
                        return;
                    }
                    if (listView1.Items.Count <= lastSelected)
                        lastSelected = 0;

                    listView1.Items[lastSelected].Selected = true;
                    button2.PerformClick();
                }
                catch
                {
                    Task.Delay(3000).ContinueWith(t => ss_Broken());
                }
            };


            if (InvokeRequired)
                this.Invoke(invoker);
            else
                invoker();
        }


        IList<ISSSite> sites;
        private void Form1_Load(object sender, EventArgs e)
        {
            Logging.OpenLogFile();

            var path = "sites";
            var dll = new[] 
            { 
                "libshadowsocks.dll",
                "HtmlAgilityPack.dll", 
                "libshadowsocks-test.exe",
            };
            /*
            var references = ConfigurationManager.AppSettings["references"];
            if (!String.IsNullOrWhiteSpace(references))
            {
                dll = dll.Union(references.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)).ToArray();
            }
            */
            var assembly = AssemblyHelper.GetAssembly(dll, path);

            sites = AssemblyHelper.GetObjects<ISSSite, SSAttribute>(assembly);

            ss_Broken();
        }
    }
}
