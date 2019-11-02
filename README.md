# PyOLab 

## Overview

Pyolab3 is a suite of Python 3 routines that gives users complete control of an IOLab system. The archive contains a folder containing the PyOLab library code (__PyOLabCode__), four folders containing example user code (__HelloWorld__, __DaqExample__, __guiExample__, and __AnalogExample__), and a folder containing Documentation referred to in the code (__Documentation__). 

There are detailed instructions for __Getting Started__ at the bottom of this page.

## PyOLabCode 

This folder contains a collection of methods to allow the user to open a serial port, send commands to the IOLab hardware, and receive both status information and asynchronous data from the hardware:

* __commMethods.py__ 
Communication with the IOLab hardware via the USB virtual com port. 
* __setupMethods.py__ 
Focused on setting up the IOLab system, initializing the anynchronous threads that 
fetch and unpack and decode data, calling user code to analyze these data, and shutting things down when finished.
* __dataMethods.py__ 
Focused on decoding, organizing, and analyzing the data received from the IOLab system.
* __iolabInfo.py__ 
Code to provide callable information about the IOLab hardware & firmware (basically documentation). 
* __pyolabGlobals.py__ 
Global structures and variables used to expose IOLab data and controls to the user. 
* __analClass.py__ 
Used to separate the user code from the analysis code (basically a naive callback structure).

---

## HelloWorld 

This folder contains the simplest possible user code example to talk to the system using the 
PyOLab library:

* __userExample.py__ 
Very simple _main()_ code that opens the serial port, asks the 
dongle what its status is, receives the answer, and quits.

---

## DaqExample 

This folder contains a slightly more sophisticated user code example that uses the PyOLab 
library to implement a multi-threaded data acquisition system:

* __userExample.py__ 
Example of some _main()_ code that opens the serial port, launches data fetching and data analysis threads, 
and then waits for user input.
* __userMethods.py__ 
Examples of user routines that are called by the system during data 
analysis to expose the user to the acquired data. In this example the user code prints out any accelerometer data that 
is received from the remote, and at the end of the job it prints a summary of the records and data that were received from the system. 
* __userGlobals.py__ 
Example of some global user variables used by the user methods. 

---

## guiExample 

This folder contains example user code that implements a GUI (using Tkinter) to send and receive IOLab records and implements a multi-threaded data acquisition system similar to the one in __DaqExample__:

* __userExample.py__ 
Example of some _main()_ code that sets up a GUI, opens the serial port, launches data fetching and data analysis threads, 
and then waits for user input.
* __userMethods.py__ 
Examples of user routines that are called by the system during data  analysis to expose the user to the acquired data. In this example the user code displays records exchanged with the IOLab system using Listboxes on the GUI. 
* __userGlobals.py__ 
Example of some global user variables used by the user methods. 

---

## AnalogtExample 

Sample code that reads the A7, A8, A9 analog inputs and controls the DAC output.

---

## Getting Started

Getting up and running with IOLab using Python should be straightforward. In this section I will assume you just removed your IOLab from the box and have done nothing else. I have tested the following procedure on Mac and Windows and I assume the Linux installation will be very similar to the Mac procedure, so if you are trying this on Linux please let me know how it goes.

__1. Setting up the driver (Windows only):__ Your computer communicates with the IOLab Remote via the thumb-drive sized Dongle that is initially stored in a recess on the back of the Remote. When the Dongle is plugged into a USB port on your computer, the operating system recognizes it as a regular serial port. This is called a _virtual com port_, and the driver needed to do this is already part of your computer's OS. For Mac and Linux you don't need to do anything to use this driver - it just happens - but for Windows there is a step that you will need to do once per computer. Instructions for doing this can be found at the [IOLab.science](http://www.iolab.science/index.html), and are to save you time [this](http://www.iolab.science/driver-installation-windows.html) is a direct link to what you need. Just follow the instructions. 

FYI: This is the same step required if you are using the IOLab application that you can download from [IOLab.science](http://www.iolab.science/index.html), so if you are already using the application on this computer you don't need to repeat this step now. 

__2. Getting Python 3:__ Go to [Anaconda](https://www.continuum.io/downloads) and follow the directions to get a free installation of Python 3 on your computer. 

A nice bonus of this installation for Windows users is that it also installs an application called _Anaconda Prompt_, which provides a command-line interface that will be very useful. Mac and Linux users can just use the native terminal window whenever you need a command-line interface. 

__3. Installing PySerial:__ Open up a command-line interface (_Anaconda Prompt_ a terminal window), and type `pip install pyserial`. The PySerial module will be added to your Python configuration. 

__4. Run the example code:__ You are now ready to try it out. Plug in your Dongle and turn on your Remote. If you are running from a command line, _cd_ to one of the example folders and then type `python userExample.py` to run it. If you are using Anaconda's _Spyder_ application, open one of the _userExample.py_ files using the GUI and click the Run button.

---

## License
Use this code at your own risk in accordance with the Open Source BSD-3-Clause license. 

