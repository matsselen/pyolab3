
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
sys.path.append('../PyOLabCode/')
from analClass import AnalysisClass
from pyolabGlobals import G
from userGlobals import U

from commMethods import *
from setupMethods import *

# local user code
from userMethods import *

def button1Action():
    U.selection = U.commandVar.get()
    U.payload = U.entry.get()
    sendCommand( U.selection, U.payload)

def commandSelect(event):
    U.selection = U.commandVar.get()
    prompt = getEntryPrompt(U.selection)
    U.labelstring.set(prompt[0])
    U.entry.delete(0, END)
    U.entry.insert(0,prompt[1])

"""
This is example main() code that creates the GUI, launches data 
fetching and data analysis threads, and responds to user input.

"""

def main():
    
    #============================================================
    # set up IOLab user callback routines
    analClass = AnalysisClass(analUserStart, analUserEnd, analUserLoop)
    
    # start up the IOLab data acquisition stuff
    if not startItUp():
        print("Problems getting things started...bye")
        os._exit(1)
    
    #
    # ======== START OF GUI SETUP ==============================
    # Create and format the graphical elements that this example uses.  
    # This takes many inches of code because Tkinter is not that fancy 
    # (and also, I suspect, because I'm not that smart)
    
    # create the root window
    root = Tk()
    root.geometry('800x600')
    root.title("IOLab Test Application")
    
    #-------------------------------------------
    # the left part of the screen holds control elements
    # this frame does not expand when the window is scaled
    leftframe = Frame(root)
    leftframe.pack( side = LEFT , fill=BOTH, expand=0)
    
    # mats crappy way of adding some whitespace at the top
    Label(leftframe, text="\n\n\n").pack() 
    
    # the drop-down menu will contain a list of possible commands
    U.commandVar = StringVar(leftframe)
    commandNames = U.commandList
    defaultCommandString = 'setFixedConfig'
    U.commandVar.set(defaultCommandString)  # set the default command
    U.selection = defaultCommandString
    
    # set up the drop-down menu using the above list of commands
    Label(leftframe, text="Select Command",font=("Arial", 16)).pack()
    commMenu = OptionMenu(leftframe, U.commandVar, *commandNames, command = commandSelect)
    commMenu.pack(side=TOP, fill=X,padx=10,pady=10)
    
    Label(leftframe, text="\n\n\nEnter payload if needed",font=("Arial", 16)).pack()
    # the entry box is for commands that require a payload
    # first set up the label under the box and the default values in the box 
    prompt = getEntryPrompt(U.selection)
    U.labelstring = StringVar(leftframe)
    U.labelstring.set(prompt[0])
    
    # create the Entry box and place the label under it. 
    U.entry = Entry(leftframe)
    U.entry.pack(side=TOP,padx=10,pady=10)
    U.entry.insert(0,prompt[1])
    entrylabel = Label(leftframe, textvariable=U.labelstring).pack()
    
    # the button is for sending selected commands to IOLab
    Label(leftframe, text="\n\n\nClick SEND to send",font=("Arial", 16)).pack()
    button1 = Button(leftframe,text = " SEND ",command = button1Action)
    button1.pack(side=TOP, padx=10,pady=10)
    
    #-------------------------------------------
    # the right part of the screen displays data and control records
    # this frame DOES expand when the window is scaled
    rightframe = Frame(root)
    rightframe.pack( side = LEFT, fill=BOTH, expand=1)
    
    # scrollable text-box to display COMMAND records
    # create a child frame to hold Tx and Rx listboxes
    cframe = Frame(rightframe)
    cframe.pack(side = TOP , fill=BOTH, expand=0)
    
    # create a child frame to hold the listbox and scrollbar
    cTxframe = Frame(cframe)
    cTxframe.pack(side = TOP , fill=BOTH, expand=1)
    Label(cTxframe, text="\nControl Records Sent").pack(side = TOP)
    
    # create scrollbar and Tx listbox and bind them together
    scrollbarCommTx = Scrollbar(cTxframe, orient=VERTICAL)
    U.listBoxCommTx = Listbox(cTxframe, yscrollcommand=scrollbarCommTx.set)
    scrollbarCommTx.config(command=U.listBoxCommTx.yview)
    # pack stuff
    scrollbarCommTx.pack(side=RIGHT, fill=Y)
    U.listBoxCommTx.pack(fill=BOTH, expand=1,padx=5,pady=5)
    
    # create a child frame to hold the Rx listbox and scrollbar
    cRxframe = Frame(cframe)
    cRxframe.pack(side = TOP , fill=BOTH, expand=1)
    Label(cRxframe, text="\nControl Records Received").pack(side = TOP)
    
    # create scrollbar and Rx listbox and bind them together
    scrollbarCommRx = Scrollbar(cRxframe, orient=VERTICAL)
    U.listBoxCommRx = Listbox(cRxframe, yscrollcommand=scrollbarCommRx.set)
    scrollbarCommRx.config(command=U.listBoxCommRx.yview)
    # pack stuff
    scrollbarCommRx.pack(side=RIGHT, fill=Y)
    U.listBoxCommRx.pack(fill=BOTH, expand=1,padx=5,pady=5)
    
    # scrollable text-box to display DATA records
    # create a child frame to hold the listbox and scrollbar
    dframe = Frame(rightframe)
    dframe.pack(side = TOP , fill=BOTH, expand=1)
    Label(dframe, text="\nData Records Received").pack(side = TOP)
    
    # create x and y scrollbars and a data listbox, and in the darkness bind them
    scrollbarData = Scrollbar(dframe, orient=VERTICAL)
    scrollbarDatax = Scrollbar(dframe, orient=HORIZONTAL)
    U.listBoxData = Listbox(dframe, xscrollcommand=scrollbarDatax.set, yscrollcommand=scrollbarData.set)
    scrollbarData.config(command=U.listBoxData.yview)
    scrollbarDatax.config(command=U.listBoxData.xview)
    # pack stuff
    scrollbarData.pack(side=RIGHT, fill=Y)
    scrollbarDatax.pack(side=BOTTOM, fill=X)
    U.listBoxData.pack(fill=BOTH, expand=1,padx=5,pady=5)
    
    # ======== END OF GUI SETUP =================
    
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