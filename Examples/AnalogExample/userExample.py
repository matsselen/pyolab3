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
from tkinter import *           #This interface allow us to draw windows

# local common code
from pyolab3.analClass import AnalysisClass
from pyolab3.pyolabGlobals import G
from userGlobals import U

from pyolab3.commMethods import *
from pyolab3.setupMethods import *

# local user code
from userMethods import *

"""
This is example code that creates the GUI, launches data 
fetching and data analysis threads, and responds to user input.

"""

# start out by defining some functions that are needed by main()
#
# this little method constructs the [sensor, key+value] bytes needed by the setOutputConfig command
# (see the engineering docs for more info about this)
def skv(s,k,v):
    kv = ((k&7)<<5) + (v&31)
    return [s,kv]

# this is called whenever the slider controlling the DAC voltage is moved.
# it calls the setOutputConfig method that tells the DAQ to set the output voltage
def dacAction(val):
    iv = int(val)
    idvList = skv(25,1,iv)+skv(25,0,1)
    setOutputConfig(G.serialPort,idvList,1)

# this is called when the "Run/Pause" button is clicked
def b2Action():
    if U.b2['text'] == ' Run ':
        U.b2['text'] = ' Pause '
        if G.configIsSet:
            startData(G.serialPort)          
        else:
            print("You need to set a configuration before acquiring data")
            

    else:
        U.b2['text'] = ' Run '
        stopData(G.serialPort)
        


# This is the main code rigth here - pretty exciting

def main():
    

    # ======== START OF GUI SETUP ==============================

    root = Tk()
    root.title('IOLab')
    root.geometry('180x220')

    frame1 = Frame(root)
    frame1.pack()
    
    # sets up a slider that controls the DAC output voltage
    U.dac = Scale(frame1, from_=0, to=31, resolution=1 , label='DAC setting', orient=HORIZONTAL, command=dacAction).pack()
    
    # sets up a button to start & stop the data acquisition
    U.b2 = Button(frame1, text=' Run ', command=b2Action)
    U.b2.pack(side=TOP, fill=NONE)
    
    # leave a space between the button and the voltage displays
    Label(frame1, text=' ').pack()

    # set up and show the voltage displays
    U.txtA7 = StringVar(frame1)
    U.txtA8 = StringVar(frame1)
    U.txtA9 = StringVar(frame1)

    labelA7=Label(frame1, textvariable=U.txtA7, font="TkHeadingFont 20").pack(side=TOP)
    labelA8=Label(frame1, textvariable=U.txtA8, font="TkHeadingFont 20").pack(side=TOP)
    labelA9=Label(frame1, textvariable=U.txtA9, font="TkHeadingFont 20").pack(side=TOP)

    # ======== END OF GUI SETUP =================

    # set up IOLab user callback routines
    analClass = AnalysisClass(analUserStart, analUserEnd, analUserLoop)
    
    # start up the IOLab data acquisition stuff
    if not startItUp():
        print("Problems getting things started...bye")
        os._exit(1)

    #-------------------------------------------
    # this is the main GUI event loop
    root.mainloop()
    
    #-------------------------------------------
    # when we get to this point it means we have quite the GUI
    print("Quitting...")
    
    # shut down the IOLab data acquisition
    shutItDown()

#=====================================================
# run the above main() code 
if __name__ == "__main__":
    main()
