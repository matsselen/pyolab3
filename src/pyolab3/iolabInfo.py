#
# This file is part of PyOLab. https://github.com/matsselen/pyolab
# (C) 2017 Mats Selen <mats.selen@gmail.com>
#
# SPDX-License-Identifier:    BSD-3-Clause
# (https://opensource.org/licenses/BSD-3-Clause)
#

"""
This file contains some methods that provide some info about
the IOLab hardware & firmware (basically documentation). 

"""


#======================================
# Provides a way to match sensor number with sensor name
# if called with sensNum = 'SensorList', returns list of all sensor numbers]
#
def sensorName(sensNum):
    
    sensorDict = {
        1  : 'Accelerometer',
        2  : 'Magnetometer',
        3  : 'Gyroscope',
        4  : 'Barometer',
        6  : 'Microphone',
        7  : 'Light',
        8  : 'Force',
        9  : 'Wheel',
        10 : 'ECG3',
        11 : 'Battery',
        12 : 'HighGain',
        21 : 'Analog7',
        22 : 'Analog8',
        23 : 'Analog9',
        26 : 'Thermometer',
        241: 'ECG9'
    }

    if sensNum == 'SensorList':
        return list(sensorDict.keys())

    if sensNum in sensorDict:
        return sensorDict[sensNum]
    else:
        return ''

#======================================
# Provides a way to match sensor configuration number with 
# the configuration name and the details of which sensors 
# and sample rates this configuration uses
#
def configName(configNum):
        
    configDict = {
     1:['Gyroscope',1,
            [{ 'sensor': 3, 'rate': 380 }]],

     2:['Accelerometer',1,
            [{ 'sensor': 1, 'rate': 400 }]],

     3:['Orientation',4,
            [{ 'sensor': 1, 'rate': 100 },
             { 'sensor': 2, 'rate': 80 },
             { 'sensor': 3, 'rate': 95 },
             { 'sensor': 12, 'rate': 100 }]],

     4:['Mini-motion',3,
            [{ 'sensor': 1, 'rate': 200 },
             { 'sensor': 9, 'rate': 100 },
             { 'sensor': 8, 'rate': 200 }]],


     5:['Pendulum',3,
            [{ 'sensor': 1, 'rate': 100 },
             { 'sensor': 3, 'rate': 95 },
             { 'sensor': 8, 'rate': 100 }]],

     6:['Ambient',4,
            [{ 'sensor': 4, 'rate': 100 },
             { 'sensor': 11, 'rate': 50 },
             { 'sensor': 7, 'rate': 400 },
             { 'sensor': 26, 'rate': 50 }]],

     7:['ECG3',1,
            [{ 'sensor': 10, 'rate': 400 }]],

     8:['Header 3V',5,
            [{ 'sensor': 21, 'rate': 100 },
             { 'sensor': 22, 'rate': 100 },
             { 'sensor': 23, 'rate': 100 },
             { 'sensor': 12, 'rate': 200 },
             { 'sensor': 13, 'rate': 100 }]],

     9:['Microphone',1,
            [{ 'sensor': 6, 'rate': 2400 }]],

     10:['Magnetic',2,
                [{ 'sensor': 2, 'rate': 80 },
             { 'sensor': 12, 'rate': 400 }]],

     32:['Gyroscope (HS)',1,
            [{ 'sensor': 3, 'rate': 760 }]],

     12:['Header 3V3',5,
            [{ 'sensor': 21, 'rate': 100 },
             { 'sensor': 22, 'rate': 100 },
             { 'sensor': 23, 'rate': 100 },
             { 'sensor': 12, 'rate': 200 },
             { 'sensor': 13, 'rate': 100 }]],

     33:['Accelerometer (HS)',1,
            [{ 'sensor': 1, 'rate': 800 }]],

     34:['Orientation (HS)',3,
            [{ 'sensor': 1, 'rate': 400 },
             { 'sensor': 2, 'rate': 80 },
             { 'sensor': 3, 'rate': 190 }]],

     35:['Motion',4,
            [{ 'sensor': 1, 'rate': 200 },
             { 'sensor': 3, 'rate': 190 },
             { 'sensor': 9, 'rate': 100 },
             { 'sensor': 8, 'rate': 200 }]],

     36:['Sports',4,
            [{ 'sensor': 10, 'rate': 200 },
             { 'sensor': 1, 'rate': 200 },
             { 'sensor': 2, 'rate': 80 },
             { 'sensor': 3, 'rate': 190 }]],

     37:['Pendulum (HS)',3,
            [{ 'sensor': 1, 'rate': 200 },
             { 'sensor': 3, 'rate': 190 },
             { 'sensor': 8, 'rate': 200 }]],

     38:['Kitchen Sink',11,
            [{ 'sensor': 2, 'rate': 80 },
             { 'sensor': 1, 'rate': 100 },
             { 'sensor': 9, 'rate': 100 },
             { 'sensor': 8, 'rate': 100 },
             { 'sensor': 3, 'rate': 95 },
             { 'sensor': 7, 'rate': 100 },
             { 'sensor': 11, 'rate': 100 },
             { 'sensor': 12, 'rate': 100 },
             { 'sensor': 21, 'rate': 100 },
             { 'sensor': 13, 'rate': 100 },
             { 'sensor': 4, 'rate': 100 }]],

     39:['Microphone (HS)',1,
            [{ 'sensor': 6, 'rate': 4800 }]],

     40:['Ambient Light (HS)',1,
            [{ 'sensor': 7, 'rate': 4800 }]],

     41:['Ambient Light & Accel (HS)',2,
            [{ 'sensor': 7, 'rate': 800 },
             { 'sensor': 1, 'rate': 800 }]],

     42:['Force Gauge & Accel (HS)',2,
            [{ 'sensor': 8, 'rate': 800 },
             { 'sensor': 1, 'rate': 800 }]],

     43:['Ambient Light & Micro (HS)',2,
            [{ 'sensor': 7, 'rate': 2400 },
             { 'sensor': 6, 'rate': 2400 }]],

     44:['Electrocardiograph (9)',1,
            [{ 'sensor': 10, 'rate': 800 }]],

     45:['High Gain (HS)',1,
            [{ 'sensor': 12, 'rate': 4800 }]],

     46:['Force Gauge (HS)',1,
            [{ 'sensor': 8, 'rate': 4800 }]],

     47:['ECG & Analog',1,
            [{ 'sensor': 241, 'rate': 400 }]]
    }


    if configNum in configDict:
        return configDict[configNum]
    else:
        return ''