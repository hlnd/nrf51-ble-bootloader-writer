using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using BootloaderWriter;

namespace BootloaderWriter
{
    class HexFileController
    {
        
        public delegate void FailureHandler(object sender, HexFileControllerStates state, int line);
        public event FailureHandler Failure;

        private MasterEmulatorController mec;
        private string[] hexLines;
        private int lineCounter = 0;
        public enum HexFileControllerStates 
        {
            Init,
            Idle,
            Erasing,
            Writing,
            Resetting,
            Finished
        }
        private HexFileControllerStates state = HexFileControllerStates.Init;

        public HexFileController(MasterEmulatorController mec, string hexFilePath)
        {
            this.mec = mec;
            this.mec.ResponseReceived += onResponseReceived;

            hexLines = File.ReadAllLines(hexFilePath);
        }

        public void StartWriting()
        {
            onResponseReceived(this, true);
        }

        private void onResponseReceived(object sender, bool success)
        {
            if (!success)
            {
                Failure(this, state, lineCounter-1);
                return;
            }

            switch (state)
            {
                case HexFileControllerStates.Init:
                    mec.WriteCommand(MasterEmulatorControllerCommand.Nop);
                    state = HexFileControllerStates.Erasing;
                    break;

                case HexFileControllerStates.Idle:
                    mec.WriteCommand(MasterEmulatorControllerCommand.EraseApp);
                    state = HexFileControllerStates.Erasing;
                    break;

                case HexFileControllerStates.Erasing:
                    mec.WriteLine(hexLines[lineCounter++]);
                    state = HexFileControllerStates.Writing;
                    break;

                case HexFileControllerStates.Writing:
                    if (lineCounter != hexLines.Length-1)
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
                    state = HexFileControllerStates.Idle;
                    break;
            }
        }
    }
}
