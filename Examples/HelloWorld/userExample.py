#
# This file is part of PyOLab. https://github.com/matsselen/pyolab
# (C) 2017 Mats Selen <mats.selen@gmail.com>
#
# SPDX-License-Identifier:    BSD-3-Clause
# (https://opensource.org/licenses/BSD-3-Clause)
#

# system stuff
import os
import sys
import time

# local common code
sys.path.append('../PyOLabCode/')
from commMethods import *

"""
This is example main() code that opens the serial port, asks the 
dongle what its status is, receives the answer, and quits.

"""

#=========================================

def main():

    # open log file if needed
    if G.logData:
        G.logFile = open('log.txt','w') # file opened in pwd

    # Start by finding the serial port that the IOLab dongle is plugged into
    print("\n")
    print("Looking for an IOLab dongle in one of the USB ports...")
    portName = getIOLabPortName()  
    
    # Open this port if one was found, otherwise quit. 
    if portName == '':
        print("Can't open the comm port - is there a dongle plugged in?")
    else:
    
        # open serial port
        G.serialPort = openIOLabPort(portName)
    
        # ask the dongle to send us a status record
        print("Asking the dongle for its status")
        getDongleStatus(G.serialPort)
    
        # Load any waiting raw serial data into a list. 
        rawList = []
        while G.serialPort.inWaiting() > 0:
            nwait = G.serialPort.inWaiting()
            raw = G.serialPort.read(nwait)
            rawList.append(raw)
    
        print("Raw hex data string from serial port:") 
        print(rawList)
 
        # Take the raw serial data and turns in into a list of bytes. 
        dList = []
        for i in range(0,len(rawList[0])):
            dList.append(rawList[0][i])   

        print("Processed data list from serial port:")
        print(dList)

        
#--------------------------
# run the above main() code 
if __name__ == "__main__":
    main()