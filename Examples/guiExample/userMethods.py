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
from pyolab3.pyolabGlobals import G
from pyolab3.dataMethods import *
from pyolab3.commMethods import *

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
    # (we can only do this if we know what sensors to expect, so check that first)
    if len(G.lastSensorBytes) != 0:
        for sensor in G.uncalDataDict:
            name = sensorName(sensor)
            count = len(G.uncalDataDict[sensor])
            print("found "+str(count) + " measurements of type " + name)
        print(" ")
    else:
        print("Didn't decode any sensor data info since len(G.lastSensorBytes) = 0")
        print("(you probably didn't send a getPacketCOnfig command)")
        print(" ")

#======================================================================
# User code called whenever new data is detected in the main analysis loop 
#
def analUserLoop():
    U.analUserCalls += 1

    # print any new accelerometer data to the screen
    # the sys.stdout.write and .flush makes it appear on the same line
    #
    nData = len(G.allRecList)
    if nData > U.lastRecord:
        for i in range(U.lastRecord,nData):
            recType = G.allRecList[i][0]
            index = G.allRecList[i][1]
            rec = G.recDict[recType][index]

            if recType == G.recType_dataFromRemote:
                U.listBoxData.insert(END,rec)
                U.listBoxData.see(END)
            else:
                U.listBoxCommRx.insert(END,rec)
                U.listBoxCommRx.see(END)

        U.lastRecord = nData

    
#======================================================================
# Sends a command to the IOLab system
#
def sendCommand(selection, payload):

    # Fetch the command selected by the GUI
    command = G.cmdTypeNumDict[selection]    

    # the command is a list of integers
    pyld = payload.split(',')

    # extract the payload values and create the command record 
    if len(pyld) > 0 and payload != '':
        payload = [int(pyld[i]) for i in range(len(pyld))]
        nBytes  = len(payload)
        command_record = [0x02, command, nBytes] + payload + [0x0A]

    else:
        command_record = [0x02, command, 0x00, 0x0A]

    # send the command record to the Tx listbox
    U.listBoxCommTx.insert(END,command_record)
    U.listBoxCommTx.see(END)

    # send the command record to the IOLab system
    sendIOLabCommand(G.serialPort,command_record)

    if selection == 'setFixedConfig':
        print("dont forget to send a getPacketConfig command")


#======================================================================
# Figure out the label under the data entry box and the default values
# to put into the box
#
def getEntryPrompt(selection):

    # extract information
    if selection in G.cmdTypeNumDict:

        # first get the command number by knowing the command name
        commandNum = G.cmdTypeNumDict[selection]

        # get the information we need out of the dictionary that
        # contains this information
        prompt = U.promptDict[commandNum]
    else:
        prompt = 'oops'

    return prompt