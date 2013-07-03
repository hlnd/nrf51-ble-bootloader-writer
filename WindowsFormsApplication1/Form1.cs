using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

using Nordicsemi;

namespace BootloaderWriter
{

    public partial class Form1 : Form
    {
        MasterEmulatorController mec = new MasterEmulatorController();        

        public Form1()
        {
            InitializeComponent();

            mec.DeviceDiscovered += DeviceDiscovered;
            mec.Connected += Connected;
            mec.Disconnected += Disconnected;
            mec.ResponseReceived += ResponseReceived;
            mec.LogMessage += LogMessage;

            selectFileDialog.FileOk += delegate
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    selectedFilePath.Text = this.selectFileDialog.FileName;
                });
            };

            foreach (var emulator in mec.GetEmulators())
                emulatorBox.Items.Add(emulator);

            emulatorBox.SelectedIndex = 0;

            if (emulatorBox.Items.Count == 1)
                openButton_Click(this, null);
            
        }

        private void DeviceDiscovered(object sender, BtDevice dev)
        {

            BeginInvoke((MethodInvoker) delegate()
            {
                logBox.AppendText("New device found.\n");
                deviceBox.Items.Add(dev);
            });
        }

        private void Connected(object sender, BtDevice dev)
        {
            BeginInvoke((MethodInvoker) delegate()
            {
                logBox.AppendText("Connected.\n");
                startButton.Enabled = true;
                connectButton.Enabled = true;
                connectButton.Text = "Disconnect";
            });
        }

        private void Disconnected(object sender, BtDevice dev)
        {
            BeginInvoke((MethodInvoker)delegate()
            {
                startButton.Enabled = false;
                connectButton.Enabled = true;
                connectButton.Text = "Connect";
            });
        }

        private void LogMessage(object sender, string message)
        {
            BeginInvoke((MethodInvoker)delegate()
            {
                logBox.AppendText(message+"\n");
            });
        }

        private void ResponseReceived(object sender, byte error)
        {
            BeginInvoke((MethodInvoker)delegate()
            {
                logBox.AppendText(String.Format("Received response: {0}\n", error));
            });
        }

        private void WriteFailure(object sender, HexFileController.HexFileControllerStates state, byte error, int line)
        {
            
        }

        private void selectFileButton_Click(object sender, EventArgs e)
        {
            selectFileDialog.ShowDialog();
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            if (mec.IsOpen)
            {
                openButton.Text = "Open";
                mec.CloseEmulator();

                connectButton.Enabled = false;
                startButton.Enabled = false;
            }
            else
            {
                openButton.Text = "Close";
                mec.OpenEmulator((string) emulatorBox.SelectedItem);

                connectButton.Enabled = true;

                mec.StartScan();
            }

        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            if (mec.IsConnected)
            {
                mec.Disconnect();
                connectButton.Text = "Disconnecting...";
                connectButton.Enabled = false;
            }
            else
            {
                mec.Connect((BtDevice) deviceBox.SelectedItem);
                connectButton.Text = "Connecting...";
                connectButton.Enabled = false;
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            connectButton.Enabled = false;
            startButton.Enabled = false;
            selectFileButton.Enabled = false;

            HexFileController hfc = new HexFileController(mec, selectedFilePath.Text);
            hfc.Failure += (s, state, error, line) =>
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    this.logBox.AppendText(String.Format("Failed in state {0}, with error {1}, on line {2}", state, error, line));
                });
            };
            

            hfc.Progress += (s, progress) =>
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    progressBar.Value = progress;
                });
            };
            hfc.Finished += (s) =>
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    logBox.AppendText("Finished\n");
                    connectButton.Enabled = true;
                    startButton.Enabled = true;
                    selectFileButton.Enabled = true;
                });
            };
            hfc.StartWriting();
        }

    }
}
