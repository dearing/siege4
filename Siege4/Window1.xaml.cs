using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Siege4
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        Action<String> SetText;
        public Classes.Network network = new Siege4.Classes.Network("localhost", "4000");

        public Window1()
        {
            InitializeComponent();

            network.ConnectionChanged += new EventHandler<Siege4.Classes.Network.MudConnectionArgs>(network_ConnectionChanged);
            network.MessageReceived += new EventHandler<Siege4.Classes.Network.MessageReceivedArgs>(network_MessageReceived);
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(network.Connect));

        }

        void network_MessageReceived(object sender, Siege4.Classes.Network.MessageReceivedArgs e)
        {
            SetText = s => mudbox.richTextBox1.AppendText(s);
            if (mudbox.richTextBox1.Dispatcher.CheckAccess())
                SetText(e.Message);
            else
                Dispatcher.Invoke(SetText, System.Windows.Threading.DispatcherPriority.Normal, new object[] { e.Message });
        }

        void network_ConnectionChanged(object sender, Siege4.Classes.Network.MudConnectionArgs e)
        {
            SetText = s => mudbox.richTextBox1.AppendText(s);
            if (mudbox.richTextBox1.Dispatcher.CheckAccess())
                SetText(String.Format("Network: {0}{1}", e.Connection_State, e.Message));
            else
                Dispatcher.Invoke(SetText, System.Windows.Threading.DispatcherPriority.Normal, new object[] { String.Format("Network: {0}{1}", e.Connection_State, e.Message) });
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                network.Input = UserInput.Text +Environment.NewLine;
                mudbox.richTextBox1.AppendText("<< " + UserInput.Text + Environment.NewLine);
                UserInput.Text = String.Empty;
            }
        }
    }
}
