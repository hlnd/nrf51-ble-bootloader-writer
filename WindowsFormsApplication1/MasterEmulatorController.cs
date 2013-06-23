using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;

using Nordicsemi;

namespace BootloaderWriter
{
    public enum MasterEmulatorControllerCommand : byte
    {
        Nop,
        EraseApp,
        WriteLine,
        ResetAndRun,
    }
        
    class MasterEmulatorController
    {
        private Nordicsemi.MasterEmulator masterEmulator = null;
        private BtDevice connectedDevice = null;
        private List<BtDeviceAddress> discoveredAddressList = new List<BtDeviceAddress>();

        public delegate void DeviceDiscoveredHandler(object sender, BtDevice device);
        public event DeviceDiscoveredHandler DeviceDiscovered;
        public delegate void LogMessageHandler(object sender, string message);
        public event LogMessageHandler LogMessage;
        public delegate void ResponseHandler(object sender, bool success);
        public event ResponseHandler ResponseReceived;

        public bool IsOpen
        {
            get { return masterEmulator.IsOpen; }
        }
        public bool IsConnected
        {
            get { return masterEmulator.IsConnected; }
        }

        private int commandPipe;
        private int responsePipe;
        private int dataPipe;

        public MasterEmulatorController()
        {
            masterEmulator = new MasterEmulator();
        }

        public List<String> GetEmulators()
        {
            return masterEmulator.EnumerateUsb().ToList();
        }

        private void setupPipes()
        {
            String baseUUID = "000000001212EFDE1523785FEABC9844";

            masterEmulator.SetupAddService(new BtUuid(0x1000, baseUUID), PipeStore.Remote);

            masterEmulator.SetupAddCharacteristicDefinition(new BtUuid(0x2000, baseUUID), 1, new byte[] { 0x00 });
            commandPipe = masterEmulator.SetupAssignPipe(PipeType.TransmitWithAck);

            masterEmulator.SetupAddCharacteristicDefinition(new BtUuid(0x2001, baseUUID), 1, null);
            responsePipe = masterEmulator.SetupAssignPipe(PipeType.Receive);

            masterEmulator.SetupAddCharacteristicDefinition(new BtUuid(0x2002, baseUUID), 1, null);
            dataPipe = masterEmulator.SetupAssignPipe(PipeType.Transmit);

        }

        public void OpenEmulator(string emulator)
        {
            masterEmulator.LogVerbosity = Verbosity.High;
            masterEmulator.Open(emulator);

            masterEmulator.DeviceDiscovered += onDeviceDiscovered;
            masterEmulator.Connected += onDeviceConnected;
            masterEmulator.Disconnected += onDeviceDisconnected;
            masterEmulator.DataReceived += onDataReceived;
            masterEmulator.LogMessage += onLogMessage;

            setupPipes();

            masterEmulator.Run();
        }

        public void CloseEmulator()
        {
            masterEmulator.Close();
        }

        public void StartScan()
        {
            BtScanParameters scan = new BtScanParameters();
            scan.ScanType = BtScanType.ActiveScanning;
            masterEmulator.StartDeviceDiscovery(scan);
        }

        public void Connect(BtDevice device)
        {
            if (masterEmulator.IsDeviceDiscoveryOngoing)
                masterEmulator.StopDeviceDiscovery();

            BtConnectionParameters connParams = new BtConnectionParameters();
            connParams.ConnectionIntervalMs = 10;
            connParams.SlaveLatency = 0;
            connParams.SupervisionTimeoutMs = 1000;
            
            connectedDevice = device;
            
            masterEmulator.Connect(device.DeviceAddress, connParams);
        }

        public bool WriteCommand(MasterEmulatorControllerCommand command)
        {
            byte[] buffer = new byte[] { (byte)command };
            return masterEmulator.SendData(commandPipe, buffer);
        }

        public void WriteLine(string line)
        {
            line += "\n";

            int remaining = line.Length;
            for (int i = 0; i < line.Length; i += 20, remaining -= 20)
            {
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes(line.Substring(i, (remaining > 20 ? 20 : remaining)));
                masterEmulator.SendData(this.dataPipe, buffer);
            }
        }

        public void Disconnect()
        {
            if (masterEmulator.IsConnected)
                masterEmulator.Disconnect();
        }

        private void onDeviceDiscovered(object sender, ValueEventArgs<BtDevice> args)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, a) =>
            {
                BtDevice dev = (BtDevice) a.Argument as BtDevice;
                
                if (!discoveredAddressList.Contains(dev.DeviceAddress))
                {
                    discoveredAddressList.Add(dev.DeviceAddress);
                    DeviceDiscovered(this, dev);
                }
            };

            bw.RunWorkerAsync(args.Value); 
        }

        private void onDeviceConnected(object sender, EventArgs e)
        {
            BackgroundWorker bw = new BackgroundWorker();
            
            bw.DoWork += delegate 
            {
                masterEmulator.DiscoverPipes();

                masterEmulator.OpenAllRemotePipes();
            };

            bw.RunWorkerAsync();
        }

        private void onDeviceDisconnected(object setnder, EventArgs e)
        {
            BackgroundWorker bw = new BackgroundWorker();

            bw.DoWork += delegate
            {
                this.Connect(connectedDevice);
            };

            bw.RunWorkerAsync();
        }

        private void onLogMessage(object sender, ValueEventArgs<string> args)
        {
            BackgroundWorker bw = new BackgroundWorker();

            bw.DoWork += delegate
            {
                LogMessage(this, args.Value);
            };
            bw.RunWorkerAsync();
        }

        public void onDataReceived(object sender, PipeDataEventArgs e)
        {
            BackgroundWorker bw = new BackgroundWorker();

            bw.DoWork += delegate
            {
                ResponseReceived(this, e.PipeData[0] == 0x00);
            };
            bw.RunWorkerAsync(e);
        }
    }
}
