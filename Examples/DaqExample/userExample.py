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
from pyolab3.analClass import AnalysisClass
from pyolab3.pyolabGlobals import G
from pyolab3.commMethods import *
from pyolab3.setupMethods import *

# local user code
from userMethods import *

"""
This is example main() code that opens the serial port, launches data 
fetching and data analysis threads, and responds to user input.

"""
def main():

    #=========================================
    # This is the main code. All it does is open the serial port,
    # launch the data fetching and data analysis threads, and then
    # go into a loop waiting for user input. 
    # Suggested user inputs (without quotes):
    # '=38'     (sets configuration 38, which includes a lot of sensors)
    # 'a'       (starts the data acquisition)
    # 's'       (stops the data acquisition)
    # 'x'       (exits the program)
    #
    # See comments in userMethods.py for more details. 
    #
    
    # This instantiates an object that holds information about which user analysis
    # methods are to be called by the main analysis code. Doing it it this way removes
    # the need for the analysis code to know about the user code in advance.
    # It is assumed that the methods analUserStart(), analUserEnd(), analUserLoop()
    # are all localed in the local file "userMethods.py"
    #
    analClass = AnalysisClass(analUserStart, analUserEnd, analUserLoop)
    
    # This causes the raw data to be dumped to a file called "data.txt" in
    # the working directory
    G.dumpData = True
    
    if not startItUp():
        print("Problems getting things started...bye")
        os._exit(1)
    
    # Loop to get user commands.
    while G.running:
        # take a little nap to allow stuff from the last command to finish
        time.sleep(.2) 
    
        # ask the user for input
        print("\nEnter command:")
        print("   =n  to set remote configuration to n (for example =38)")
        print("   a   to run acquisition")
        print("   s   to stop acquisition")
        print("   x   to exit\n")
    
        # wait for user input
        command = input()
    
        # this block is executed when its time to quit
        if command.count('x') > 0:
            #signal that we want to quit
            shutItDown()
    
        else:
    
            if command.count('a') > 0:
                if G.configIsSet:
                    print("Calling startData()")
                    startData(G.serialPort)
                else:
                    print("You need to set a configuration before acquiring data")
    
            elif command.count('s') > 0:
                print("Calling stopData()")
                stopData(G.serialPort)
    
            elif command.count('=') > 0:
                n=int(command[1:])
                print("Calling setFixedConfig("+str(n)+")") 
                print(configName(n))
                # set the fixed configuration for remote 1 
                setFixedConfig(G.serialPort,n,1)
    
                # ask remote 1 to tell us about its configuration 
                print("Calling getFixedConfig()") 
                getFixedConfig(G.serialPort,1)
    
                # ask remote 1 to tell us about its data packet configuration
                print("Calling getPacketConfig()") 
                getPacketConfig(G.serialPort,1)


#--------------------------
# run the above main() code 
if __name__ == "__main__":
    main()