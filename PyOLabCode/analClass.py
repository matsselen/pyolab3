#
# This file is part of PyOLab. https://github.com/matsselen/pyolab
# (C) 2017 Mats Selen <mats.selen@gmail.com>
#
# SPDX-License-Identifier:    BSD-3-Clause
# (https://opensource.org/licenses/BSD-3-Clause)
#

"""
This is a cheap (and probably naive) trick to allow us to separate the 
user code from the analysis code. We basically create an instance
of this class in the main user code, and in so doing we tell the 
analysis code which user methods to call during the analysis. 

The class is instantiated in the "main" routine of your code 
(which is called userExample.py in this example), thus:

    analClass = AnalysisClass(analUserStart, analUserEnd, analUserLoop)

where analUserStart(), analUserEnd(), analUserLoop() are defined by you,
and currently have examples living in "userMethods.py"

"""

class AnalysisClass(object):
    handle = ''
    def __init__(self,an1,an2,an3):
        print("creating instance of AnalysisClass")
        self.analStart = an1
        self.analEnd   = an2
        self.analLoop  = an3
        AnalysisClass.handle = self