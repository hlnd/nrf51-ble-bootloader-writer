nrf51-ble-bootloader-writer
===========================

A Windows C# application based on the Master Emulator API to write a firmware to an nRF51822 running the 
nrf51-ble-bootloader application. 

To compile and use this application, you need to have the Master Emulator DLL, which can be downloaded from 
www.nordicsemi.com. It must be added as an external reference to the project. 

This application is proof-of-concept quality, and does have known issues if you do strange things. Any hex files written 
must have the same base address set as used in the bootloader (I've been using 0x20000).


