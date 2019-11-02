#
# This file is part of PyOLab. https://github.com/matsselen/pyolab
# (C) 2017 Mats Selen <mats.selen@gmail.com>
#
# SPDX-License-Identifier:    BSD-3-Clause
# (https://opensource.org/licenses/BSD-3-Clause)
#

# system stuff
import time
import serial
import serial.tools.list_ports
from threading import Thread

# local stuff
from pyolabGlobals import G


"""
This file contains a set of methods that allows a user to communicate 
with the IOLab system via the virtual com port. 

"""

#======================================
# Returns a list of names of the serial ports that
# the OS thinks has an IOLab dongle is plugged into them
def getIOLabPortName():

    # get a list of all serial ports
    ports = list(serial.tools.list_ports.comports())
    
    # loop over all of the ports found and get the name of 
    # the last one that is an IOLab USB virtual com port
    
    p = ''
    pList = []
    for port in ports:
         pInfo = list(port)
         if 'IOLab' in pInfo[1]:
            pList.append(pInfo)

    nFound = len(pList)

    if nFound == 0:
        print("Found no IOLab USB Dongles")
        if G.logData:
            G.logFile.write("\nFound no IOLab USB Dongles")
    
    else:

        p = pList[0][0]
        if nFound == 1:
            if G.logData:
                G.logFile.write("\nFound IOLab USB Dongle: " + p)
        else:
            if G.logData:
                G.logFile.write("\nWarning: found " +str(nFound)+ " IOLab USB Dongles.")
                G.logFile.write("\nUsing the first one found: " + str(p))

    return p

#======================================
# Opens the IOLab com port that has name pName
#
def openIOLabPort(pName):

    # open the com port
    serialport = serial.Serial(pName)
    serialport.baudrate = 115200
    serialport.timeout  = 1
    G.port = serialport

    return serialport


#=======================================================================
# This next bunch of routines sends commands to the IOLab remote via
# the serial port "s". For a description of the data packets that are returned
# by each one see the USB Interface Specification document 
# (Indesign document number 1814F03 Revision 11, available on the IOLab web page at
#  http://www.iolab.science/Documents/IOLab_Expert_Docs/IOLab_usb_interface_specs.pdf)

#======================================
# Ask the dongle to send a data packet of type 0x14 telling us its status
def getDongleStatus(s):

    command = 0x14
    command_record = [0x02, command, 0x00, 0x0A] 
    s.write(bytearray(command_record))
    time.sleep(G.sleepCommand)  #give the serial port some time to receive the data

#======================================
# Start data acquisition. 
# The response will be an ACK packet if successful, or NACK packet if not.
# The remote will asynchronously start sending data packets in the format 
# described by record returned by the "getPacketConfig" command. 
# The asynchronous data packets all have the same format and are identified by record type 0x41. 
def startData(s):

    command = 0x20
    command_record = [0x02, command, 0x00, 0x0A]
    s.write(bytearray(command_record))
    time.sleep(G.sleepCommand)  #give the serial port some time to receive the data
    
#======================================
# Stop data acquisition. 
# The response will be an ACK packet if successful, or NACK packet if not.
def stopData(s):

    command = 0x21
    command_record = [0x02, command, 0x00, 0x0A]
    s.write(bytearray(command_record))
    time.sleep(G.sleepCommand)  #give the serial port some time to receive the data

#======================================
# Sends a sensor configuration record to the selected remote. 
# The response will be an ACK packet if successful, or NACK packet if not. 
def setSensorConfig(s,idValueList,remote):

    nPairs  = len(idValueList)/2 
    payload = [remote,nPairs]+idValueList
    nBytes  = len(payload)

    command = 0x22
    command_record = [0x02, command, nBytes] + payload + [0x0A] 

    s.write(bytearray(command_record))
    time.sleep(G.sleepCommand)  #give the serial port some time to receive the data

#======================================
# Gets a sensor configuration record from the selected remote. 
def getSensorConfig(s,remote):

    command = 0x23
    command_record = [0x02, command, 0x01, remote, 0x0A] 
    s.write(bytearray(command_record))
    time.sleep(G.sleepCommand)  #give the serial port some time to receive the data

#======================================
# Sends an output configuration record to the selected remote. 
# The response will be an ACK packet if successful, or NACK packet if not. 
def setOutputConfig(s,idValueList,remote):

    nPairs  = int(len(idValueList)/2)
    payload = [remote,nPairs]+idValueList
    nBytes  = len(payload)
#    print(nPairs)
#    print(nBytes)
    command = 0x24
    command_record = [0x02, command, nBytes] + payload + [0x0A] 

    s.write(bytearray(command_record))
    time.sleep(G.sleepCommand)  #give the serial port some time to receive the data

#======================================
# Gets an output configuration record from the selected remote. 
def getOutputConfig(s,remote):

    command = 0x25
    command_record = [0x02, command, 0x01, remote, 0x0A] 
    s.write(bytearray(command_record))
    time.sleep(G.sleepCommand)  #give the serial port some time to receive the data

#======================================
# Ask remote to set the current sensor configuration to "config". 
# The response will be an ACK packet if successful, or NACK packet if not. 
def setFixedConfig(s,config,remote):

    command = 0x26
    command_record = [0x02, command, 0x02, remote, config, 0x0A] 
    s.write(bytearray(command_record))
    time.sleep(G.sleepCommand)  #give the serial port some time to receive the data

#======================================
# Ask remote to send a data packet of type 0x27 telling us the current sensor configuration 
def getFixedConfig(s, remote):

    command = 0x27
    command_record = [0x02, command, 0x01, remote, 0x0A] 
    s.write(bytearray(command_record))
    time.sleep(G.sleepCommand)  #give the serial port some time to receive the data

#======================================
# Ask remote to send a data packet of type 0x28 telling us the format of the 
# data packets that will be sent to us when acquisition is started
def getPacketConfig(s, remote):

    command = 0x28
    command_record = [0x02, command, 0x01, remote, 0x0A] 
    s.write(bytearray(command_record))
    time.sleep(G.sleepCommand)  #give the serial port some time to receive the data

#======================================
# Ask remote to send a data packet of type 0x29 containing calibration information from sensor. 
def getCalibration(s, sensor, remote):

    command = 0x29
    command_record = [0x02, command, 0x02, remote, sensor, 0x0A] 
    s.write(bytearray(command_record))
    time.sleep(G.sleepCommand)  #give the serial port some time to receive the data

#======================================
# Ask remote to send a data packet of type 0x2a telling us its status
def getRemoteStatus(s, remote):

    command = 0x2A
    command_record = [0x02, command, 0x01, remote, 0x0A] 
    s.write(bytearray(command_record))
    time.sleep(G.sleepCommand)  #give the serial port some time to receive the data

#======================================
# Power down remote. 
# The response will be an ACK packet if successful, or NACK packet if not.
def powerDown(s,remote):

    command = 0x2B
    command_record = [0x02, command, 0x01, remote, 0x0A]
    s.write(bytearray(command_record))
    time.sleep(G.sleepCommand)  #give the serial port some time to receive the data

#======================================
# This is a generic command 
def sendIOLabCommand(s,command_record):

    s.write(bytearray(command_record))
    time.sleep(G.sleepCommand)  #give the serial port some time to receive the data