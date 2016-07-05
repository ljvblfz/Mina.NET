using System;
using System.Net;
using System.Windows.Forms;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Filter.Codec;
using Mina.Filter.Codec.TextLine;
using Mina.Filter.Logging;
using Mina.Filter.Ssl;
using Mina.Transport.Socket;

namespace Mina.Example.Chat.Client
{
    public partial class FormChat : Form
    {
        IOConnector _connector = new AsyncSocketConnector();
        IOSession _session;

        public FormChat()
        {
            InitializeComponent();

            textBoxUser.Text = "user" + Math.Round(new Random().NextDouble() * 10);

            _connector.FilterChain.AddLast("logger", new LoggingFilter());
            _connector.FilterChain.AddLast("codec", new ProtocolCodecFilter(new TextLineCodecFactory()));

            _connector.SessionClosed += (o, e) => Append("Connection closed.");
            _connector.MessageReceived += OnMessageReceived;

            SetState(false);
        }

        private void OnMessageReceived(object sender, IoSessionMessageEventArgs e)
        {
            var theMessage = (string)e.Message;
            var result = theMessage.Split(new char[] { ' ' }, 3);
            var status = result[1];
            var theCommand = result[0];

            if ("OK".Equals(status))
            {
                if (string.Equals("BROADCAST", theCommand, StringComparison.OrdinalIgnoreCase))
                {
                    if (result.Length == 3)
                        Append(result[2]);
                }
                else if (string.Equals("LOGIN", theCommand, StringComparison.OrdinalIgnoreCase))
                {
                    SetState(true);
                    Append("You have joined the chat session.");
                }
                else if (string.Equals("QUIT", theCommand, StringComparison.OrdinalIgnoreCase))
                {
                    SetState(false);
                    Append("You have left the chat session.");
                }
            }
            else
            {
                if (result.Length == 3)
                {
                    MessageBox.Show(result[2]);
                }
            }
        }

        private void SetState(bool loggedIn)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(SetState), loggedIn);
                return;
            }
            buttonConnect.Enabled = textBoxUser.Enabled = textBoxServer.Enabled = !loggedIn;
            buttonDisconnect.Enabled = buttonSend.Enabled = buttonQuit.Enabled = textBoxChat.Enabled = textBoxInput.Enabled = loggedIn;
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            var server = textBoxServer.Text;
            if (string.IsNullOrEmpty(server))
                return;

            if (checkBoxSSL.Checked)
            {
                if (!_connector.FilterChain.Contains("ssl"))
                    _connector.FilterChain.AddFirst("ssl", new SslFilter("TempCert", null));
            }
            else if (_connector.FilterChain.Contains("ssl"))
            {
                _connector.FilterChain.Remove("ssl");
            }

            IPEndPoint ep;
            var parts = server.Trim().Split(':');
            if (parts.Length > 0)
            {
                ep = new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
            }
            else
            {
                ep = new IPEndPoint(IPAddress.Loopback, int.Parse(parts[0]));
            }

            var future = _connector.Connect(ep).Await();

            if (future.Connected)
            {
                _session = future.Session;
                _session.Write("LOGIN " + textBoxUser.Text);
            }
            else
            {
                MessageBox.Show("Could not connect to " + server);
            }
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            Quit();
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            Broadcast(textBoxInput.Text);
        }

        private void buttonQuit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void FormChat_FormClosing(object sender, FormClosingEventArgs e)
        {
            _connector.Dispose();
        }

        public void Broadcast(string message)
        {
            if (_session != null)
                _session.Write("BROADCAST " + message);
        }

        public void Quit()
        {
            if (_session != null)
            {
                _session.Write("QUIT");
                // session will be closed by the server
                _session = null;
            }
        }

        public void Append(string line)
        {
            if (textBoxChat.InvokeRequired)
            {
                textBoxChat.Invoke(new Action<string>(Append), line);
                return;
            }

            textBoxChat.AppendText(line);
            textBoxChat.AppendText(Environment.NewLine);
        }
    }
}
