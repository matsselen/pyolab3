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
from tkinter import *

# local common code
sys.path.append('../PyOLabCode/')
from pyolabGlobals import G
from dataMethods import *
from commMethods import *

# local user code
from userGlobals import U

"""
Files starting with the name "user", like this one, are provided 
so that users can create their own analysis jobs.

These user methods are a handy way to try and isolate the user code from the 
library code.  

"""

#======================================================================
# User code called at the beginning. 
#
def analUserStart():
    print("in analUserStart()")
    print("\n Make sure your remote is turned on. \n")

    # set up analog inputs with 3.3V reference
    print("setting up the remote to measure voltages...")
    setFixedConfig(G.serialPort,12,1)
    getFixedConfig(G.serialPort,1)
    getPacketConfig(G.serialPort,1)        


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

    # find the average of the A7 data acquired since the last time
    nA7 = len(G.uncalDataDict[21])
    aveA7 = 0
    if nA7 > U.lastA7:
        for i in range(U.lastA7,nA7):
            aveA7 += G.uncalDataDict[21][i]
        aveA7 = float(aveA7)/(nA7 - U.lastA7)
    U.lastA7 = nA7

    # find the average of the A8 data acquired since the last time
    nA8 = len(G.uncalDataDict[22])
    aveA8 = 0
    if nA8 > U.lastA8:
        for i in range(U.lastA8,nA8):
            aveA8 += G.uncalDataDict[22][i]
        aveA8 = float(aveA8)/(nA8 - U.lastA8)
    U.lastA8 = nA8

    # find the average of the A9 data acquired since the last time
    nA9 = len(G.uncalDataDict[23])
    aveA9 = 0
    if nA9 > U.lastA9:
        for i in range(U.lastA9,nA9):
            aveA9 += G.uncalDataDict[23][i]
        aveA9 = float(aveA9)/(nA9 - U.lastA9)
    U.lastA9 = nA9

    # find the average of the HG data acquired since the last time
    nHG = len(G.uncalDataDict[12])
    aveHG = 0
    if nHG > U.lastHG:
        for i in range(U.lastHG,nHG):
            aveHG += G.uncalDataDict[12][i]
        aveHG = float(aveHG)/(nHG - U.lastHG)
    U.lastHG = nHG

    adcPerVolt_A       = 4095 / 3.3
    adcPerMilliVolt_HG = 1400 * 2047 / (1000*3.3/2)
    offsetHG           = 2047

    aveA7 = aveA7/adcPerVolt_A
    aveA8 = aveA8/adcPerVolt_A
    aveA9 = aveA9/adcPerVolt_A
    aveHG = (aveHG-offsetHG)/adcPerMilliVolt_HG

    U.txtA7.set('A7=%5.3f V'%aveA7)
    U.txtA8.set('A8=%5.3f V'%aveA8)
    U.txtA9.set('A9=%5.3f V'%aveA9)
    #U.txtHG.set('HG=%6.3f V'%aveHG)