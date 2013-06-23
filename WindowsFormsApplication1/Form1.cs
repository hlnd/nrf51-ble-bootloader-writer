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
            mec.ResponseReceived += ResponseReceived;
            mec.LogMessage += LogMessage;

            foreach (var emulator in mec.GetEmulators())
                emulatorBox.Items.Add(emulator);

            emulatorBox.SelectedIndex = 0;

            if (emulatorBox.Items.Count == 1)
                openButton_Click(this, null);
            
        }

        private void DeviceDiscovered(object sender, BtDevice dev)
        {

            logBox.BeginInvoke((MethodInvoker) delegate()
            {
                logBox.AppendText("New device found.\n");
                deviceBox.Items.Add(dev);
            });
        }

        private void LogMessage(object sender, string message)
        {
            BeginInvoke((MethodInvoker)delegate()
            {
                logBox.AppendText(message+"\n");
            });
        }

        private void ResponseReceived(object sender, bool success)
        {
            BeginInvoke((MethodInvoker)delegate()
            {
                logBox.AppendText(String.Format("Received response: {0}\n", success));
            });
        }

        private void WriteFailure(object sender, HexFileController.HexFileControllerStates state, int line)
        {
            BeginInvoke((MethodInvoker)delegate()
            {
                this.logBox.AppendText(String.Format("Failed in state {0}, line {1}", state, line));
            });
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
            }
            else
            {
                openButton.Text = "Close";
                mec.OpenEmulator((string) emulatorBox.SelectedItem);

                mec.StartScan();
            }

        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            if (mec.IsConnected)
            {
                mec.Disconnect();
                connectButton.Text = "Connect";
                
            }
            else
            {
                mec.Connect((BtDevice) deviceBox.SelectedItem);
                connectButton.Text = "Disconnect";
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            HexFileController hfc = new HexFileController(mec, selectFileDialog.FileName);
            hfc.Failure += WriteFailure;
            hfc.StartWriting();
            
            
                //masterEmulator.SendData(commandPipe, new byte[] {0x01});
                /*
                foreach (string trimmedLine in File.ReadLines(selectFileDialog.FileName))
                {
                    string line = trimmedLine+"\n";

                    int remaining = line.Length;
                    for (int i = 0; i < line.Length; i += 20, remaining -= 20)
                    {
                        
                        byte[] buffer = System.Text.Encoding.ASCII.GetBytes(line.Substring(i, (remaining > 20 ? 20 : remaining)));

                        try
                        {
                            masterEmulator.SendData(dataPipe, buffer);
                        }
                        catch (Exception exception)
                        {
                            logBox.AppendText("Something failed...");
                            return;
                        }
                    }
                    System.Threading.Thread.Sleep(50);
                    
                }
                logBox.AppendText("Finished sending.");*/
            
        }

    }
}
