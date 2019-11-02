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
from pyolabGlobals import G
from iolabInfo import *

"""
These methods are focused on dealing with the data received 
from the IOLab system. 

"""

#=================================
# returns the n'th record received
#
def getAllRec(n):
    if n < len(G.allRecList):
        return G.recDict[G.allRecList[n][0]][G.allRecList[n][1]]
    else:
        return []

#=========================================
# returns the n'th data record received
#
def getDataRec(n):
    if n < len(G.dataRecList):
        return G.recDict[G.dataRecList[n][0]][G.dataRecList[n][1]]
    else:
        return []

#=========================================
# returns the n'th command record received
#
def getCommRec(n):
    if n < len(G.commRecList):
        return G.recDict[G.commRecList[n][0]][G.commRecList[n][1]]
    else:
        return []


#===========================================================================================
# This method spins through the raw data array and finds the actual data packet records received 
# from the remote. These are described in detail in the Indesign USB Interface Specification 
# document that can be found at at Documentation/IOLab_usb_interface_specs.pdf)
# 
# The records are put into dictionary recDict (see pyolabGlobals.py)
#
def findRecords():

    i = G.nextData         # where we will start looking
    iLast = len(G.dataList) # where we will stop looking

    # work through the data looking for valid records and saving these to G.recDict
    # 
    while i < (iLast - 3):
        if (G.dataList[i] == 2):  # find start of packet (SOP) byte = 0x2
            # find record type
            for recType in G.recTypeList:
                if G.dataList[i+1] == recType:

                    # find byte count (BC)
                    # see if we can find the end of packet (EOP) byte = 0xa
                    ndata = G.dataList[i+2]
                    # check that this isn't past the end of the list
                    if i+3+ndata < iLast:
                        if G.dataList[i+3+ndata] == 0xa:
                            # if SOP, BC, and EOP are all consistent then save the record
                            rec = G.dataList[i:i+4+ndata]
                            index = len(G.recDict[recType])

                            # all records: [recType,index] points into recDict[recType][index]
                            G.allRecList.append([recType,index])
                            if recType == G.recType_dataFromRemote:
                                # data records: [recType,index] points into recDict[recType][index]
                                G.dataRecList.append([recType,index])
                            else:
                                # command records: [recType,index] points into recDict[recType][index]
                                G.commRecList.append([recType,index])

                            # add record to the appropriate list in the record dictionary
                            G.recDict[recType].append(rec)
                            # if the thing we just received was a NACK it means a command was
                            # not properly serviced, so we should tell someone
                            if recType == G.recType_NACK:
                                if G.logData:
                                    G.logFile.write("\nNACK: " + str(rec))

                            # figure out where we are starting next
                            G.nextData = i + 4 + ndata # where the next record starts
                            i = G.nextData - 1         # since we are adding 1 after the break
                            break
                        else:
                            # shouldn't ever get here but check just in case
                            if G.logData:
                                G.logFile.write("\nguessed wrong recType ' + hex(recType) + ' at i = "+str(i))
                    else:
                        break
            i += 1

        else:
            i += 1


#=================================================================
# This method looks for changes to the fixed configuration of the 
# IOLab remote (for now just assumes you are using one remote)
#
def findLastConfig():

    # look for fixed config information
    if len(G.recDict[G.recType_getFixedConfig]) > 0:
        fc = G.recDict[G.recType_getFixedConfig][-1][4]   # the latest fixed config
    else:
        fc = 0                                            # or 0 if none found

    # if new, save it and print it
    if fc != G.lastFixedConfig:        
        G.lastFixedConfig = fc
        if G.logData:
            G.logFile.write("\nNew fixed configuration " + str(fc))


    # look for packet config information
    if len(G.recDict[G.recType_getPacketConfig]) > 0:
        pc = G.recDict[G.recType_getPacketConfig][-1][4:-1] # the latest packet config
    else:
        pc = []                                           # or [] if none found

    # if new, save it and print it
    if pc != G.lastPacketConfig:       
        G.lastPacketConfig = pc

        sc = {}
        for i in range(pc[0]):      # decode the packet config record
            s = pc[i*2+1]           # sensor
            l = pc[i*2+2]           # max data length
            sc[s] = l

        G.lastSensorBytes = sc     # save it
        G.configIsSet = True

        if G.logData:
            G.logFile.write("\nNew packet configuration " + str(pc))
            G.logFile.write("\nNew sensor configuration " + str(sc))

#===================================================================
# Extracts the payload data from dataFromRemote records and calls 
# extractSensorData() to extract raw sensor data from these 
#
def decodeDataPayloads():

    # we can only do this if we know what sensors to expect
    if len(G.lastSensorBytes) == 0:
        if G.logData:
            G.logFile.write("\n len(G.lastSensorBytes) = " + str(len(G.lastSensorBytes)))
            G.logFile.write(" this will happen if you haven't sent a getPacketConfig command")
        return


    nRec = len(G.recDict[G.recType_dataFromRemote])
    if nRec > G.nextRecord:
        for n in range(G.nextRecord,nRec):
            r = G.recDict[G.recType_dataFromRemote][n]

            recSequence = r[5]  # record sequence byte (incremented every record)
            nSens = r[6]        # number of sensors in this data record

            # this should be the same as the number expected for this config
            if nSens != len(G.lastSensorBytes):

                if G.logData:
                    G.logFile.write("\nsensors found "+str(nSens)+" expected "+str(len(G.lastSensorBytes)))
                    G.logFile.write("this can happen if you havent sent a getPacketConfig command")

            i = 7        # pointer to info and data from first sensor
            nSaved = 0   # the number of sensors we have saved data from
            while nSaved < nSens:
                thisSensor = r[i] & 0x7F            # ID of the current sensor
                sensorOverflow = r[i] > thisSensor  # is overflow bit set?

                # the first couple if records may have the overflow bit set
                if G.logData and sensorOverflow:
                    G.logFile.write("\noverflow on recSequence " +str(recSequence)+" sensor "+str(thisSensor)+" nSaved "+str(nSaved)+" nSens "+str(nSens))

                # make sure thisSensor is on the list of expected sensors for this config
                if thisSensor in G.lastSensorBytes:
                    nValidBytes = r[i+1]
                    sensorBytes = r[i+2:i+2+nValidBytes]

                    # this is where the the good stuff happens
                    extractSensorData(thisSensor,sensorBytes)
                else:
                    # if we ever get here we need to tell Mats there is a problem.
                    if G.logData:
                        G.logFile.write("\nBailing out after finding wrong sensor: " +str(thisSensor) + " in " + str(r))

                    return

                nSaved += 1

                i += (2 + G.lastSensorBytes[thisSensor])

        G.nextRecord = nRec

#======================================================================
# Extracts the raw (uncalibrated) data from individual sensor sub-payloads. 
# For details see 
#
# Inputs:
#   sensor      the number of the sensor we are decoding as per sensorName()
#   data        the data payload we are decoding
#
# Output:
#   The information is placed in a global dictionary G.uncalDataDict
#   (see pyolabGlobals.py)
#
def extractSensorData(sensor,data):

    # The different code segments below deal with data from different sensors. 
    # Only sensors marked with '*' in the list below are extracted so far
    # (I'm still working on the sensors labeled with '-')
    #
    #  * 'Accelerometer',
    #  * 'Magnetometer',
    #  * 'Gyroscope',
    #  * 'Barometer',
    #  * 'Microphone',
    #  * 'Light',
    #  * 'Force',
    #  * 'Wheel',
    #  - 'ECG3',
    #  - 'Battery',
    #  * 'HighGain',
    #  * 'Analog7',
    #  * 'Analog8',
    #  * 'Analog9',
    #  * 'Thermometer',
    #  - 'ECG9'
    #
    # Note also that the extracted data is uncalibrated. 
    # Examples of applying calibration constants cane be found 
    # in Documentation/old_csharp_code.cs. 
    #
    # For some sensors these calibration constants are
    # known (Analog and HighGain for example), for some they needs to be extracted 
    # from the system with a getCalibration() call (barometer for example), and for others
    # they need to be measured by the user (force probe & magnetometer for example).
    # 
    # The code below does not apply any calibration - it just extracts the uncalibrated data.
    #


    #-----------------------
    # Accelerometer
    # A 16 bit signed number for each of the three axes.
    # Full scale depends on device settings. Default is 4g. 
    # Calibration needed for both scale and offset. 
    #
    if sensorName(sensor) == 'Accelerometer':
        # data comes in 6 byte blocks
        if(len(data)%6 > 0):
            if G.logData:
                G.logFile.write("\nAccelerometer data not a multiple of 6 bytes")
        else:
            nsets = int(len(data)/6)
            for i in range(nsets):
                d = data[i*6:i*6+6]
                d01 = np.int16(d[0]<<8 | d[1])
                d23 = np.int16(d[2]<<8 | d[3])
                d45 = np.int16(d[4]<<8 | d[5])
                # the odd ordering & signs of the data below represent 
                # the fact that the sensor is rotated on the PCB 
                G.uncalDataDict[sensor].append([-d23,d01,d45])

    #-----------------------
    # Magnetometer
    # A 16 bit signed number for each of the three axes.
    # Full scale depends on device settings.
    # Calibration needed for both scale and offset. 
    #
    if sensorName(sensor) == 'Magnetometer':
        # data comes in 6 byte blocks
        if(len(data)%6 > 0):
            if G.logData:
                G.logFile.write("\nMagnetometer data not a multiple of 6 bytes")
        else:
            nsets = int(len(data)/6)
            for i in range(nsets):
                d = data[i*6:i*6+6]
                d01 = np.int16(d[0]<<8 | d[1])
                d23 = np.int16(d[2]<<8 | d[3])
                d45 = np.int16(d[4]<<8 | d[5])
                G.uncalDataDict[sensor].append([-d01,-d23,-d45])

    #-----------------------
    # Gyroscope
    # A 16 bit signed number for each of the three axes. Linear with omega.
    # Full scale depends on device settings.
    # Calibration needed for both scale and offset. 
    #
    if sensorName(sensor) == 'Gyroscope':
        # data comes in 6 byte blocks
        if(len(data)%6 > 0):
            if G.logData:
                G.logFile.write("\nGyroscope data not a multiple of 6 bytes")
        else:
            nsets = int(len(data)/6)
            for i in range(nsets):
                d = data[i*6:i*6+6]
                d01 = np.int16(d[0]<<8 | d[1])
                d23 = np.int16(d[2]<<8 | d[3])
                d45 = np.int16(d[4]<<8 | d[5])
                # the odd ordering & signs of the data below represent 
                # the fact that the sensor is rotated on the PCB 
                G.uncalDataDict[sensor].append([-d23,d01,d45])

    #-----------------------
    # Microphone. 
    # Linear with intensity (I assume). Returns 16 bit unsigned number.  
    #
    if sensorName(sensor) == 'Microphone':
        # data comes in 2 byte blocks
        if(len(data)%2 > 0):
            if G.logData:
                G.logFile.write("\nMicrophone data not a multiple of 2 bytes")
        else:
            nsets = int(len(data)/2)
            for i in range(nsets):
                d = data[i*2:i*2+2]
                d01 = np.uint16(d[0]<<8 | d[1])
                G.uncalDataDict[sensor].append(d01)

    #-----------------------
    # Light
    # Linear with intensity (I assume). Returns 16 bit unsigned number.   
    #
    if sensorName(sensor) == 'Light':
        # data comes in 2 byte blocks
        if(len(data)%2 > 0):
            if G.logData:
                G.logFile.write("\nLight data not a multiple of 2 bytes")
        else:
            nsets = int(len(data)/2)
            for i in range(nsets):
                d = data[i*2:i*2+2]
                d01 = np.uint16(d[0]<<8 | d[1])
                G.uncalDataDict[sensor].append(d01)

    #-----------------------
    # Force
    # A 16 bit signed number derived by measuring the B-field of a magnet 
    # that moves in response to an applied force. Linear with force. 
    # Calibration needed for both scale and offset. 
    #
    if sensorName(sensor) == 'Force':
        # data comes in 2 byte blocks
        if(len(data)%2 > 0):
            if G.logData:
                G.logFile.write("\nForce data not a multiple of 2 bytes")
        else:
            nsets = int(len(data)/2)
            for i in range(nsets):
                d = data[i*2:i*2+2]
                d01 = np.int16(d[0]<<8 | d[1])
                G.uncalDataDict[sensor].append(d01)

    #-----------------------
    # Wheel
    # A 16 bit signed number. Each measurement is the change of the wheels position 
    # in 1mm increments since the last measurement. Measurement interval is 1/100 sec.  
    #
    if sensorName(sensor) == 'Wheel':
        # data comes in 2 byte blocks
        if(len(data)%2 > 0):
            if G.logData:
                G.logFile.write("\nWheel data not a multiple of 2 bytes")
        else:
            nsets = int(len(data)/2)
            for i in range(nsets):
                d = data[i*2:i*2+2]
                d01 = np.int16(d[0]<<8 | d[1]) # dr
                G.uncalDataDict[sensor].append(d01)

    #-----------------------
    # HighGain
    # G+/G- feeds DC coupled differential op-amp w/ gain 1400
    # Op-amp output feeds internal 12 bit ADC (raw ADC values [0 - 4095])
    # Full scale count is +- 3V/2 = +- 1500 mV
    # Zero offset 0x7FF (half full scale)
    # Full scale deflection = 1500mV/1400 = 1.07 mV
    # counts per volt = 2048 * 1400 / 1500
    # 
    if sensorName(sensor) == 'HighGain':
        # data comes in 2 byte blocks
        if(len(data)%2 > 0):
            if G.logData:
                G.logFile.write("\nHighGain data not a multiple of 2 bytes")
        else:
            nsets = int(len(data)/2)
            for i in range(nsets):
                d = data[i*2:i*2+2]
                d01 = np.uint16(d[0]<<8 | d[1])
                G.uncalDataDict[sensor].append(d01)

    #-----------------------
    # Analog7
    # Feeds internal 12 bit ADC (raw ADC values [0 - 4095])
    # Full scale corresponds to either 3.0V or 3.3V depending on configuration
    # (see configName(); if configuration name contains '3V3' reference is 3.3V)
    #
    if sensorName(sensor) == 'Analog7':
        # data comes in 2 byte blocks
        if(len(data)%2 > 0):
            if G.logData:
                G.logFile.write("\nAnalog7 data not a multiple of 2 bytes")
        else:
            nsets = int(len(data)/2)
            for i in range(nsets):
                d = data[i*2:i*2+2]
                d01 = np.uint16(d[0]<<8 | d[1])
                G.uncalDataDict[sensor].append(d01)

    #-----------------------
    # Analog8
    # Feeds internal 12 bit ADC (raw ADC values [0 - 4095])
    # Full scale corresponds to either 3.0V or 3.3V depending on configuration
    # (see configName(); if configuration name contains '3V3' reference is 3.3V)
    #
    if sensorName(sensor) == 'Analog8':
        # data comes in 2 byte blocks
        if(len(data)%2 > 0):
            if G.logData:
                G.logFile.write("\nAnalog8 data not a multiple of 2 bytes")
        else:
            nsets = int(len(data)/2)
            for i in range(nsets):
                d = data[i*2:i*2+2]
                d01 = np.uint16(d[0]<<8 | d[1])
                G.uncalDataDict[sensor].append(d01)

    #-----------------------
    # Analog9
    # Feeds internal 12 bit ADC (raw ADC values [0 - 4095])
    # Full scale corresponds to either 3.0V or 3.3V depending on configuration
    # (see configName(); if configuration name contains '3V3' reference is 3.3V)
    #
    if sensorName(sensor) == 'Analog9':
        # data comes in 2 byte blocks
        if(len(data)%2 > 0):
            if G.logData:
                G.logFile.write("\nAnalog9 data not a multiple of 2 bytes")
        else:
            nsets = int(len(data)/2)
            for i in range(nsets):
                d = data[i*2:i*2+2]
                d01 = np.uint16(d[0]<<8 | d[1])
                G.uncalDataDict[sensor].append(d01)


    #-----------------------
    # Barometer
    # The uncalibrated data read from the Barometer chip represents both
    # pressure and temperature, but calibration is needed in order to turn
    # these into useful numbers. The calibration constants are programmed into
    # the barometer chip itself and can be extracted by sending the system a 
    # "getCalibration()" request with the appropriate parameters. 
    # (see IOLab_usb_interface_specs.pdf and IOLab_data_specs.pdf in Documentation/)
    #
    # from Mats old code (where i1 == d01 and i2 == d23 below):
    #            // keep only the lowest 10 bits in each
    #            i1 = (i1 >> 6) & (uint)0x3FF;
    #            i2 = (i2 >> 6) & (uint)0x3FF;
    #
    #            // Apply calibration
    #            s.cal.P = Pressure(i1, i2);
    # 
    # and see CalculateCalibrationConstants() and Pressure() in Documentation/old_csharp_code.cs
    #
    if sensorName(sensor) == 'Barometer':
        # data comes in 4 byte blocks
        if(len(data)%4 > 0):
            if G.logData:
                G.logFile.write("\nBarometer data not a multiple of 4 bytes")
        else:
            nsets = int(len(data)/4)
            for i in range(nsets):
                d = data[i*4:i*4+4]
                d01 = np.uint16(d[0]<<8 | d[1])
                d23 = np.uint16(d[2]<<8 | d[3])

                G.uncalDataDict[sensor].append([d01,d23])


    #-----------------------
    # Thermometer
    # The uncalibrated Thermometer data is oversampled 
    # (it needs to be divided by 400, which is done in this code), 
    # and then the result needs to be turned into a temperature by a linear function 
    # for which we know the slope and intercept:
    # cal = 30 + (raw - calAt30degrees)*(85-30)/(calAt85degrees-calAt30degrees)
    #   where calAt30degrees = 2041 and calAt85degrees = 2426
    # caution - these values are from some older code of Mats and should be 
    # suspect until verified. 
    #
    if sensorName(sensor) == 'Thermometer':
        # data comes in 4 byte blocks
        if(len(data)%4 > 0):
            if G.logData:
                G.logFile.write("\nThermometer data not a multiple of 4 bytes")
        else:
            nsets = int(len(data)/4)
            for i in range(nsets):
                d = data[i*4:i*4+4]
                d0123 = np.uint( d[0]<<24 | d[1]<<16 | d[2]<<8 | d[3] )

                G.uncalDataDict[sensor].append(d0123)







