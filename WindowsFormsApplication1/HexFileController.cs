using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Nordicsemi;
using BootloaderWriter;

namespace BootloaderWriter
{
    class HexFileController
    {
        
        public delegate void FailureHandler(object sender, HexFileControllerStates state, byte error, int line);
        public event FailureHandler Failure;

        public delegate void ProgressHandler(object sender, int percent);
        public event ProgressHandler Progress;

        public delegate void FinishedHandler(object sender);
        public event FinishedHandler Finished;

        private MasterEmulatorController mec;
        private string[] hexLines;
        private int lineCounter = 0;
        public enum HexFileControllerStates 
        {
            Idle,
            Starting,
            Erasing,
            Writing,
            Resetting,
            Finished
        }
        private HexFileControllerStates state = HexFileControllerStates.Idle;

        public HexFileController(MasterEmulatorController mec, string hexFilePath)
        {
            this.mec = mec;
            this.mec.ResponseReceived += onResponseReceived;

            hexLines = File.ReadAllLines(hexFilePath);
        }

        public void StartWriting()
        {
            onResponseReceived(this, (byte) 0x00);
        }

        private void onDisconnected(object sender, BtDevice dev)
        {
            mec.Connect(dev);
        }

        private void onConnected(object sender, BtDevice dev)
        {
            onResponseReceived(this, (byte) 0x00);
            this.mec.Connected -= onConnected;
        }

        private void onResponseReceived(object sender, byte error)
        {
            if (error != 0)
            {
                Failure(this, state, error, lineCounter);
                Finished(this);
                this.mec.ResponseReceived -= onResponseReceived;
                return;
            }

            double progress = 100*lineCounter/(double) hexLines.Count();
            Progress(this, (int) Math.Round(progress));

            switch (state)
            {
                case HexFileControllerStates.Idle:
                    mec.WriteCommand(MasterEmulatorControllerCommand.Nop);
                    state = HexFileControllerStates.Starting;
                    break;

                case HexFileControllerStates.Starting:
                    this.mec.Disconnected += onDisconnected;
                    this.mec.Connected += onConnected;

                    mec.WriteCommand(MasterEmulatorControllerCommand.EraseApp);
                    state = HexFileControllerStates.Erasing;
                    break;

                case HexFileControllerStates.Erasing:
                    this.mec.Disconnected -= onDisconnected;
                    this.mec.Connected -= onConnected;

                    mec.WriteLine(hexLines[lineCounter++]);
                    state = HexFileControllerStates.Writing;
                    break;

                case HexFileControllerStates.Writing:
                    if (lineCounter < hexLines.Length)
                    {
                        mec.WriteLine(hexLines[lineCounter++]);
                    }
                    else
                    {
                        mec.WriteCommand(MasterEmulatorControllerCommand.ResetAndRun);
                        state = HexFileControllerStates.Resetting;
                    }
                    break;

                case HexFileControllerStates.Resetting:
                    this.mec.ResponseReceived -= onResponseReceived;
                    state = HexFileControllerStates.Idle;
                    Finished(this);
                    break;
            }
        }
    }
}
