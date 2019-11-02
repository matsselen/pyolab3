#
# This file is part of PyOLab. https://github.com/matsselen/pyolab
# (C) 2017 Mats Selen <mats.selen@gmail.com>
#
# SPDX-License-Identifier:    BSD-3-Clause
# (https://opensource.org/licenses/BSD-3-Clause)
#

"""
Global variables used by the pyolab library.
These expose data acquired by the system, as well as control 
parameters for the system and for this analysis code. 

"""

class G(object):

    # control varialbles
    sleepTimeRead = 0.05 # time to sleep each read loop
    sleepTimeAnal = 0.11 # time to sleep each read loop
    sleepCommand  = 0.10 # time to sleep after a command is sent
    dumpData    = False  # if True the base analysis code dumps data to a file
    logData     = True   # if True code writes info/error messages to a file
    running     = True   # used to signal treads to quit
    configIsSet = False  # is it?

    # ports & files & threads
    serialPort = None    # pointer to the virtual com port
    outputFile = None    # file handle for output
    logFile    = None    # file handle for message logging file
    readThread = None    # pointer to data reading thread
    analThread = None    # pointer to data analysis thread

    # raw data retrieval and analysis - don't mess with these
    dataList = []        # data received from the serial port
    dataPointer = 0      # pointer to the next raw byte to be analyzed
    nextData    = 0      # used by findRecords()
    nextRecord  = 0      # used by decodeDataPayloads()

    # used by data analysis
    lastFixedConfig  = 0        # the last fixed config record received
    lastPacketConfig = []       # the last packet config record received
    lastSensorBytes  = {}       # dictionary of maximum byte-counts keyed by sensor

    
    #  Here is a description of the various commands and record types.
    #  These are described in detail in the USB Interface Specification document 
    #  http://www.iolab.science/Documents/IOLab_Expert_Docs/IOLab_usb_interface_specs.pdf

    #
    # records that are received from the system are of these types
    #
    recType_ACK = 0xaa
    #   0xaa = 170 (ACK)
    #
    recType_NACK = 0xbb
    #   0xbb = 187 (NACK)
    #
    recType_getDongleStatus = 0x14
    #   0x14 =  19 (response to getDongleStatus() )    
    #   Record: 0x02 : 0x14 : 0x06 : Dongle FW (2) : Mode : ID (3) : 0xa
    #
    recType_getSensorConfig = 0x23
    #   0x23 =  35 (response to getSensorConfig() )
    #   Record: 0x02 : 0x23 : Nbytes : Remote : NsensKeyPairs : ..Pairs.. : 0xa
    #
    recType_getOutputConfig = 0x25
    #   0x25 =  37 (response to getOutputConfig() )
    #   Record: 0x02 : 0x25 : Nbytes : Remote : NoutputKeyPairs : ..Pairs.. : 0xa
    #
    recType_getFixedConfig = 0x27
    #   0x27 =  39 (response to getFixedConfig() )
    #   Record: 0x02 : 0x27 : 0x01 : Config : 0xa
    #   
    recType_getPacketConfig = 0x28
    #   0x28 =  40 (response to getPacketConfig() )
    #   Record: 0x02 : 0x28 : Nbytes : Remote : Nsens : sens1 : len1 :...: sensN : lenN : 0xa
    #
    recType_getCalibration = 0x29
    #   0x29 =  41 (response to getCalibration() )
    #   Record: 0x02 : 0x29 : Nbytes : Remote : sensor : Nbytes : cal1 :...: calN : 0xa
    #
    recType_getRemoteStatus = 0x2a
    #   0x2a =  42 (response to getRemoteStatus() )
    #   Record: 0x02 : 0x2a : 0x07 : Remote : Sens FW (2) : RF FW (2) : Battery (2) : 0xa
    #
    recType_rfStatusFromRemote = 0x40
    #   0x40 =  64 (asynchronous RF status records when RF signal lost or re-acquired)
    #   Record: 0x02 : 0x40 : 0x02 : Remote : RFstatus : 0xa
    #   
    recType_dataFromRemote = 0x41
    #   0x41 =  65 (asynchronous data records sent during actual data acquisition)
    #   Record: 0x02 : 0x41 : Nbytes : Remote : Frame# : RFinfo : Data Packet : RSSI : 0xa

    # set up a dictionary of the above record type names keyed by record type number
    # (note to self - make this a bit less redundant in the future)
    recTypeDict = {
                   recType_getDongleStatus      :'getDongleStatus',
                   recType_getSensorConfig      :'getSensorConfig',
                   recType_getOutputConfig      :'getOutputConfig',
                   recType_getFixedConfig       :'getFixedConfig', 
                   recType_getPacketConfig      :'getPacketConfig', 
                   recType_getCalibration       :'getCalibration',
                   recType_getRemoteStatus      :'getRemoteStatus', 
                   recType_rfStatusFromRemote   :'rfStatusFromRemote', 
                   recType_dataFromRemote       :'dataFromRemote', 
                   recType_ACK                  :'ACK', 
                   recType_NACK                 :'NACK'
                   }

    # as initialized in setupGlobalVariables(), called by startItUp(),
    # this is the inverse dictionary of recTypeDict
    recTypeNumDict = {}

    # as initialized in setupGlobalVariables(), called by startItUp(), 
    # this will a list of the record types that findRecords() will look for
    recTypeList = []

    #
    # dictionary of supported commands (so far at least)
    cmdTypeDict = {
        0x20 :'startData',        # [0x02, 0x20, 0x00, 0x0A]
        0x21 :'stopData',         # [0x02, 0x21, 0x00, 0x0A]
        0x22 :'setSensorConfig',  # [0x02, 0x22, nBytes, payload, 0x0A]
        0x23 :'getSensorConfig',  # [0x02, 0x23, 0x01, remote, 0x0A] 
        0x24 :'setOutputConfig',  # [0x02, 0x24, nBytes, payload, 0x0A]
        0x25 :'getOutputConfig',  # [0x02, 0x25, 0x01, remote, 0x0A]
        0x26 :'setFixedConfig',   # [0x02, 0x26, 0x02, remote, config, 0x0A]
        0x27 :'getFixedConfig',   # [0x02, 0x27, 0x01, remote, 0x0A]
        0x28 :'getPacketConfig',  # [0x02, 0x28, 0x01, remote, 0x0A] 
        0x29 :'getCalibration',   # [0x02, 0x29, 0x02, remote, sensor, 0x0A] 
        0x14 :'getDongleStatus',  # [0x02, 0x14, 0x00, 0x0A]
        0x2A :'getRemoteStatus',  # [0x02, 0x2A, 0x01, remote, 0x0A] 
        0x2B :'powerDown'         # [0x02, 0x2B, 0x01, remote, 0x0A] 
        }

    # as initialized in setupGlobalVariables(), called by startItUp(),
    # this will the inverse dictionary of the above
    cmdTypeNumDict = {}

    #=======================================================================
    # The following dictionaries are basically the key outputs of the system, and provide
    # various ways for the user to extract these data.

    recDict = {}  
    # Dictionary that stores received records, keyed by record type as listed above
    # (for example, asynchronous data records from the remote have type 0x41 =  65)
    # Each record is stored as a list, starting with SOP = 0x2 and ending with EOP = 0xa.
    # See Documentation/record_example_1.pdf for some examples.
    # Find detailed documentation at Documentation/IOLab_usb_interface_specs.pdf

    allRecList = []
    # list of all records received in order. Each entry is a list [recType, index], 
    # so that the record can be found at recDict[recType][index] 

    commRecList = []
    # list of all command records received in order. Each entry is a list [recType, index], 
    # so that the record can be found at recDict[recType][index] 

    dataRecList = []
    # list of all data records received in order. Each entry is a list [recType, index], 
    # so that the record can be found at recDict[recType][index] 

    uncalDataDict = {}
    # Dictionary that stores uncalibrated data from sensors, keyed by sensor number. 
    # Each key returns a list of numbers, or a list of lists if data for a sensor involves
    # more than a single number, such as the accelerometer for example, which returns 
    # values for all 3 axes. (If you want to know details have a look at the Indesign documents 
    # See Documentation/record_example_2.pdf for some examples.
    # Find detailed documentation at Documentation/IOLab_data_specs.pdf





