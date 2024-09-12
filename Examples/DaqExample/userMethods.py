#
# This file is part of PyOLab. https://github.com/matsselen/pyolab
# (C) 2017 Mats Selen <mats.selen@gmail.com>
#
# SPDX-License-Identifier:    BSD-3-Clause
# (https://opensource.org/licenses/BSD-3-Clause)
#

# system stuff
import sys
import time


# local common code
sys.path.append('../PyOLabCode/')
from pyolabGlobals import G
from dataMethods import *

# local user code
from userGlobals import U

"""
Files starting with the name "user", like this one, are provided 
so that users can create their own analysis jobs.

These user methods are a handy way to try and isolate the user code from the 
library code.  

In this particular example the user code print out any accelerometer data that 
is received from the remote, and at the end of the job it prints a summary of
the records and data that were received from the system.

"""

#======================================================================
# User code called at the beginning. 
#
def analUserStart():
    print("in analUserStart()")

    U.sensNum = 1
    print("...will dump data from " + sensorName(U.sensNum))

    print("\n MAKE SURE YOUR REMOTE IS TURNED ON \n")

#======================================================================
# User code called at the end. 
#
def analUserEnd():
    print("in analUserEnd()")
    print("analUserLoop() was called " + str(U.analUserCalls) + " times")

    # print information about the records that were received
    for rectype in G.recTypeDict:
        name = G.recTypeDict[rectype]
        count = len(G.recDict[rectype])
        print("found "+str(count) + " records of type " + name)
    print(" ")

    # print information about the sensor data that was received
    for sensor in G.uncalDataDict:
        name = sensorName(sensor)
        count = len(G.uncalDataDict[sensor])
        print("found "+str(count) + " measurements of type " + name)
    print(" ")

#======================================================================
# User code called whenever new data is detected in the main analysis loop 
#
def analUserLoop():
    U.analUserCalls += 1

    # print any new accelerometer data to the screen
    # the sys.stdout.write and .flush makes it appear on the same line
    #
    nData = len(G.uncalDataDict[U.sensNum])
    if nData > U.lastDataPrinted:
        for i in range(U.lastDataPrinted,nData):
            #print G.uncalDataDict[U.sensNum][i]
            sys.stdout.write('%s\r' % '                                  ') # clear previous line
            sys.stdout.write('%s\r' % str(G.uncalDataDict[U.sensNum][i]))   # print uncalibrated accel data
            sys.stdout.flush()

        U.lastDataPrinted = nData