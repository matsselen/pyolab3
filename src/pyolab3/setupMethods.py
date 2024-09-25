#
# This file is part of PyOLab. https://github.com/matsselen/pyolab
# (C) 2017 Mats Selen <mats.selen@gmail.com>
#
# SPDX-License-Identifier:    BSD-3-Clause
# (https://opensource.org/licenses/BSD-3-Clause)
#

# system stuff
import time
import numpy as np

# local stuff
from .analClass import AnalysisClass
from .pyolabGlobals import G
from .commMethods import *
from .dataMethods import *

"""
These methods are focused on setting up the IOLab system, initializing the 
threads to fetch and analyze data, and calling code to analyze these data.

"""

#=================================================
# setup some useful lists and inverse dictionaries that are 
# defined but not initialized in pyolabGlobals.py
def setupGlobalVariables():

    # set up list of valid record types
    G.recTypeList = list(G.recTypeDict.keys())

    # initialize dictionary to go from command names to numbers
    for keyNum in G.cmdTypeDict:
        G.cmdTypeNumDict[G.cmdTypeDict[keyNum]] = keyNum

    # initialize dictionary to go from record names to numbers
    for keyNum in G.recTypeDict:
        G.recTypeNumDict[G.recTypeDict[keyNum]] = keyNum

    # dictionary that will hold data records received on serial port
    for recType in G.recTypeList:
        G.recDict[recType] = []

    # set up the of lists that will hold uncalibrated sensor data
    sensorList = sensorName('SensorList')
    for sensNum in sensorList:
        G.uncalDataDict[sensNum] = []

#===============================================
# This starts up the pyolab software framework by:   
#   1) setting up the serial port that the IOLab Dongle is plugged into
#   2) launching asynchronous threads to read data and analyze data 
# 
def startItUp():

    # open log file if needed
    if G.logData:
        G.logFile = open('log.txt','w') # file opened in pwd
    # open output file if needed
    if G.dumpData:
        G.outputFile = open('data.txt','w') # file opened in pwd

    # Start by finding the serial port that the IOLab dongle is plugged into
    portName = getIOLabPortName()
    
    # Open this port if one was found, otherwise quit. 
    if portName != '':

        G.serialPort = openIOLabPort(portName)

        # create and launch a thread that gets data from the serial port
        # this will keep running until the global variable "G.running" is set to False
        G.readThread = Thread(target=readDataThread)
        G.readThread.start()

        # create and launch a thread that analyzes data
        # this will keep running until the global variable "G.running" is set to False
        G.analThread = Thread(target=analyzeDataThread)
        G.analThread.start()

        # set up some more stuff that will be needed for analysis:

        # create some useful global lists & dictionaries
        setupGlobalVariables()

        # call the user code that is executed at the beginning of a job
        AnalysisClass.handle.analStart()

        return True

    else:
        print("Can't open the comm port - is there a dongle plugged in?")
        if G.logData:
            G.logFile.write("\nCan't open the comm port - is there a dongle plugged in?")
        return False


#===============================================
# This shuts down the pyolab software framework by:   
#   1) signaling the reading and analysis threads to stop
#   2) pausing until these threads have indeed stopped
#   3) to be safe, send a signal to the IOLab remote to stop aquiring data. 
#      (it may not be aquiring data, but it doesnt hurt to make sure)
#   3) sending a signal to the IOLab remote to power itself down 
# 
def shutItDown():

    #signal that we want to quit
    G.running = False
    if G.logData:
        G.logFile.write("\nsignaling exit")

    #require that each thread finish before exiting
    G.readThread.join()
    G.analThread.join()
    if G.logData:
        G.logFile.write("\nall threads finished")

    if G.logData:
        G.logFile.write("\npower down remote 1")

    stopData(G.serialPort)
    powerDown(G.serialPort,1)


#=========================================
# This will run in a separate thread to read data from the serial port. 
# It calls readData(), which does the actual work.
#
def readDataThread():

    if G.logData:
        G.logFile.write("\nIn readDataThread: " + str(G.sleepTimeRead))
  
    # keep looping as long as G.running is True
    while G.running:
        newdata = readData()
        G.dataList.extend(newdata)
        time.sleep(G.sleepTimeRead)

    if G.logData:
        G.logFile.write("\nExiting readDataThread")


#======================================
# Called by readDataThread, which means that this method
# is basically called several times per second to get incoming data from the
# serial port.
#
def readData():

    # This part loads raw serial data into a list. 
    rawList = []
    while G.serialPort.inWaiting() > 0:
        nwait = G.serialPort.inWaiting()
        raw = G.serialPort.read(nwait)
        rawList.append(raw)

        # since the pyserial input buffer seems to be limited to just over 1000 bytes, 
        # send a warning if we are getting too close so we can reduce "sleepTime"
        if nwait > 1000: 
            if G.logData:
                G.logFile.write("\n" + str(nwait) + ": careful with that buffer, Eugene")

    # This part takes the raw data and turns in into a list of bytes. 
    dList = []
    while len(rawList) > 0:
        for i in range(0,len(rawList[0])):
            dList.append(rawList[0][i])   
        del rawList[0]

    # Return the list of bytes
    return dList


#=========================================
# This will run in a separate thread to analyze data
# It calls analyzeData(), which does the actual work.
#
def analyzeDataThread():

    if G.logData:
        G.logFile.write("\nIn analyzeDataThread: " + str(G.sleepTimeAnal))

    # keep looping as long as G.running is True
    while G.running:
        newPointer = analyzeData()
        G.dataPointer = newPointer
        time.sleep(G.sleepTimeAnal)

    if G.logData:
        G.logFile.write("\nExiting analyzeDataThread")

    # user code that is called at the end
    AnalysisClass.handle.analEnd()

    if G.dumpData:
        G.outputFile.close()

#======================================================================
# It is called by analyzeDataThread several times per second on a timer. 
# Each time this method is called there may be new data present in dataList
# since this is filled asynchronously as data packets arrive to the serial port. 
#
def analyzeData():

    # for now just print the data to "outputfile". You should do something 
    # more interesting here (like actually analyzing data for example)

    # dataLength is the length of the dataList "now"
    # G.dataPointer was the value of dataLength the last time this was called,
    # which means that data between these two values is new.
    dataLength = len(G.dataList)
    if dataLength > G.dataPointer:

        # analyze the raw data stream and sort it into records. 
        findRecords()

        # look for a change in configuration
        findLastConfig()

        # extract sensor data information from the data records
        decodeDataPayloads()

        # call user analysis code
        AnalysisClass.handle.analLoop()

        # write data to an output file if the dumpData flag is set
        if G.dumpData:
            dataString = ''
            for i in range(G.dataPointer,dataLength):
                dataString += hex(G.dataList[i])[2:] + ' '
            G.outputFile.write(dataString)

    return dataLength


