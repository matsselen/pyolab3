using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace IOLabV2
{
    class SensorDataTypes
    {
    }

    // static class for matching sensor names and numbers
    class SensorType
    {
        // this dictionary keys on sensor number and returns the name of the sensor
        public static Dictionary<int, String> name = new Dictionary<int, string>();

        static SensorType()
        {
            name.Add(0x00, "RSSI");
            name.Add(0x01, "Accelerometer");
            name.Add(0x02, "Magnetometer");
            name.Add(0x03, "Gyroscope");
            name.Add(0x04, "Barometer");
            name.Add(0x06, "Microphone");
            name.Add(0x07, "Light");
            name.Add(0x08, "Force");
            name.Add(0x09, "Wheel");
            name.Add(0x0A, "ECG");
            name.Add(0x0B, "Battery");
            name.Add(0x0C, "HighGain");
            name.Add(0x0D, "Digital");
            name.Add(0x15, "Analog7");
            name.Add(0x16, "Analog8");
            name.Add(0x17, "Analog9");
            name.Add(0x1A, "Thermometer");
        }

    }

    #region ## sensor data framework ##

    //================================================================
    // This class in the interface between the control panel and the 
    // classes that deal with hardware and data
    class SensorDataFramework
    {
        // this dictionary keys on sensor number and returns the index into the lists for this sensor
        public Dictionary<int, int> dictIndexFromNumber = new Dictionary<int, int>();

        // this dictionary keys on sensor name and returns the index into the lists for this sensor
        public Dictionary<string, int> dictIndexFromName = new Dictionary<string, int>();

        // this dictionary keys on sensor index and returns the sensor number
        public Dictionary<int, int> dictNumberFromIndex = new Dictionary<int, int>();

        // this dictionary keys on sensor inded and returns chart index
        public Dictionary<int, int> dictChartFromIndex = new Dictionary<int, int>();

        // this list tells us which sensors are active
        public List<bool> sensorActive = new List<bool>();

        // list of checkboxes for active sensors
        public List<CheckBox> sensorCheckBox = new List<CheckBox>();

        // the number of sensors
        public int nSensors = 0;

        // pointer back up to the parent Remote class
        public Remote parentRemote;

        // constructor
        public SensorDataFramework(Remote r)
        {
            parentRemote = r;
        }

        // this method adds another sensor to the appropriate dictioaries and lists
        public void AnotherSensor(int sensorNumber)
        {
            // sensor number - index - name dictionaries
            dictIndexFromNumber.Add(sensorNumber, nSensors);
            dictNumberFromIndex.Add(nSensors, sensorNumber);
            dictIndexFromName.Add(SensorType.name[sensorNumber], nSensors);

            // default sensor status to inactive
            sensorActive.Add(false);

            nSensors++;
        }

        // used to clear stuff out before initializing a new configuration
        public void Clear()
        {
            MessLog.AddMessage("In SensorDataFramework.Clear() for remoteIndex " + parentRemote.remoteIndex);

            // close the active chart pages
            for (int i = 0; i < parentRemote.chartList.Count(); i++)
            {
                ChartPage cp = parentRemote.chartList[i];
                cp.CleanupFFT(); // probably not needed as long as the chart is the parent of the FFT
                cp.Close();
            }

            // clear the data for each active sensor
            dictChartFromIndex.Clear();
            for (int i = 0; i < nSensors; i++)
            {
                sensorActive[i] = false;
                parentRemote.sensorDataList[i].Clear();
            }

            // remove the checkboxes that controlled the charts
            for (int i = 0; i < parentRemote.chartList.Count; i++)
            {
                CheckBox cb = sensorCheckBox[i];
                CPglobal.cbPanel.Controls.Remove(cb);
                cb = null;
            }

            // clear the list of checkboxes
            sensorCheckBox.Clear();

            // clear the list of charts
            parentRemote.chartList.Clear();

            // signal that this remote is no-longer configured
            parentRemote.configured = false;

            // stop & reset the run timer
            TimingInfo.runTimer.Stop();
            TimingInfo.runTimer.Reset();
        }

        // call the sensors setup routine, then set up a checkbox to control the chart for this sensor
        public void Setup(int sensorIndex, int sampleRate)
        {
            string sensorName = SensorType.name[dictNumberFromIndex[sensorIndex]];
            MessLog.AddMessage("Setting up " + sensorName);

            // the index of the parent remote
            int remoteIndex = parentRemote.remoteIndex;

            // make association with this sensors index and a location in the chartlist
            dictChartFromIndex.Add(sensorIndex, parentRemote.chartList.Count());
            // call the Setup method up the sensor data class for this sensor
            parentRemote.sensorDataList[sensorIndex].Setup(sampleRate, remoteIndex);
            // add the chart created by the above setup to the chartlist for this remote
            parentRemote.chartList.Add(parentRemote.sensorDataList[sensorIndex].chartPage);

            // if remote 2 is active then append the remote number to the chart titles
            if (CPglobal.control.data.R[1].active)
            {
                string remoteTitlePrefix = String.Format(" (Remote {0})", remoteIndex + 1);
                parentRemote.sensorDataList[sensorIndex].chartPage.ModifyTitle(remoteTitlePrefix);
            }

            // figure out the index of this chart and set its initial location accordingly
            int chartIndex = parentRemote.chartList.Count() - 1;
            parentRemote.chartList[chartIndex].initialLocation = new Point(30 * chartIndex + remoteIndex * 200, 230 + 30 * chartIndex);

            // set up the checkbox for this sensor. Put Remote 1 (2) info on the left (right) side
            CheckBox checkbox = new CheckBox();
            checkbox.AutoSize = true;
            checkbox.Location = new System.Drawing.Point(10 + remoteIndex * 150, 30 + CPglobal.heightPerCheckbox * chartIndex);
            checkbox.Name = sensorName;
            checkbox.UseVisualStyleBackColor = true;
            checkbox.Visible = true;
            checkbox.Checked = false;
            checkbox.CheckedChanged += new System.EventHandler(CheckedChanged);
            CPglobal.cbPanel.Controls.Add(checkbox);
            checkbox.Text = sensorName;
            checkbox.Font = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
            checkbox.ForeColor = Color.Blue;
            sensorCheckBox.Add(checkbox);

        }

        // hide or show selected chart page
        private void CheckedChanged(object sender, EventArgs e)
        {
            CheckBox s = (CheckBox)sender;
            int index = dictIndexFromName[s.Text];
            ChartPage cp =  parentRemote.chartList[dictChartFromIndex[index]];
            if (s.Checked)
                cp.Show();
            else
                cp.Hide();
        }

        //=========================================================================
        // check and show (or uncheck & hide) the chart for the selected sensorName
        public void HideAllCharts()
        {
            for (int i = 0; i < parentRemote.chartList.Count; i++)
            {
                parentRemote.chartList[i].Hide();
                sensorCheckBox[i].Checked = false;
            }
        }

        //=========================================================================
        // check and show (or uncheck & hide) the chart for the selected sensorName
        public void CheckAndShowChart(string chartName, bool showChart)
        {
            if (dictIndexFromName.ContainsKey(chartName))
            {
                int sensorIndex = dictIndexFromName[chartName];
                if (dictChartFromIndex.ContainsKey(sensorIndex))
                {
                    int chartIndex = dictChartFromIndex[sensorIndex];
                    ChartPage cp = parentRemote.chartList[chartIndex];

                    if (showChart)
                    {
                        sensorCheckBox[chartIndex].Checked = true;
                        cp.Show();
                    }
                    else
                    {
                        sensorCheckBox[chartIndex].Checked = false;
                        cp.Hide();
                    }
                }
            }
            else
                MessLog.AddMessage("In CheckAndShowChart() - dont recognize chart name " + chartName);
        }

        //=========================================================================
        // set trace name. commandString format = sensor name , trace number , trace name
        public void SetTraceName(string commandString)
        {
            string[] parts = commandString.Split(',');
            if (parts.Length < 3)
            {
                MessLog.AddMessage("Short command string passed to SetTraceName: " + commandString);
                return;
            }

            string sensorName = parts[0].Trim();
            int traceNumber = Int32.Parse(parts[1]);
            string traceName = parts[2].Trim();

            if (dictIndexFromName.ContainsKey(sensorName))
            {
                int sensorIndex = dictIndexFromName[sensorName];
                if (dictChartFromIndex.ContainsKey(sensorIndex))
                {
                    int chartIndex = dictChartFromIndex[sensorIndex];
                    ChartPage cp = parentRemote.chartList[chartIndex];
                    if(traceNumber < cp.stripChart.traceList.Count)
                        cp.stripChart.traceList[traceNumber].SetCheckBoxName(traceName);
                }
            }
            else
                MessLog.AddMessage("In SetTraceName() - dont recognize chart name " + sensorName);
        }
        
        //=========================================================================
        // set trace status. commandString format = sensor name , trace number , trace status
        // three bits are used:
        // status: b001 = show/hide checkbox, b010 = check/uncheck, b100 integrable/unintegrable  
        public void SetTraceStatus(string commandString)
        {
            string[] parts = commandString.Split(',');

            if (parts.Length < 3)
            {
                MessLog.AddMessage("Short command string passed to SetTraceStatus: " + commandString);
                return;
            }

            string sensorName = parts[0].Trim();
            int traceNumber = Int32.Parse(parts[1]);
            int traceStatus = Int32.Parse(parts[2]);

            if (dictIndexFromName.ContainsKey(sensorName))
            {
                int sensorIndex = dictIndexFromName[sensorName];
                if (dictChartFromIndex.ContainsKey(sensorIndex))
                {
                    int chartIndex = dictChartFromIndex[sensorIndex];
                    ChartPage cp = parentRemote.chartList[chartIndex];
                    if (traceNumber < cp.stripChart.traceList.Count)
                    {
                        cp.stripChart.traceList[traceNumber].checkbox.Visible = ((traceStatus & 0x01) > 0);
                        cp.stripChart.traceList[traceNumber].checkbox.Checked = ((traceStatus & 0x02) > 0);
                        cp.stripChart.traceList[traceNumber].isIntegrable     = ((traceStatus & 0x04) > 0);
                    }
                }
            }
            else
                MessLog.AddMessage("In SetTraceStatus() - dont recognize chart name " + sensorName);
        }

        //=========================================================================
        // set default trace limits. commandString format = sensor name , trace number , yMax0, yMin0
        // NOTE - changes the scale for a single trace which may create confusing results - beware
        // its probably best to use SetChartScaleAndShift instead
        public void SetTraceLimits(string commandString)
        {
            string[] parts = commandString.Split(',');
            if (parts.Length < 4)
            {
                MessLog.AddMessage("Short command string passed to SetTraceLimits: " + commandString);
                return;
            }

            string sensorName = parts[0].Trim();
            int traceNumber = Int32.Parse(parts[1]);
            double yMax = Double.Parse(parts[2]);
            double yMin = Double.Parse(parts[3]);

            if (dictIndexFromName.ContainsKey(sensorName))
            {
                int sensorIndex = dictIndexFromName[sensorName];
                if (dictChartFromIndex.ContainsKey(sensorIndex))
                {
                    int chartIndex = dictChartFromIndex[sensorIndex];
                    ChartPage cp = parentRemote.chartList[chartIndex];
                    if (traceNumber < cp.stripChart.traceList.Count)
                    {
                        cp.stripChart.SetTraceLimits(traceNumber, yMax, yMin);
                        cp.stripChart.SetDefaultTraceLimits(traceNumber, yMax, yMin);
                        cp.stripChart.clearBeforeNext = true;
                        cp.stripChart.Draw();
                    }
                }
            }
            else
                MessLog.AddMessage("In SetTraceLimits() - dont recognize chart name " + sensorName);
        }

        //=========================================================================
        // scale & shift a chart. commandString format = sensor name , scale, shift
        public void SetChartScaleAndShift(string commandString)
        {
            string[] parts = commandString.Split(',');
            if (parts.Length < 3)
            {
                MessLog.AddMessage("Short command string passed to SetChartScaleAndShift: " + commandString);
                return;
            }

            string sensorName = parts[0].Trim();
            double scale = Double.Parse(parts[1]);
            double shift = Double.Parse(parts[2]);

            if (dictIndexFromName.ContainsKey(sensorName))
            {
                int sensorIndex = dictIndexFromName[sensorName];
                if (dictChartFromIndex.ContainsKey(sensorIndex))
                {
                    int chartIndex = dictChartFromIndex[sensorIndex];
                    ChartPage cp = parentRemote.chartList[chartIndex];
                    cp.stripChart.MoveChartLimits(scale, shift);
                    cp.SetSlidersYaxis(scale, shift);
                    cp.stripChart.clearBeforeNext = true;
                    cp.stripChart.Draw();
                }
            }
        }

        //=========================================================================
        // turn off background images & data for all charts (default)
        public void ChartBackgroundClear()
        {
            for (int i = 0; i < parentRemote.chartList.Count; i++)
            {
                parentRemote.chartList[i].stripChart.useChartUnderlayData = false;
                parentRemote.chartList[i].stripChart.useChartUnderlayImage = false;
                parentRemote.chartList[i].ShowFit(false);
            }
        }

        //=========================================================================
        // set the background image for a chart. commandString format = sensor name , fileName
        public void SetChartBackgroundImage(string commandString)
        {
            string[] parts = commandString.Split(',');
            if (parts.Length < 2)
            {
                MessLog.AddMessage("Short command string passed to SetChartBackgroundImage: " + commandString);
                return;
            }

            string sensorName = parts[0].Trim();
            string fileName = parts[1].Trim();

            if (dictIndexFromName.ContainsKey(sensorName))
            {
                int sensorIndex = dictIndexFromName[sensorName];
                if (dictChartFromIndex.ContainsKey(sensorIndex))
                {
                    int chartIndex = dictChartFromIndex[sensorIndex];
                    ChartPage cp = parentRemote.chartList[chartIndex];
                    cp.stripChart.chartUnderlayImageFile = fileName;
                    cp.stripChart.useChartUnderlayImage = true;
                    cp.stripChart.clearBeforeNext = true;
                    cp.stripChart.Draw();
                }
            }
        }

        //=========================================================================
        // set the background data for a chart. 
        // commandString format = sensor name , traceNumber, fileName, penWidth, iFirst, iLast, iOffset, nLookback
        public void SetTraceTemplateData(string commandString)
        {
            string[] parts = commandString.Split(',');
            if (parts.Length < 6)
            {
                MessLog.AddMessage("Short command string passed to SetTraceTemplateData: " + commandString);
                return;
            }

            string sensorName = parts[0].Trim();
            int traceNumber = Int32.Parse(parts[1]);
            string fileName = parts[2].Trim();
            int penWidth = Int32.Parse(parts[3]);
            int iFirst = Int32.Parse(parts[4]);
            int iLast = Int32.Parse(parts[5]);
            int iOffset = Int32.Parse(parts[6]);

            int nLookback = 0;
            if (parts.Length > 7)
                nLookback = Int32.Parse(parts[7]);

            if (dictIndexFromName.ContainsKey(sensorName))
            {
                int sensorIndex = dictIndexFromName[sensorName];
                if (dictChartFromIndex.ContainsKey(sensorIndex))
                {
                    int chartIndex = dictChartFromIndex[sensorIndex];
                    ChartPage cp = parentRemote.chartList[chartIndex];
                    cp.stripChart.bkgTrace.TraceData(fileName, traceNumber, penWidth, iOffset, iFirst, iLast, nLookback);
                    cp.stripChart.useChartUnderlayData = true;
                    cp.ShowFit(true);
                    cp.stripChart.clearBeforeNext = true;
                    cp.stripChart.Draw();
                }
            }
        }

        //=========================================================================
        // run the customMethod delegate for a chart (if it has been defined)
        public void RunCustomMethod(string commandString)
        {
            string[] parts = commandString.Split(',');
            string sensorName = parts[0].Trim();

            if (dictIndexFromName.ContainsKey(sensorName))
            {
                int sensorIndex = dictIndexFromName[sensorName];
                if (dictChartFromIndex.ContainsKey(sensorIndex))
                {
                    int chartIndex = dictChartFromIndex[sensorIndex];
                    ChartPage cp = parentRemote.chartList[chartIndex];
                    if (cp.customMethod != null)
                        cp.customMethod();
                }
            }
        }

        //=========================================================================
        // set the averaging parameter for a chart (if it has been defined)
        public void SetChartAveragingParameter(string commandString)
        {
            string[] parts = commandString.Split(',');
            if (parts.Length < 2)
            {
                MessLog.AddMessage("Short command string passed to SetChartAveragingParameter: " + commandString);
                return;
            }

            string sensorName = parts[0].Trim();
            int averageNumber = Int32.Parse(parts[1]);

            if (dictIndexFromName.ContainsKey(sensorName))
            {
                int sensorIndex = dictIndexFromName[sensorName];
                if (dictChartFromIndex.ContainsKey(sensorIndex))
                {
                    int chartIndex = dictChartFromIndex[sensorIndex];
                    ChartPage cp = parentRemote.chartList[chartIndex];
                    cp.SetnAverage(averageNumber);
                }
            }
        }

        //=========================================================================
        // set the analysis mode of a chart (i.e. are measurements enabled? are we integrating?)
        public void SetChartAnalysisMode(string commandString)
        {
            string[] parts = commandString.Split(',');
            if (parts.Length < 2)
            {
                MessLog.AddMessage("Short command string passed to SetChartAnalysisMode: " + commandString);
                return;
            }

            string sensorName = parts[0].Trim();
            int analysisMode = Int32.Parse(parts[1]); // bits decode: b001 - measurements, b010 - integrals, b100 show menu

            if (dictIndexFromName.ContainsKey(sensorName))
            {
                int sensorIndex = dictIndexFromName[sensorName];
                if (dictChartFromIndex.ContainsKey(sensorIndex))
                {
                    int chartIndex = dictChartFromIndex[sensorIndex];
                    ChartPage cp = parentRemote.chartList[chartIndex];

                    // if if bit 0 is set we enable regular measurements                  
                    if ((analysisMode & 0x01) > 0)
                        cp.SetMeasurement(true);
                    else
                        cp.SetMeasurement(false);

                    // if bit 1 is set we enable integrals as well
                    // and if not we disable integrals
                    if ((analysisMode & 0x02) > 0)
                        cp.SetIntegral(true);
                    else
                        cp.SetIntegral(false);

                    // if bit 2 is set we show the options menu
                    // and if not we disable integrals
                    if ((analysisMode & 0x04) > 0)
                        cp.OptionMenuVisible(true);
                    else
                        cp.OptionMenuVisible(false);
                }
            }
        }

    }

    #endregion ## sensor data framework ##

    #region ## sensor data base class ##

    //================================================================
    // sensor data base class from which the others are derived
    class SensorData
    {
        // classes needed by all data types
        public ChartPage chartPage;
        public PacketTimingCollection packets;
        public Sample3DCollection samples; // overridden in some classes

        public int averageNumber = 1;
        public bool active = false;
        public int remoteIndex = 0;

        //================================================================
        // setup method
        public virtual void Setup(int nominalSampleRate, int remote = 0)
        {
            MessLog.AddMessage("Ooops - somehow we are in SensorData base class Setup()");
        }

        //================================================================
        // clear class
        public virtual void Clear()
        {
            active = false;
            chartPage = null;
            samples = null;
            packets = null;
        }

        //================================================================
        // adds data 
        public virtual void Add(DataFromRemote d)
        {
            MessLog.AddMessage("Ooops - somehow we are in SensorData base class Add()");
        }

        //================================================================
        // returns the length of the sample list 
        public virtual int NumDataSamples()
        {
            return samples.sList.Count;
        }

        //===========================================================
        // analyzes the packet timing data and updates samplesPerSecond
        public virtual void AnalyzeTime()
        {
            packets.Analyze();
            chartPage.stripChart.samplesPerSecond = packets.samplesPerSecond;
        }

        //============================================
        // recalibrate the data and make a new chart
        public virtual void ReCalibrate()
        {
            MessLog.AddMessage("Ooops - somehow we are in SensorData base class ReCalibrate()");
        }

        //============================================
        // custom function
        public virtual void Custom()
        {
            MessLog.AddMessage("Ooops - somehow we are in SensorData base class Custom()");
        }

        //==========================================
        // Calculate calibration constants
        public virtual void CalcCal(CalibrationData cd)
        {
            MessLog.AddMessage("Ooops - somehow we are in SensorData base class CalcCal()");
        }

        //==========================================
        // Write calibration constants
        public virtual bool WriteCal()
        {
            MessLog.AddMessage("Ooops - somehow we are in SensorData base class WriteCal()");
            return false;
        }

        //==========================================
        // Read accelerometer calibration constants
        public virtual bool ReadCal()
        {
            MessLog.AddMessage("Ooops - somehow we are in SensorData base class ReadCal()");
            return false;
        }

        //==========================================
        // Write data to file
        public virtual bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("Ooops - somehow we are in SensorData base class WriteData()");
            return false;
        }

    }

    #endregion ## sensor data base class ##

    #region ## accelerometer data ##

    //================================================================
    // This class holds data from the accelerometer
    class AccelerometerData : SensorData
    {
 
        // calibration constants
        public double xCountsPerUnit = 8206;
        public double xCountsOffset = 38;
        public double yCountsPerUnit = 8116;
        public double yCountsOffset = 0;
        public double zCountsPerUnit = 8162;
        public double zCountsOffset = -53;

        // set up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {

            active = true;
            samples = new Sample3DCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("Accelerometer");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0.0;

            // define the y axis for each trace
            chartPage.stripChart.traceNameWidth = 80;
            chartPage.stripChart.includeRawValues = false;
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, -4, 4);       // ax
            chartPage.stripChart.AddStrip(chartPage.thinBluePen, -4, 4);      // ay
            chartPage.stripChart.AddStrip(chartPage.thinGreenPen, -4, 4);     // az

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "Ax/g", "Ay/g", "Az/g" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 3;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be drawn on the background of the chart
            for (int i = 1; i <= 20; i++)
            {
                chartPage.stripChart.AddHorizontalLine(i, 0, chartPage.fineGreyPen);
                chartPage.stripChart.AddHorizontalLine(-i, 0, chartPage.fineGreyPen);
            }

            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

            if (!ReadCal())
                MessLog.AddMessage("Cant read accelerometer calibration  constants - using defaults");

        }

        //================================================================
        // adds data to the AccelerometerData class
        public override void Add(DataFromRemote d)
        {
            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 1; // accelerometer
            int nSamples = d.sensorData[sensType].Count() / 6;
            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // each of the (x,y,z) axes is 2 bytes, MSB then LSB. Cast into Int16 below.
                uint i1 = (uint)(d.sensorData[sensType][i * 6 + 0] << 8) + (uint)d.sensorData[sensType][i * 6 + 1];
                uint i2 = (uint)(d.sensorData[sensType][i * 6 + 2] << 8) + (uint)d.sensorData[sensType][i * 6 + 3];
                uint i3 = (uint)(d.sensorData[sensType][i * 6 + 4] << 8) + (uint)d.sensorData[sensType][i * 6 + 5];

                // create an instance of Sample to put data into
                Sample3D s = new Sample3D();
                s.raw.x = (Int16)(-i2); // sensor on first V2 prototype 
                s.raw.y = (Int16)i1;    // is rotated
                s.raw.z = (Int16)i3;

                // Apply calibration
                s.cal.x = ((double)s.raw.x - xCountsOffset) / xCountsPerUnit;
                s.cal.y = ((double)s.raw.y - yCountsOffset) / yCountsPerUnit;
                s.cal.z = ((double)s.raw.z - zCountsOffset) / zCountsPerUnit;
                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;

                // are we reversing the y-axis?
                if (Remotes.remoteInfo[remoteIndex].reverseY) s.cal.y *= -1;

                samples.Add(s);

                // do averaging according to the number selected on the chart page
                Data3DStats stat = samples.TimeAverage(chartPage.nAverage);

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time, new double[] { stat.aveXcal, stat.aveYcal, stat.aveZcal },
                                                     new double[] { s.raw.x, s.raw.y, s.raw.z });
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In AccelerometerData.ReCalibrate:");

           // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples + 
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal.x = ((double)samples.sList[i].raw.x - xCountsOffset) / xCountsPerUnit;
                samples.sList[i].cal.y = ((double)samples.sList[i].raw.y - yCountsOffset) / yCountsPerUnit;
                samples.sList[i].cal.z = ((double)samples.sList[i].raw.z - zCountsOffset) / zCountsPerUnit;
                samples.sList[i].time = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;

                // are we reversing the y-axis?
                if (Remotes.remoteInfo[remoteIndex].reverseY) samples.sList[i].cal.y *= -1;

            }

            for (int i = 0; i < nSamples; i++)
            {
                // do averaging according to the number selected on the chart page
                Data3DStats stat = samples.TimeAverage(chartPage.nAverage,"CalXYZ", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time, 
                    new double[] { stat.aveXcal, stat.aveYcal, stat.aveZcal },
                    new double[] { samples.sList[i].raw.x, samples.sList[i].raw.y, samples.sList[i].raw.z });
            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In AccelerometerData.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\Accelerometer_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\Accelerometer_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time rawX rawY rawZ calX calY calZ aveX({0}) aveY({0}) aveZ({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3} {4} {5:f4} {6:f4} {7:f4} {8:f4} {9:f4} {10:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index, time, rawX, rawY, rawZ, calX, calY, calZ, aveX({0}), aveY({0}), aveZ({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3}, {4}, {5:f4}, {6:f4}, {7:f4}, {8:f4}, {9:f4}, {10:f4}";
                    }


                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        Data3DStats stat = samples.TimeAverage(chartPage.nAverage, "CalXYZ", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw.x, samples.sList[i].raw.y, samples.sList[i].raw.z,
                            samples.sList[i].cal.x, samples.sList[i].cal.y, samples.sList[i].cal.z,
                            stat.aveXcal, stat.aveYcal, stat.aveZcal);
                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save Accelerometer data");
            }

            return writeStatus;
        }

        //==========================================
        // Calculate calibration constants
        public override void CalcCal(CalibrationData cd)
        {
            MessLog.AddMessage("In AccelerometerData.CalcCal()");

            xCountsPerUnit = (cd.a[0].x - cd.a[1].x) / 2;
            xCountsOffset  = (cd.a[0].x + cd.a[1].x) / 2;
            yCountsPerUnit = (cd.a[2].y - cd.a[3].y) / 2;
            yCountsOffset  = (cd.a[2].y + cd.a[3].y) / 2;
            zCountsPerUnit = (cd.a[4].z - cd.a[5].z) / 2;
            zCountsOffset  = (cd.a[4].z + cd.a[5].z) / 2;

            WriteCal();
        }

        //==========================================
        // Write accelerometer calibration constants
        public override bool WriteCal()
        {
            MessLog.AddMessage("In AccelerometerData.WriteCal()");

            // dont try to save aything if the ID hasnt been read from the remote yet
            if (Remotes.remoteInfo[0].ID == 0)
            {
                MessLog.AddMessage("Remote ID not known: Cant save Accelerometer calibration data");
                return false;
            }

            // figure out filename
            string calFileName = String.Format(@"Calibration\CalData\{0:x6}_Accelerometer.txt", Remotes.remoteInfo[0].ID);

            // write the calibration data
            try
            {
                using (StreamWriter sw = new StreamWriter(calFileName))
                {

                    sw.WriteLine("Accelerometer, {0:f4}, {1:f4}, {2:f4}, {3:f4}, {4:f4}, {5:f4}, {6:f4}, {7:f6}",
                        xCountsPerUnit, xCountsOffset, yCountsPerUnit, yCountsOffset, zCountsPerUnit, zCountsOffset, chartPage.stripChart.samplesPerSecond, chartPage.stripChart.timingShift);
                }
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save Accelerometer calibration data");
                return false;
            }

            return true;
        }

        //==========================================
        // Read accelerometer calibration constants
        public override bool ReadCal()
        {
            MessLog.AddMessage("In AccelerometerData.ReadCal()");

            int remoteID = Remotes.remoteInfo[remoteIndex].ID;
            MessLog.AddMessage(String.Format("RemoteIndex: {0}, RemoteID: 0x{1:x6}", remoteIndex, remoteID));

            // dont try to save aything if the ID hasnt been read from the remote yet
            if (remoteID == 0)
            {
                MessLog.AddMessage("Remote ID not known: Cant read Accelerometer calibration data");
                return false;
            }

            // figure out filename
            string calFileName = String.Format(@"Calibration\CalData\{0:x6}_Accelerometer.txt", remoteID);

            // read the calibration data
            try
            {
                using (StreamReader sr = new StreamReader(calFileName))
                {
                    string line = sr.ReadLine();
                    if (line != null)
                    {
                        string[] parts = line.Split(',');
                        if (parts[0].Contains("Accelerometer") && parts.Length == 9)
                        {
                            xCountsPerUnit = Convert.ToDouble(parts[1]);
                            xCountsOffset = Convert.ToDouble(parts[2]);
                            yCountsPerUnit = Convert.ToDouble(parts[3]);
                            yCountsOffset = Convert.ToDouble(parts[4]);
                            zCountsPerUnit = Convert.ToDouble(parts[5]);
                            zCountsOffset = Convert.ToDouble(parts[6]);
                            chartPage.stripChart.samplesPerSecond = Convert.ToDouble(parts[7]);
                            chartPage.stripChart.timingShift = Convert.ToDouble(parts[8]);
                            MessLog.AddMessage("samplesPerSecond " + chartPage.stripChart.samplesPerSecond + ", timingShift " + chartPage.stripChart.timingShift);
                        }
                    }
                    else
                    {
                        MessLog.AddMessage("Trouble finding accelerometer constants - using some defaults");
                        return false;
                    }
                }
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant read Accelerometer calibration data");
                return false;
            }

            return true;
        }

    }

    #endregion ## accelerometer data ##

    #region ## magnetometer data ##

    //================================================================
    // This class holds data from the magnetometer
    class MagnetometerData : SensorData
    {
        // default calibration constants
        public double xCountsPerUnit = 573;
        public double xCountsOffset = -569;
        public double yCountsPerUnit = 591;
        public double yCountsOffset = -388;
        public double zCountsPerUnit = 558;
        public double zCountsOffset = -1283;

        // constructor sets up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {
            active = true;
            samples = new Sample3DCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("Magnetometer");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0.0;

            // define the y axis for each trace
            chartPage.stripChart.traceNameWidth = 80;
            chartPage.stripChart.includeRawValues = false;
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, -4, 4);       // Bx
            chartPage.stripChart.AddStrip(chartPage.thinBluePen, -4, 4);      // By
            chartPage.stripChart.AddStrip(chartPage.thinGreenPen, -4, 4);     // Bz

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "Bx", "By", "Bz" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 1;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be drawn on the background of the chart
            for (int i = 1; i <= 20; i++)
            {
                chartPage.stripChart.AddHorizontalLine(i, 0, chartPage.fineGreyPen);
                chartPage.stripChart.AddHorizontalLine(-i, 0, chartPage.fineGreyPen);
            }

            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

            if (!ReadCal())
                MessLog.AddMessage("Cant read magnetometer calibration  constants - using defaults");
        }

        //================================================================
        // adds data to the MagnetometerData class
        public override void Add(DataFromRemote d)
        {
            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 2; // magnetometer
            int nSamples = d.sensorData[sensType].Count() / 6;
            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // each of the (x,y,z) axes is 2 bytes, MSB then LSB. Cast into Int16 below.
                uint i1 = (uint)(d.sensorData[sensType][i * 6 + 0] << 8) + (uint)d.sensorData[sensType][i * 6 + 1];
                uint i2 = (uint)(d.sensorData[sensType][i * 6 + 2] << 8) + (uint)d.sensorData[sensType][i * 6 + 3];
                uint i3 = (uint)(d.sensorData[sensType][i * 6 + 4] << 8) + (uint)d.sensorData[sensType][i * 6 + 5];

                // create an instance of Sample to put data into
                Sample3D s = new Sample3D();
                s.raw.x = (Int16)(-i1);
                s.raw.y = (Int16)(-i2);
                s.raw.z = (Int16)(-i3);

                // Apply calibration
                s.cal.x = ((double)s.raw.x - xCountsOffset) / xCountsPerUnit;
                s.cal.y = ((double)s.raw.y - yCountsOffset) / yCountsPerUnit;
                s.cal.z = ((double)s.raw.z - zCountsOffset) / zCountsPerUnit;
                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;

                // are we reversing the y-axis?
                if (Remotes.remoteInfo[remoteIndex].reverseY) s.cal.y *= -1;

                samples.Add(s);

                // do averaging according to the number selected on the chart page
                Data3DStats stat = samples.TimeAverage(chartPage.nAverage);

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time, new double[] { stat.aveXcal, stat.aveYcal, stat.aveZcal },
                                                     new double[] { s.raw.x, s.raw.y, s.raw.z });
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In MagnetometerData.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal.x = ((double)samples.sList[i].raw.x - xCountsOffset) / xCountsPerUnit;
                samples.sList[i].cal.y = ((double)samples.sList[i].raw.y - yCountsOffset) / yCountsPerUnit;
                samples.sList[i].cal.z = ((double)samples.sList[i].raw.z - zCountsOffset) / zCountsPerUnit;
                samples.sList[i].time = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;

                // are we reversing the y-axis?
                if (Remotes.remoteInfo[remoteIndex].reverseY) samples.sList[i].cal.y *= -1;
            }

            for (int i = 0; i < nSamples; i++)
            {
                // do averaging according to the number selected on the chart page
                Data3DStats stat = samples.TimeAverage(chartPage.nAverage, "CalXYZ", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time,
                    new double[] { stat.aveXcal, stat.aveYcal, stat.aveZcal },
                    new double[] { samples.sList[i].raw.x, samples.sList[i].raw.y, samples.sList[i].raw.z });
            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Write data 
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In MagnetometerData.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\Magnetometer_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\Magnetometer_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time rawX rawY rawZ calX calY calZ aveX({0}) aveY({0}) aveZ({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3} {4} {5:f4} {6:f4} {7:f4} {8:f4} {9:f4} {10:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index, time, rawX, rawY, rawZ, calX, calY, calZ, aveX({0}), aveY({0}), aveZ({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3}, {4}, {5:f4}, {6:f4}, {7:f4}, {8:f4}, {9:f4}, {10:f4}";
                    }


                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        Data3DStats stat = samples.TimeAverage(chartPage.nAverage, "CalXYZ", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw.x, samples.sList[i].raw.y, samples.sList[i].raw.z,
                            samples.sList[i].cal.x, samples.sList[i].cal.y, samples.sList[i].cal.z,
                            stat.aveXcal, stat.aveYcal, stat.aveZcal);
                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save Magnetometer data");
            }

            return writeStatus;
        }

        //==========================================
        // Calculate calibration constants
        public override void CalcCal(CalibrationData cd)
        {
            MessLog.AddMessage("In MagnetometerData.CalcCal()");

            // the calibration works by looking at the vertical component of the earths field. 
            // the minus signs when calculating the scale factor is because the earths field points down
            // (I guess we'll have to re-think all this for users in other parts of the globe...)
            //
            xCountsPerUnit = -(cd.m[0].x - cd.m[1].x) / 2;
            xCountsOffset = (cd.m[0].x + cd.m[1].x) / 2;
            yCountsPerUnit = -(cd.m[2].y - cd.m[3].y) / 2;
            yCountsOffset = (cd.m[2].y + cd.m[3].y) / 2;
            zCountsPerUnit = -(cd.m[4].z - cd.m[5].z) / 2;
            zCountsOffset = (cd.m[4].z + cd.m[5].z) / 2;

            // calculate the ratio of the vertica B field to the total B field 
            // use the measurement that had +z upward as the reference since this seems like the most stable orientation 
            double magBx = Math.Abs(cd.m[4].x - xCountsOffset) / xCountsPerUnit;
            double magBy = Math.Abs(cd.m[4].y - yCountsOffset) / yCountsPerUnit;
            double magBz = Math.Abs(cd.m[4].z - zCountsOffset) / zCountsPerUnit;

            double ratio = Math.Sqrt(magBx * magBx + magBy * magBy + magBz * magBz) / magBz;
            // since the current "%CountsPerUnit" are normalized to B_vertical, and the total field is bigger by the above ratio,
            // we need to rescale the %CountsPerUnit varible by this ratio (we are assuming that the gains are about the same in a 3 directions)

            xCountsPerUnit *= ratio;
            yCountsPerUnit *= ratio;
            zCountsPerUnit *= ratio;

            // save the calibration data
            WriteCal();
        }

        //==========================================
        // Write magnetometer calibration constants
        public override bool WriteCal()
        {
            MessLog.AddMessage("In MagnetometerData.WriteCal()");

            // dont try to save aything if the ID hasnt been read from the remote yet
            if (Remotes.remoteInfo[0].ID == 0)
            {
                MessLog.AddMessage("Remote ID not known: Cant save Magnetometer calibration data");
                return false;
            }

            // figure out filename
            string calFileName = String.Format(@"Calibration\CalData\{0:x6}_Magnetometer.txt", Remotes.remoteInfo[0].ID);

            // write the calibration data
            try
            {
                using (StreamWriter sw = new StreamWriter(calFileName))
                {

                    sw.WriteLine("Magnetometer, {0:f4}, {1:f4}, {2:f4}, {3:f4}, {4:f4}, {5:f4}, {6:f4}, {7:f6}",
                        xCountsPerUnit, xCountsOffset, yCountsPerUnit, yCountsOffset, zCountsPerUnit, zCountsOffset, chartPage.stripChart.samplesPerSecond, chartPage.stripChart.timingShift);
                }
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save Magnetometer calibration data");
                return false;
            }

            return true;
        }

        //==========================================
        // Read magnetometer calibration constants
        public override bool ReadCal()
        {
            MessLog.AddMessage("In MagnetometerData.ReadCal()");

            int remoteID = Remotes.remoteInfo[remoteIndex].ID;
            MessLog.AddMessage(String.Format("RemoteIndex: {0}, RemoteID: 0x{1:x6}", remoteIndex, remoteID));
            
            // dont try to save aything if the ID hasnt been read from the remote yet
            if (remoteID == 0)
            {
                MessLog.AddMessage("Remote ID not known: Cant read Magnetometer calibration data");
                return false;
            }

            // figure out filename
            string calFileName = String.Format(@"Calibration\CalData\{0:x6}_Magnetometer.txt", remoteID);

            // read the calibration data
            //
            try
            {
                using (StreamReader sr = new StreamReader(calFileName))
                {
                    string line = sr.ReadLine();
                    if (line != null)
                    {
                        string[] parts = line.Split(',');
                        if (parts[0].Contains("Magnetometer") && parts.Length == 9)
                        {
                            xCountsPerUnit = Convert.ToDouble(parts[1]);
                            xCountsOffset = Convert.ToDouble(parts[2]);
                            yCountsPerUnit = Convert.ToDouble(parts[3]);
                            yCountsOffset = Convert.ToDouble(parts[4]);
                            zCountsPerUnit = Convert.ToDouble(parts[5]);
                            zCountsOffset = Convert.ToDouble(parts[6]);
                            chartPage.stripChart.samplesPerSecond = Convert.ToDouble(parts[7]);
                            chartPage.stripChart.timingShift = Convert.ToDouble(parts[8]);
                            MessLog.AddMessage("samplesPerSecond " + chartPage.stripChart.samplesPerSecond + ", timingShift " + chartPage.stripChart.timingShift);

                            // this cluge is to fix the calibration constants because I had a mistake for a while that cause the scale factor
                            // to have the wrong sign. It should always be +ve when the earths vertical component is negative (as in Illinois)
                            if (xCountsPerUnit < 0)
                            {
                                MessLog.AddMessage("Flipping sign of Bx calibration scale");
                                xCountsPerUnit *= -1;
                            }
                            if (yCountsPerUnit < 0)
                            {
                                MessLog.AddMessage("Flipping sign of By calibration scale");
                                yCountsPerUnit *= -1;
                            }
                            if (zCountsPerUnit < 0)
                            {
                                MessLog.AddMessage("Flipping sign of Bz calibration scale");
                                zCountsPerUnit *= -1;
                            }
                        }
                    }
                    else
                    {
                        MessLog.AddMessage("Trouble finding magnetometer constants - using some defaults");
                        return false;
                    }
                }
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant read Magnetometer calibration data");
                return false;
            }

            return true;
        }

    }

    #endregion ## magnetometer data ##

    #region ## gyroscope data ##

    //================================================================
    // This class holds data from the gyroscope
    class GyroscopeData : SensorData
    {
        // calibration constants
        public double xCountsPerUnit = 938.7; // 2000 dps = 34.91 rad/s = 0x7FFF
        public double xCountsOffset = -20;
        public double yCountsPerUnit = 938.7;
        public double yCountsOffset = 230;
        public double zCountsPerUnit = 938.7;
        public double zCountsOffset = -80;

        // constructor sets up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {
            active = true;
            samples = new Sample3DCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("Gyroscope");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0.0;

            // define the y axis for each trace
            chartPage.stripChart.traceNameWidth = 100;
            chartPage.stripChart.includeRawValues = false;
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, -40, 40);       // Wx
            chartPage.stripChart.AddStrip(chartPage.thinBluePen, -40, 40);      // Wy
            chartPage.stripChart.AddStrip(chartPage.thinGreenPen, -40, 40);     // Wz

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "Wx(rad/s)", "Wy(rad/s)", "Wz(rad/s)" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 1;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be drawn on the background of the chart
            for (int i = 1; i <= 5; i ++)
            {
                chartPage.stripChart.AddHorizontalLine(i*10, 0, chartPage.fineGreyPen);
                chartPage.stripChart.AddHorizontalLine(-i*10, 0, chartPage.fineGreyPen);
            }

            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

            if (!ReadCal())
                MessLog.AddMessage("Cant read gyroscope calibration  constants - using defaults");

        }

        //================================================================
        // adds data to the GyroscopeData class
        public override void Add(DataFromRemote d)
        {
            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 3; // gyroscope
            int nSamples = d.sensorData[sensType].Count() / 6;
            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // each of the (x,y,z) axes is 2 bytes, MSB then LSB. Cast into Int16 below.
                uint i1 = (uint)(d.sensorData[sensType][i * 6 + 0] << 8) + (uint)d.sensorData[sensType][i * 6 + 1];
                uint i2 = (uint)(d.sensorData[sensType][i * 6 + 2] << 8) + (uint)d.sensorData[sensType][i * 6 + 3];
                uint i3 = (uint)(d.sensorData[sensType][i * 6 + 4] << 8) + (uint)d.sensorData[sensType][i * 6 + 5];

                // create an instance of Sample3D to put data into
                Sample3D s = new Sample3D();
                s.raw.x = (Int16)(-i2);
                s.raw.y = (Int16)(i1);
                s.raw.z = (Int16)(i3);

                // Apply calibration
                s.cal.x = ((double)s.raw.x - xCountsOffset) / xCountsPerUnit;
                s.cal.y = ((double)s.raw.y - yCountsOffset) / yCountsPerUnit;
                s.cal.z = ((double)s.raw.z - zCountsOffset) / zCountsPerUnit;
                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;

                // are we reversing the y-axis?
                if (Remotes.remoteInfo[remoteIndex].reverseY) s.cal.y *= -1;

                samples.Add(s);

                // do averaging according to the number selected on the chart page
                Data3DStats stat = samples.TimeAverage(chartPage.nAverage);

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time, new double[] { stat.aveXcal, stat.aveYcal, stat.aveZcal },
                                                     new double[] { s.raw.x, s.raw.y, s.raw.z });
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In GyroscopeData.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal.x = ((double)samples.sList[i].raw.x - xCountsOffset) / xCountsPerUnit;
                samples.sList[i].cal.y = ((double)samples.sList[i].raw.y - yCountsOffset) / yCountsPerUnit;
                samples.sList[i].cal.z = ((double)samples.sList[i].raw.z - zCountsOffset) / zCountsPerUnit;
                samples.sList[i].time = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;

                // are we reversing the y-axis?
                if (Remotes.remoteInfo[remoteIndex].reverseY) samples.sList[i].cal.y *= -1;
            }

            for (int i = 0; i < nSamples; i++)
            {
                // do averaging according to the number selected on the chart page
                Data3DStats stat = samples.TimeAverage(chartPage.nAverage, "CalXYZ", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time,
                    new double[] { stat.aveXcal, stat.aveYcal, stat.aveZcal },
                    new double[] { samples.sList[i].raw.x, samples.sList[i].raw.y, samples.sList[i].raw.z });
            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In GyroscopeData.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\Gyroscope_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\Gyroscope_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time rawX rawY rawZ calX calY calZ aveX({0}) aveY({0}) aveZ({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3} {4} {5:f4} {6:f4} {7:f4} {8:f4} {9:f4} {10:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index, time, rawX, rawY, rawZ, calX, calY, calZ, aveX({0}), aveY({0}), aveZ({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3}, {4}, {5:f4}, {6:f4}, {7:f4}, {8:f4}, {9:f4}, {10:f4}";
                    }


                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        Data3DStats stat = samples.TimeAverage(chartPage.nAverage, "CalXYZ", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw.x, samples.sList[i].raw.y, samples.sList[i].raw.z,
                            samples.sList[i].cal.x, samples.sList[i].cal.y, samples.sList[i].cal.z,
                            stat.aveXcal, stat.aveYcal, stat.aveZcal);
                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save Gyroscope data");
            }

            return writeStatus;
        }

        //==========================================
        // Calculate calibration constants
        public override void CalcCal(CalibrationData cd)
        {
            MessLog.AddMessage("In GyroscopeData.CalcCal()");

            xCountsOffset = (cd.g[0].x + cd.g[1].x + cd.g[2].x + cd.g[3].x + cd.g[4].x + cd.g[5].x) / 6;
            yCountsOffset = (cd.g[0].y + cd.g[1].y + cd.g[2].y + cd.g[3].y + cd.g[4].y + cd.g[5].y) / 6;
            zCountsOffset = (cd.g[0].z + cd.g[1].z + cd.g[2].z + cd.g[3].z + cd.g[4].z + cd.g[5].z) / 6;

            WriteCal();
        }

        //==========================================
        // Write gyroscope calibration constants
        public override bool WriteCal()
        {
            MessLog.AddMessage("In GyroscopeData.WriteCal()");

            // dont try to save aything if the ID hasnt been read from the remote yet
            if (Remotes.remoteInfo[0].ID == 0)
            {
                MessLog.AddMessage("Remote ID not known: Cant save Gyroscope calibration data");
                return false;
            }

            // figure out filename
            string calFileName = String.Format(@"Calibration\CalData\{0:x6}_Gyroscope.txt", Remotes.remoteInfo[0].ID);

            // write the calibration data
            try
            {
                using (StreamWriter sw = new StreamWriter(calFileName))
                {

                    sw.WriteLine("Gyroscope, {0:f4}, {1:f4}, {2:f4}, {3:f4}, {4:f4}, {5:f4}, {6:f4}, {7:f6}",
                        xCountsPerUnit, xCountsOffset, yCountsPerUnit, yCountsOffset, zCountsPerUnit, zCountsOffset, chartPage.stripChart.samplesPerSecond, chartPage.stripChart.timingShift);
                }
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save Gyroscope calibration data");
                return false;
            }

            return true;
        }

        //==========================================
        // Read gyroscope calibration constants
        public override bool ReadCal()
        {
            MessLog.AddMessage("In GyroscopeData.ReadCal()");

            int remoteID = Remotes.remoteInfo[remoteIndex].ID;
            MessLog.AddMessage(String.Format("RemoteIndex: {0}, RemoteID: 0x{1:x6}", remoteIndex, remoteID));

            // dont try to save aything if the ID hasnt been read from the remote yet
            if (remoteID == 0)
            {
                MessLog.AddMessage("Remote ID not known: Cant read Gyroscope calibration data");
                return false;
            }

            // figure out filename
            string calFileName = String.Format(@"Calibration\CalData\{0:x6}_Gyroscope.txt", remoteID);

            // read the calibration data
            try
            {
                using (StreamReader sr = new StreamReader(calFileName))
                {
                    string line = sr.ReadLine();
                    if (line != null)
                    {
                        string[] parts = line.Split(',');
                        if (parts[0].Contains("Gyroscope") && parts.Length == 9)
                        {
                            //xCountsPerUnit = Convert.ToDouble(parts[1]);
                            xCountsOffset = Convert.ToDouble(parts[2]);
                            //yCountsPerUnit = Convert.ToDouble(parts[3]);
                            yCountsOffset = Convert.ToDouble(parts[4]);
                            //zCountsPerUnit = Convert.ToDouble(parts[5]);
                            zCountsOffset = Convert.ToDouble(parts[6]);
                            chartPage.stripChart.samplesPerSecond = Convert.ToDouble(parts[7]);
                            chartPage.stripChart.timingShift = Convert.ToDouble(parts[8]);
                            MessLog.AddMessage("samplesPerSecond " + chartPage.stripChart.samplesPerSecond + ", timingShift " + chartPage.stripChart.timingShift);
                        }
                    }
                    else
                    {
                        MessLog.AddMessage("Trouble finding Gyroscope constants - using some defaults");
                        return false;
                    }
                }
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant read Gyroscope calibration data");
                return false;
            }

            return true;
        }

    }

    #endregion ## gyroscope data ##

    #region ## force probe data ##

    //================================================================
    // This class holds data from the gyroscope
    class ForceProbeData : SensorData
    {
        new public Sample1DCollection samples; // override the base class

        // calibration constants
        public double countsPerUnit = -120;
        public double countsOffsetNominal = 0x7FF;
        public double countsOffset = 0;

        // constructor sets up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {
            active = true;
            samples = new Sample1DCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("Force Probe");
            chartPage.SetCustomMethod(Custom, "F = 0", "running");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0;

            // define the y axis for the trace
            chartPage.stripChart.traceNameWidth = 70;
            chartPage.stripChart.includeRawValues = true;
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, -5, 5);       // Fy

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "Fy (N)" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 3;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be drawn on the background of the chart
            for (int i = 1; i <= 20; i++)
            {
                chartPage.stripChart.AddHorizontalLine(i, 0, chartPage.fineGreyPen);
                chartPage.stripChart.AddHorizontalLine(-i, 0, chartPage.fineGreyPen);
            }

            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

            if (!ReadCal())
                MessLog.AddMessage("Cant read force probe calibration  constants - using defaults");

        }

        // called when custom button in pushed
        public override void Custom()
        {
            // do averaging according to the number selected on the chart page
            Data1DStats stat = samples.TimeAverage(50);

            countsOffset = stat.aveRaw;
            countsOffset -= countsOffsetNominal;
        }

        //================================================================
        // adds data to the ForceProbeData class
        public override void Add(DataFromRemote d)
        {
            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 8; // force probe
            int nSamples = d.sensorData[sensType].Count() / 2;
            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // decode the MSB - LSB into an unsigned integer
                uint i1 = (uint)(d.sensorData[sensType][i * 2 + 0] << 8) + (uint)d.sensorData[sensType][i * 2 + 1];

                // create an instance of Sample1D to put data into
                Sample1D s = new Sample1D();
                s.raw = (Int16)(i1);

                // Apply calibration
                s.cal = ((double)s.raw - countsOffsetNominal - countsOffset) / countsPerUnit;
                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;

                // are we reversing the y-axis?
                if (Remotes.remoteInfo[remoteIndex].reverseY) s.cal *= -1;

                samples.Add(s);

                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage);

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time, new double[] { stat.aveCal }, new double[] { s.raw });
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In ForceProbeData.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal = ((double)samples.sList[i].raw - countsOffsetNominal - countsOffset) / countsPerUnit;
                samples.sList[i].time = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;

                // are we reversing the y-axis?
                if (Remotes.remoteInfo[remoteIndex].reverseY) samples.sList[i].cal *= -1;

            }

            for (int i = 0; i < nSamples; i++)
            {
                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time, new double[] { stat.aveCal }, new double[] { samples.sList[i].raw });

            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Calculate calibration constants
        public override void CalcCal(CalibrationData cd)
        {
            MessLog.AddMessage("In ForceProbeData.CalcCal()");

            double newtonsPerDevice = 2;

            countsOffset = cd.force[0];
            countsOffset -= countsOffsetNominal;

            countsPerUnit = (cd.force[0] - cd.force[1]) / newtonsPerDevice;

            WriteCal();
        }

        //==========================================
        // Write force probe calibration constants
        public override bool WriteCal()
        {
            MessLog.AddMessage("In ForceProbeData.WriteCal()");

            // dont try to save aything if the ID hasnt been read from the remote yet
            if (Remotes.remoteInfo[0].ID == 0)
            {
                MessLog.AddMessage("Remote ID not known: Cant save Force Probe calibration data");
                return false;
            }

            // figure out filename
            string calFileName = String.Format(@"Calibration\CalData\{0:x6}_Force.txt", Remotes.remoteInfo[0].ID);

            // write the calibration data
            try
            {
                using (StreamWriter sw = new StreamWriter(calFileName))
                {

                    sw.WriteLine("Force, {0:f4}, {1:f4}, {2:f4}, {3:f6}",
                        countsPerUnit, countsOffset, chartPage.stripChart.samplesPerSecond, chartPage.stripChart.timingShift);
                }
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save Force Probe calibration data");
                return false;
            }

            return true;
        }

        //==========================================
        // Read force probe calibration constants
        public override bool ReadCal()
        {
            MessLog.AddMessage("In ForceProbeData.ReadCal()");

            int remoteID = Remotes.remoteInfo[remoteIndex].ID;
            MessLog.AddMessage(String.Format("RemoteIndex: {0}, RemoteID: 0x{1:x6}", remoteIndex, remoteID));
            
            // dont try to save aything if the ID hasnt been read from the remote yet
            if (remoteID == 0)
            {
                MessLog.AddMessage("Remote ID not known: Cant read Force Probe calibration data");
                return false;
            }

            // figure out filename
            string calFileName = String.Format(@"Calibration\CalData\{0:x6}_Force.txt", remoteID);

            // read the calibration data
            try
            {
                using (StreamReader sr = new StreamReader(calFileName))
                {
                    string line = sr.ReadLine();
                    if (line != null)
                    {
                        string[] parts = line.Split(',');
                        if (parts[0].Contains("Force") && parts.Length == 5)
                        {
                            countsPerUnit = Convert.ToDouble(parts[1]);
                            countsOffset = Convert.ToDouble(parts[2]);
                            chartPage.stripChart.samplesPerSecond = Convert.ToDouble(parts[3]);
                            chartPage.stripChart.timingShift = Convert.ToDouble(parts[4]);
                            MessLog.AddMessage("samplesPerSecond " + chartPage.stripChart.samplesPerSecond + ", timingShift " + chartPage.stripChart.timingShift);
                        }
                    }
                    else
                    {
                        MessLog.AddMessage("Trouble finding Force Probe constants - using some defaults");
                        return false;
                    }
                }
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant read Force Probe calibration data");
                return false;
            }

            return true;
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In ForceProbeData.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\Force_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\Force_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time raw cal ave({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3:f4} {4:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index, time, raw, cal, ave({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3:f4}, {4:f4}";
                    }

                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw,
                            samples.sList[i].cal, 
                            stat.aveCal);

                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save Force Probe data");
            }

            return writeStatus;
        }
    }
    #endregion ## force probe data ##

    #region ## wheel encoder data ##

    //================================================================
    // This class holds data from the wheel encoder
    class WheelEncoderData : SensorData
    {
        new public SampleRVACollection samples; // override the base class

        // the encoder measures velocity. Integrate this to find position. 
        public int yRawLocation = 0;

        // calibration constants
        public double countsPerUnitR = 1000;

        // when fitting the velocity to find acceleration, the minimum  
        // number of points on each side of the current one to include
        public int minPointsEachSide = 5;
        public int fitPointsEachSide = 5;

        // the previous velocity measurement
        public int lastVelocityCount = 0;

        // constructor sets up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {
            active = true;
            samples = new SampleRVACollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            int remoteID = Remotes.remoteInfo[remoteIndex].ID;
            if (remoteID < 0x150)
            {
                MessLog.AddMessage(String.Format("Since RemoteIndex: 0x{0:x} is less than 0x150 set wheel calibration to 926 counts/meter", remoteID));
                countsPerUnitR = 926;
            }

            chartPage.Setup("Wheel Rotation");
            chartPage.SetCustomMethod(Custom, "R = 0", "running");
            chartPage.SetRecalibrateMethod(ReCalibrate);


            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0;

            // define the y axis for the trace
            chartPage.stripChart.traceNameWidth = 90;
            chartPage.stripChart.includeRawValues = false;
            chartPage.stripChart.AddStrip(chartPage.thinDarkRedPen, -1, 1, false);  // R (not integrable)
            chartPage.stripChart.AddStrip(chartPage.thinDarkOrangePen, -2, 2);      // V
            chartPage.stripChart.AddStrip(chartPage.thinBluePen, -20, 20);          // A

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "R(m)" , "V(m/s)", "A(m/s/s)"});

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 3;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be drawn on the background of the chart
            for (int i = 1; i <= 20; i++)
            {
                chartPage.stripChart.AddHorizontalLine(i, 1, chartPage.fineGreyPen);
                chartPage.stripChart.AddHorizontalLine(-i, 1, chartPage.fineGreyPen);
            }

            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 1, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;
        }

        // called when custom button in pushed
        public override void Custom()
        {
            yRawLocation = 0;
        }

        //================================================================
        // adds data to the WheelEncoderData class
        public override void Add(DataFromRemote d)
        {
            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 9; // wheel encoder
            int nSamples = d.sensorData[sensType].Count() / 2;

            // the number of points to fit o each side when calculating the acceleration
            fitPointsEachSide = Math.Max(minPointsEachSide, chartPage.nAverage / 2);

            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // decode the MSB - LSB into an unsigned integer - below we will cast this into an Int16.
                uint i1 = (uint)(d.sensorData[sensType][i * 2 + 0] << 8) + (uint)d.sensorData[sensType][i * 2 + 1];

                // create an instance of Sample1D to put data into
                SampleRVA s = new SampleRVA();
                s.raw.dr = (Int16)(i1);    // velocity
                yRawLocation += s.raw.dr;
                s.raw.r = yRawLocation;   // position
                s.raw.dv = s.raw.dr - lastVelocityCount; // count difference that will be used to calculate acceleration
                lastVelocityCount = s.raw.dr;

                // Apply calibration
                s.cal.r = ((double)s.raw.r) / countsPerUnitR;
                s.cal.v = ((double)s.raw.dr) * chartPage.stripChart.samplesPerSecond / countsPerUnitR;
                s.cal.a = ((double)s.raw.dv) * chartPage.stripChart.samplesPerSecond * chartPage.stripChart.samplesPerSecond;

                // are we reversing the y-axis?
                if (Remotes.remoteInfo[remoteIndex].reverseY)
                {
                    s.cal.r *= -1;
                    s.cal.v *= -1;
                    s.cal.a *= -1;
                }

                // add this to the sample list
                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;

                samples.Add(s);

                // now recaculate the acceleration by fitting the previous 2*fitPointsEachSide points along
                // with the current point to a straight line. The factor of 2 is a cheap way to make up for the fact that 
                // we really want to have fitPointsEachSide points on each side of the current point, but we dont know the future.
                // This is recalculated correctly for all poits once the running stops or the user clicks "Recalc"
                int lastSampleIndex = samples.sList.Count() - 1;

                int iFirst = lastSampleIndex - 2*fitPointsEachSide;
                if (iFirst < 0) iFirst = 0;

                if ((lastSampleIndex - iFirst) > 1)
                {
                    samples.sList[lastSampleIndex].cal.a = FitVslope(iFirst, lastSampleIndex);
                    // reverse y direction if needed
                    if (Remotes.remoteInfo[remoteIndex].reverseY)
                        samples.sList[lastSampleIndex].cal.a *= -1;
                   
                }

                // do averaging according to the number selected on the chart page
                DataRVAStats stat = samples.TimeAverage(chartPage.nAverage);

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time, new double[] { stat.aveRcal, stat.aveVcal, stat.aveAcal }, new double[] { s.raw.r, s.raw.dr, s.raw.dv });
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In WheelEncoderData.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            // the number of points to fit o each side when calculating the acceleration
            fitPointsEachSide = Math.Max(minPointsEachSide, chartPage.nAverage / 2);
            MessLog.AddMessage("nAverage: " + chartPage.nAverage + "  fitPointsEachSide: " + fitPointsEachSide);

            // recalculate the calibrated values and the times for each sample
            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal.r = ((double)samples.sList[i].raw.r) / countsPerUnitR;
                samples.sList[i].cal.v = ((double)samples.sList[i].raw.dr) * chartPage.stripChart.samplesPerSecond / countsPerUnitR;
                samples.sList[i].cal.a = ((double)samples.sList[i].raw.dv) * chartPage.stripChart.samplesPerSecond * chartPage.stripChart.samplesPerSecond;
                samples.sList[i].time = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;

                int iFirst = i - fitPointsEachSide;
                if (iFirst < 0) iFirst = 0;

                int iLast = i + fitPointsEachSide;
                if (iLast > (nSamples - 1)) iLast = nSamples - 1;

                samples.sList[i].cal.a = FitVslope(iFirst, iLast);

                if (Remotes.remoteInfo[remoteIndex].reverseY)
                {
                    samples.sList[i].cal.r *= -1;
                    samples.sList[i].cal.v *= -1;
                    samples.sList[i].cal.a *= -1;
                }

            }

            // run through the arrays again to do averaging and plotting
            for (int i = 0; i < samples.sList.Count; i++)
            {
                // do averaging according to the number selected on the chart page
                DataRVAStats stat = samples.TimeAverage(chartPage.nAverage, "CalRVA", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time,
                    new double[] { stat.aveRcal, stat.aveVcal, stat.aveAcal },
                    new double[] { samples.sList[i].raw.r, samples.sList[i].raw.dr, samples.sList[i].raw.dv });
            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In WheelEncoderData.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\Wheel_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\Wheel_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index, time, rawR, rawV, rawA, calR, calV, calA, aveR({0}), aveV({0}), aveA({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3} {4} {5:f4} {6:f4} {7:f4} {8:f4} {9:f4} {10:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index time rawR rawV rawA calR calV calA aveR({0}) aveV({0}) aveA({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3}, {4}, {5:f4}, {6:f4}, {7:f4}, {8:f4}, {9:f4}, {10:f4}";
                    }

                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        DataRVAStats stat = samples.TimeAverage(chartPage.nAverage, "CalRVA", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw.r, samples.sList[i].raw.dr, samples.sList[i].raw.dv,
                            samples.sList[i].cal.r, samples.sList[i].cal.v, samples.sList[i].cal.a,
                            stat.aveRcal, stat.aveVcal, stat.aveAcal);
                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save Wheel Encoder data");
            }

            return writeStatus;
        }

        //==========================================
        // Linear Regression
        public virtual double FitVslope(int iFirst, int iLast)

        {
            double sumOfX = 0;
            double sumOfY = 0;
            double sumOfXSq = 0;
            double sumOfYSq = 0;
            double ssX = 0;
            double ssY = 0;
            double sumCodeviates = 0;
            double sCo = 0;
            double count = iLast - iFirst + 1;

            for (int i  = iFirst; i <= iLast; i++)
            {
                double x = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
                //double x = samples.sList[i].time;
                double y = samples.sList[i].raw.dr * chartPage.stripChart.samplesPerSecond / countsPerUnitR;
                sumCodeviates += x * y;
                sumOfX += x;
                sumOfY += y;
                sumOfXSq += x * x;
                sumOfYSq += y * y;
            }
            ssX = sumOfXSq - ((sumOfX * sumOfX) / count);
            ssY = sumOfYSq - ((sumOfY * sumOfY) / count);
            double RNumerator = (count * sumCodeviates) - (sumOfX * sumOfY);
            double RDenom = (count * sumOfXSq - (sumOfX * sumOfX))
             * (count * sumOfYSq - (sumOfY * sumOfY));
            sCo = sumCodeviates - ((sumOfX * sumOfY) / count);

            double meanX = sumOfX / count;
            double meanY = sumOfY / count;
            double dblR = RNumerator / Math.Sqrt(RDenom);
            double rsquared = dblR * dblR;
            double yintercept = meanY - ((sCo / ssX) * meanX);
            double slope = sCo / ssX;

            return slope;
        }

    }

    #endregion ## wheel encoder data ##

    #region ## barometer data ##

    //================================================================
    // This class holds data from the barometer/thermometer
    class PressureData : SensorData
    {
        new public SamplePTCollection samples; // override the base class

        // calibration constants
        public uint rawA0 = 0x4422;  // these dafaults are from the first unit I tested - they are pretty close
        public uint rawB1 = 0xad63;  // the actual values are read using data.GetCalibration(), which
        public uint rawB2 = 0xbcda;  // overwrites these values so any remote can be used without 
        public uint rawC12 = 0x3ab8; // changing this code

        public bool calculateConstants = true;

        public double calA0 = 0;
        public double calB1 = 0;
        public double calB2 = 0;
        public double calC12 = 0;

        public int nTempAverage = 20; // the number of raw temperature values to average before calculating the pressure

        // constructor sets up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {
            active = true;
            samples = new SamplePTCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("Pressure");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0;

            // define the y axis for the trace
            chartPage.stripChart.traceNameWidth = 120;
            chartPage.stripChart.includeRawValues = false;
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, 75, 125);     // T

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "Pressure (kPa)" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 2;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be draw1n on the background of the chart
            for (int i = 8; i <= 12; i++)
            {
                chartPage.stripChart.AddHorizontalLine(i*10, 0, chartPage.fineGreyPen);
            }

            // make the line at 100 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(100, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

            // get barometer calibration constants
            MessLog.AddMessage("Send command to fetch raw barometer calibration constants");
            byte remoteNumber = (byte)(remoteIndex + 1);
            CPglobal.control.data.GetCalibration(remoteNumber, 4);
            CPglobal.control.data.GetCalibration(remoteNumber, 4);
            CPglobal.control.data.GetCalibration(remoteNumber, 4);
            calculateConstants = true;

        }

        //================================================================
        // adds data to the PressureTemperatureData class
        public override void Add(DataFromRemote d)
        {

            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 4; // barometer/thermometer
            int nSamples = d.sensorData[sensType].Count() / 4;
            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // decode the MSB - LSB into an unsigned integer - below we will cast this into an Int16.
                uint i1 = (uint)(d.sensorData[sensType][i * 4 + 0] << 8) + (uint)d.sensorData[sensType][i * 4 + 1];
                uint i2 = (uint)(d.sensorData[sensType][i * 4 + 2] << 8) + (uint)d.sensorData[sensType][i * 4 + 3];

                // keep only the lowesr 10 bits in each
                i1 = (i1 >> 6) & (uint)0x3FF;
                i2 = (i2 >> 6) & (uint)0x3FF;

                // create an instance of SamplePT to put data into
                SamplePT s = new SamplePT();
                s.raw.P = (Int16)(i1);    // Pressure
                s.raw.T = (Int16)(i2);    // Temperature

                // Apply calibration
                s.cal.P = Pressure(i1, i2);
                s.cal.T = (((double)s.raw.T - 605.75) / -5.35);

                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
                samples.Add(s);

                // do averaging on the calibrated values according to the number selected on the chart page
                DataPTStats stat = samples.TimeAverage(chartPage.nAverage,"CalP");

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time, 
                    new double[] { stat.avePcal },
                    new double[] { s.raw.P });
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In PressureTemperatureData.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal.P = Pressure(samples.sList[i].raw.P, samples.sList[i].raw.T);
                samples.sList[i].cal.T = (((double)samples.sList[i].raw.T - 605.75) / -5.35);
                samples.sList[i].time  = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
            }

            for (int i = 0; i < nSamples; i++)
            {
                // do averaging according to the number selected on the chart page
                DataPTStats stat = samples.TimeAverage(chartPage.nAverage, "CalP", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time, 
                    new double[] { stat.avePcal }, 
                    new double[] { samples.sList[i].raw.P });
            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In PressureTemperatureData.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\Pressure_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\Pressure_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time rawP rawT calP calT aveP({0}) aveT({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3} {4:f4} {5:f4} {6:f4} {7:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index, time, rawP, rawT, calP, calT, aveP({0}), aveT({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3}, {4:f4}, {5:f4}, {6:f4}, {7:f4}";
                    }

                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        DataPTStats stat = samples.TimeAverage(chartPage.nAverage, "CalPT", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw.P, samples.sList[i].raw.T,
                            samples.sList[i].cal.P, samples.sList[i].cal.T,
                            stat.avePcal, stat.aveTcal);

                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant PressureTemperature data");
            }

            return writeStatus;
        }

        //===============================================================
        // turns the bytes read from the calibration registers into 
        // floating point numbers used for calculating the pressure
        public void CalculateCalibrationConstants()
        {
            MessLog.AddMessage("Calculating barometer calibration constants");

            // get the sign of the coefficients
            double signA0 = Math.Sign((Int16)rawA0);
            double signB1 = Math.Sign((Int16)rawB1);
            double signB2 = Math.Sign((Int16)rawB2);
            double signC12 = Math.Sign((Int16)rawC12);

            // get the encoded integer and fractional part of the coefficients
            int absA0  = Math.Abs((Int16)rawA0);
            int absB1  = Math.Abs((Int16)rawB1);
            int absB2  = Math.Abs((Int16)rawB2);
            int absC12 = Math.Abs((Int16)(rawC12>>2)); // only the top 14 bits used here

            // decode the integer and fractional parts and pit it all together
            double integerA0 = (double)(absA0 >> 3);
            double numeratorA0 = (double)(absA0 & 0x7);
            double denominatorA0 = Math.Pow(2,3);
            calA0 = signA0*(integerA0 + numeratorA0 / denominatorA0);

            double integerB1 = (double)(absB1 >> 13);
            double numeratorB1 = (double)(absB1 & 0x1FFF);
            double denominatorB1 = Math.Pow(2, 13);
            calB1 = signB1 * (integerB1 + numeratorB1 / denominatorB1);

            double integerB2 = (double)(absB2 >> 14);
            double numeratorB2 = (double)(absB2 & 0x3FFF);
            double denominatorB2 = Math.Pow(2, 14);
            calB2 = signB2 * (integerB2 + numeratorB2 / denominatorB2);

            double numeratorC12 = (double)(absC12 & 0x1FFF);
            double denominatorC12 = Math.Pow(2, 22);
            calC12 = signC12 * numeratorC12 / denominatorC12;

            string calMessage = String.Format("Barometer A0:{0:f4} B1:{1:f4} B2:{2:f4} C12:{3:f4}", calA0, calB1, calB2, calC12);
            MessLog.AddMessage(calMessage);
        }

        // caluclate absolute pressure in kPa
        public double Pressure(double Padc, double Tadc)
        {
            // the first time through calculate calibrations constants
            if (calculateConstants)
            {
                CalculateCalibrationConstants();
                calculateConstants = false;
            }

            double pComp = calA0 + (calB1 + calC12 * Tadc) * Padc + calB2 * Tadc;
            double p = 50 + pComp * (115 - 50) / 1023;

            return p;

        }

    }

    #endregion ## barometer/thermometer data ##

    #region ## thermometer data ##

    //================================================================
    // This class holds data from the barometer/thermometer
    class ThermometerData : SensorData
    {
        new public SampleTCollection samples; // override the base class

        // default calibration constants (will read actual values from device)
        public double calAt30degrees = 2041; // ADC value at 30 degrees
        public double calAt85degrees = 2426; // ADC valur at 85 degrees
        public double thisSampleRate = 50;   // the sample rate of this particular configutation (Hz) - can vary
        public double rawSampleRate = 400;   // the actual hardware sample rate (Hz)

        // constructor sets up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {
            active = true;
            samples = new SampleTCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("Thermometer");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0;

            // define the y axis for the trace
            chartPage.stripChart.traceNameWidth = 120;
            chartPage.stripChart.includeRawValues = true;
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, -10, 190);     // T

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "Temperature (C)" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 1;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be draw1n on the background of the chart
            for (int i = 0; i < 20; i++)
            {
                chartPage.stripChart.AddHorizontalLine(i*10, 0, chartPage.fineGreyPen);
            }

            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

            // get thermometer calibration constants
            MessLog.AddMessage("Send command to fetch thermometer calibration constants");
            byte remoteNumber = (byte)(remoteIndex + 1);
            CPglobal.control.data.GetCalibration(remoteNumber, 0x1a);
            CPglobal.control.data.GetCalibration(remoteNumber, 0x1a);
            CPglobal.control.data.GetCalibration(remoteNumber, 0x1a);
        }

        //================================================================
        // adds data to the ThermometerData class
        public override void Add(DataFromRemote d)
        {

            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 0x1A; // Thermometer
            int nSamples = d.sensorData[sensType].Count() / 4;
            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // decode the MSB - LSB into an unsigned 32 bit integer.
                uint i1 = (uint)(d.sensorData[sensType][i * 4 + 0] << 24) + (uint)(d.sensorData[sensType][i * 4 + 1] << 16) + 
                          (uint)(d.sensorData[sensType][i * 4 + 2] << 8) + (uint)d.sensorData[sensType][i * 4 + 3];

                // create an instance of SampleT to put data into
                SampleT s = new SampleT();

                // the raw data is oversampled so divide it by the appropriate factor to get the average ADC value for this interval
                s.raw = ((double)i1)*thisSampleRate/rawSampleRate;

                // Apply calibration
                s.cal = 30 + (s.raw - calAt30degrees)*(85-30)/(calAt85degrees-calAt30degrees);

                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
                samples.Add(s);

                // do averaging on the calibrated values according to the number selected on the chart page
                DataTStats stat = samples.TimeAverage(chartPage.nAverage, "Cal");

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time,
                    new double[] { stat.aveTcal },
                    new double[] { s.raw });
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In ThermometerData.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal = 30 + (samples.sList[i].raw - calAt30degrees) * (85 - 30) / (calAt85degrees - calAt30degrees);
                samples.sList[i].time = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
            }

            for (int i = 0; i < nSamples; i++)
            {
                // do averaging according to the number selected on the chart page
                DataTStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time,
                    new double[] { stat.aveTcal },
                    new double[] { samples.sList[i].raw });
            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In PressureTemperatureData.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\Pressure_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\Pressure_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time rawT calT aveT({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3} {4:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index time rawT calT aveT({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3}, {4:f4}";
                    }

                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        DataTStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw,
                            samples.sList[i].cal,
                            stat.aveTcal);

                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant write Thermometer data");
            }

            return writeStatus;
        }


    }

    #endregion ##Thermometer data ##

    #region ## ambient light data ##

    //================================================================
    // This class holds data from the ambient light sensor
    class AmbientLightData : SensorData
    {
        new public Sample1DCollection samples; // override the base class

        // calibration constants
        public double countsPerUnit = 500;
        public double countsOffset = 0;

        // constructor sets up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {
            active = true;
            samples = new Sample1DCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("Ambient Light");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0;

            // define the y axis for the trace
            chartPage.stripChart.includeRawValues = false;
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, -1, 11);       // Fy

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "Light Intensity" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 3;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be drawn on the background of the chart
            for (int i = 1; i <= 10; i++)
                chartPage.stripChart.AddHorizontalLine(i, 0, chartPage.fineGreyPen);

            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

        }

        //================================================================
        // adds data to the AmbientLightData class
        public override void Add(DataFromRemote d)
        {
            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 7; // ambient light
            int nSamples = d.sensorData[sensType].Count() / 2;
            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // decode the MSB - LSB into an unsigned integer
                uint i1 = (uint)(d.sensorData[sensType][i * 2 + 0] << 8) + (uint)d.sensorData[sensType][i * 2 + 1];

                // create an instance of Sample1D to put data into
                Sample1D s = new Sample1D();
                s.raw = (Int16)(i1);

                // Apply calibration
                s.cal = ((double)s.raw - countsOffset) / countsPerUnit;
                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
                samples.Add(s);

                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage);

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time, new double[] { stat.aveCal }, new double[] { s.raw });
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In AmbientLightData.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal = ((double)samples.sList[i].raw - countsOffset) / countsPerUnit;
                samples.sList[i].time = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
            }

            for (int i = 0; i < nSamples; i++)
            {
                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time, new double[] { stat.aveCal }, new double[] { samples.sList[i].raw });

            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In AmbientLightData.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\Light_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\Light_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time raw cal ave({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3:f4} {4:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index, time, raw, cal, ave({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3:f4}, {4:f4}";
                    }

                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw,
                            samples.sList[i].cal,
                            stat.aveCal);

                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save ambient light data");
            }

            return writeStatus;
        }
    }
    #endregion ## ambient light data ##

    #region ## ECG data ##

    //================================================================
    // This class holds data from the ECG
    class ECGData : SensorData
    {

        // calibration constants turns ADC counts into differential voltage (in mV) at input 
        public double vxCountsPerUnit = 2048 * 350 / 1500;  // countsFullScale * Gain / mV Full Scale
        public double vxCountsOffset = 0x7FF;               // referenced to Vcc/2
        public double vyCountsPerUnit = 2048 * 350 / 1500;  // full scale deflection = 1500mV/350 = 4.29 mV
        public double vyCountsOffset = 0x7FF;
        public double vzCountsPerUnit = 2048 * 350 / 1500;
        public double vzCountsOffset = 0x7FF;

        // constructor sets up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {
            active = true;
            samples = new Sample3DCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("ECG");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0.0;

            // define the y axis for each trace
            chartPage.stripChart.traceNameWidth = 100;
            chartPage.stripChart.includeRawValues = false;
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, -2, 2);       // vx
            chartPage.stripChart.AddStrip(chartPage.thinBluePen, -3, 1);      // vy
            chartPage.stripChart.AddStrip(chartPage.thinGreenPen, -1, 3);     // vz

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "V1(mV)", "V2(mV)", "V3(mV)" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 3;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be drawn on the background of the chart
            for (int i = 1; i <= 10; i++)
            {
                chartPage.stripChart.AddHorizontalLine(i / 2, 0, chartPage.fineGreyPen);
                chartPage.stripChart.AddHorizontalLine(-i / 2, 0, chartPage.fineGreyPen);
            }

            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

        }

        //================================================================
        // adds data to the ECGData class
        public override void Add(DataFromRemote d)
        {
            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 10; // ECG
            int nSamples = d.sensorData[sensType].Count() / 6;
            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // each of the (x,y,z) axes is 2 bytes, MSB then LSB. 
                uint i1 = (uint)(d.sensorData[sensType][i * 6 + 0] << 8) + (uint)d.sensorData[sensType][i * 6 + 1];
                uint i2 = (uint)(d.sensorData[sensType][i * 6 + 2] << 8) + (uint)d.sensorData[sensType][i * 6 + 3];
                uint i3 = (uint)(d.sensorData[sensType][i * 6 + 4] << 8) + (uint)d.sensorData[sensType][i * 6 + 5];

                // create an instance of Sample to put data into
                Sample3D s = new Sample3D();
                s.raw.x = (Int16)i1; 
                s.raw.y = (Int16)i2;
                s.raw.z = (Int16)i3;

                // Apply calibration
                s.cal.x = ((double)s.raw.x - vxCountsOffset) / vxCountsPerUnit;
                s.cal.y = ((double)s.raw.y - vyCountsOffset) / vyCountsPerUnit;
                s.cal.z = ((double)s.raw.z - vzCountsOffset) / vzCountsPerUnit;
                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
                samples.Add(s);

                // do averaging according to the number selected on the chart page
                Data3DStats stat = samples.TimeAverage(chartPage.nAverage);

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time, new double[] { stat.aveXcal, stat.aveYcal, stat.aveZcal },
                                                     new double[] { s.raw.x, s.raw.y, s.raw.z });
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In ECGData.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal.x = ((double)samples.sList[i].raw.x - vxCountsOffset) / vxCountsPerUnit;
                samples.sList[i].cal.y = ((double)samples.sList[i].raw.y - vyCountsOffset) / vyCountsPerUnit;
                samples.sList[i].cal.z = ((double)samples.sList[i].raw.z - vzCountsOffset) / vzCountsPerUnit;
                samples.sList[i].time = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
            }

            for (int i = 0; i < nSamples; i++)
            {
                // do averaging according to the number selected on the chart page
                Data3DStats stat = samples.TimeAverage(chartPage.nAverage, "CalXYZ", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time,
                    new double[] { stat.aveXcal, stat.aveYcal, stat.aveZcal },
                    new double[] { samples.sList[i].raw.x, samples.sList[i].raw.y, samples.sList[i].raw.z });
            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In ECGData.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\ECG_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\ECG_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time raw1 raw2 raw3 cal1 cal2 cal3 ave1({0}) ave2({0}) ave3({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3} {4} {5:f4} {6:f4} {7:f4} {8:f4} {9:f4} {10:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index, time, raw1, raw2, raw3, cal1, cal2, cal3, ave1({0}), ave2({0}), ave3({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3}, {4}, {5:f4}, {6:f4}, {7:f4}, {8:f4}, {9:f4}, {10:f4}";
                    }


                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        Data3DStats stat = samples.TimeAverage(chartPage.nAverage, "CalXYZ", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw.x, samples.sList[i].raw.y, samples.sList[i].raw.z,
                            samples.sList[i].cal.x, samples.sList[i].cal.y, samples.sList[i].cal.z,
                            stat.aveXcal, stat.aveYcal, stat.aveZcal);
                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save ECG data");
            }

            return writeStatus;
        }

    }

    #endregion ## ECG data ##

    #region ## microphone data ##

    //================================================================
    // This class holds data from the ambient light sensor
    class MicrophoneData : SensorData
    {
        new public Sample1DCollection samples; // override the base class

        // calibration constants
        public double countsPerUnit = 500;
        public double countsOffset = 0;

        // constructor sets up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {
            active = true;
            samples = new Sample1DCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("Microphone");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0;

            // define the y axis for the trace
            chartPage.stripChart.includeRawValues = true;
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, -1, 11, false);  // not integrable

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "Sound Intensity" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 1;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be drawn on the background of the chart
            for (int i = 1; i < 10; i++)
                chartPage.stripChart.AddHorizontalLine(i, 0, chartPage.fineGreyPen);

            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

        }

        //================================================================
        // adds data to the MicrophoneData class
        public override void Add(DataFromRemote d)
        {
            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 6; // Microphone
            int nSamples = d.sensorData[sensType].Count() / 2;
            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // decode the MSB - LSB into an unsigned integer
                uint i1 = (uint)(d.sensorData[sensType][i * 2 + 0] << 8) + (uint)d.sensorData[sensType][i * 2 + 1];

                // create an instance of Sample1D to put data into
                Sample1D s = new Sample1D();
                s.raw = (Int16)(i1);

                // Apply calibration
                s.cal = ((double)s.raw - countsOffset) / countsPerUnit;
                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
                samples.Add(s);

                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage);

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time, new double[] { stat.aveCal }, new double[] { s.raw });
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In MicrophoneData.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal = ((double)samples.sList[i].raw - countsOffset) / countsPerUnit;
                samples.sList[i].time = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
            }

            for (int i = 0; i < nSamples; i++)
            {
                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time, new double[] { stat.aveCal }, new double[] { samples.sList[i].raw });

            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In MicrophoneData.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\Microphone_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\Microphone_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time raw cal ave({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3:f4} {4:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index, time, raw, cal, ave({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3:f4}, {4:f4}";
                    }

                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw,
                            samples.sList[i].cal,
                            stat.aveCal);

                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save microphone data");
            }

            // just for Lee
            dataFileName = String.Format(@"IOLabData\Microphone_" + unique + ".bin");
            using (BinaryWriter writer = new BinaryWriter(File.Open(dataFileName, FileMode.Create)))
            {
                for (int i = 0; i < samples.sList.Count; i++)
                    writer.Write(samples.sList[i].raw);
            }


            return writeStatus;
        }
    }
    #endregion ## microphone data ##

    #region ## battery data ##

    //================================================================
    // This class holds battery voltage data 
    class BatteryData : SensorData
    {
        new public Sample1DCollection samples; // override the base class

        // calibration constants
        public double countsPerUnit = 682.5;
        public double countsOffset = 0;

        // constructor sets up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {
            active = true;
            samples = new Sample1DCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("Battery");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0;

            // define the y axis for the trace
            chartPage.stripChart.includeRawValues = false;
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, -1, 4); // voltge trace

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "Battery Voltage" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 1;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be drawn on the background of the chart
            for (int i = 1; i < 10; i++)
                chartPage.stripChart.AddHorizontalLine(i, 0, chartPage.fineGreyPen);

            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

        }

        //================================================================
        // adds data to the BatteryData class
        public override void Add(DataFromRemote d)
        {
            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 0x0B; // Battery
            int nSamples = d.sensorData[sensType].Count() / 2;
            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // decode the MSB - LSB into an unsigned integer
                uint i1 = (uint)(d.sensorData[sensType][i * 2 + 0] << 8) + (uint)d.sensorData[sensType][i * 2 + 1];

                // create an instance of Sample1D to put data into
                Sample1D s = new Sample1D();
                s.raw = (Int16)(i1);

                // Apply calibration
                s.cal = ((double)s.raw - countsOffset) / countsPerUnit;
                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
                samples.Add(s);

                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage);

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time, new double[] { stat.aveCal }, new double[] { s.raw });
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In BatteryData.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal = ((double)samples.sList[i].raw - countsOffset) / countsPerUnit;
                samples.sList[i].time = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
            }

            for (int i = 0; i < nSamples; i++)
            {
                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time, new double[] { stat.aveCal }, new double[] { samples.sList[i].raw });

            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In BatteryData.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\Battery_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\Battery_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time raw cal ave({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3:f4} {4:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index, time, raw, cal, ave({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3:f4}, {4:f4}";
                    }

                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw,
                            samples.sList[i].cal,
                            stat.aveCal);

                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save battery data");
            }

            return writeStatus;
        }
    }
    #endregion ## battery data ##

    #region ## high gain data ##

    //================================================================
    // This class holds data from the ambient light sensor
    class HighGainData : SensorData
    {
        new public Sample1DCollection samples; // override the base class

        // calibration constants turns ADC counts into differential voltage (in mV) at input 
        public double countsPerUnit = 2048 * 1400 / 1500;  // countsFullScale * Gain / mV Full Scale
        public double countsOffset = 0x7FF;                // referenced to Vcc/2
                                                           // full scale deflection = 1500mV/1400 = 1.07 mV

        // constructor sets up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {
            active = true;
            samples = new Sample1DCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("High Gain Input");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0;

            // define the y axis for the trace
            chartPage.stripChart.includeRawValues = false;
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, -.4, .4); // voltge trace

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "High Gain Input" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 1;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be drawn on the background of the chart
            for (int i = 1; i <= 11; i++)
            {
                chartPage.stripChart.AddHorizontalLine(i/10, 0, chartPage.fineGreyPen);
                chartPage.stripChart.AddHorizontalLine(-i/10, 0, chartPage.fineGreyPen);
            }
            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

        }

        //================================================================
        // adds data to the HighGainData class
        public override void Add(DataFromRemote d)
        {
            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 0x0C; // High Gain Input
            int nSamples = d.sensorData[sensType].Count() / 2;
            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // decode the MSB - LSB into an unsigned integer
                uint i1 = (uint)(d.sensorData[sensType][i * 2 + 0] << 8) + (uint)d.sensorData[sensType][i * 2 + 1];

                // create an instance of Sample1D to put data into
                Sample1D s = new Sample1D();
                s.raw = (Int16)(i1);

                // Apply calibration
                s.cal = ((double)s.raw - countsOffset) / countsPerUnit;
                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
                samples.Add(s);

                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage);

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time, new double[] { stat.aveCal }, new double[] { s.raw });
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In HighGainData.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal = ((double)samples.sList[i].raw - countsOffset) / countsPerUnit;
                samples.sList[i].time = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
            }

            for (int i = 0; i < nSamples; i++)
            {
                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time, new double[] { stat.aveCal }, new double[] { samples.sList[i].raw });

            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In HighGainData.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\HighGain_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\HighGain_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time raw cal ave({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3:f4} {4:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index, time, raw, cal, ave({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3:f4}, {4:f4}";
                    }

                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw,
                            samples.sList[i].cal,
                            stat.aveCal);

                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant high gain input data");
            }

            return writeStatus;
        }
    }
    #endregion ## high gain data ##

    #region ## digital data ##

    //================================================================
    // This class holds data from the ambient light sensor
    class DigitalData : SensorData
    {
        new public Sample1DCollection samples; // override the base class

        // calibration constants
        public double countsPerUnit = 500;
        public double countsOffsetNominal = 0x7FF;
        public double countsOffset = 0;

        // constructor sets up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {
            active = true;
            samples = new Sample1DCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("Digital Data");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0;

            // define the y axis for the trace
            chartPage.stripChart.traceNameWidth = 70;
            chartPage.stripChart.includeRawValues = true;
            chartPage.stripChart.AddStrip(chartPage.thinBluePen, -1, 10, false);  //Header 1 (all not integrable)
            chartPage.stripChart.AddStrip(chartPage.thinGreenPen, -1, 10, false); //Header 2
            chartPage.stripChart.AddStrip(chartPage.thinBluePen, -1, 10, false);  //Header 3
            chartPage.stripChart.AddStrip(chartPage.thinGreenPen, -1, 10, false); //Header 4
            chartPage.stripChart.AddStrip(chartPage.thinBluePen, -1, 10, false);  //Header 5
            chartPage.stripChart.AddStrip(chartPage.thinGreenPen, -1, 10, false); //Header 6
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, -1, 10, false);        //Button 0
            chartPage.stripChart.AddStrip(chartPage.thinDarkOrangePen, -1, 10, false); //Button 1

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "D1","D2","D3","D4","D5","D6","B0","B1" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 1;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be drawn on the background of the chart
            for (int i = 1; i < 10; i++)
            {
                chartPage.stripChart.AddHorizontalLine(i, 0, chartPage.fineGreyPen);
                chartPage.stripChart.AddHorizontalLine(-i, 0, chartPage.fineGreyPen);
            }
            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

        }

        //================================================================
        // adds data to the DigitalData class
        public override void Add(DataFromRemote d)
        {
            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 0x0D; // Digital Data
            int nSamples = d.sensorData[sensType].Count();
            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // the digital data is packed into one byte
                uint i1 = (uint)(d.sensorData[sensType][i]);

                // create an instance of Sample1D to put data into
                Sample1D s = new Sample1D();
                s.raw = (Int16)(i1);

                // Apply calibration
                s.cal = (double)s.raw;
                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
                samples.Add(s);

                double[] raw = new double[8];
                double[] cal = new double[8];

                for (int bit = 0; bit < 8; bit++)
                {
                    raw[bit] = (double)((s.raw >> bit) & 0x01);
                    cal[bit] = (double)bit + raw[bit] * 0.75;
                }

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time, cal, raw);
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In DigitalData.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                double[] raw = new double[8];
                double[] cal = new double[8];

                for (int bit = 0; bit < 8; bit++)
                {
                    raw[bit] = (double)((samples.sList[i].raw >> bit) & 0x01);
                    cal[bit] = (double)bit + raw[bit] * 0.75;
                }

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time, cal, raw);

            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In DigitalData.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\Digital_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\Digital_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time raw cal ave({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3:f4} {4:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index, time, raw, cal, ave({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3:f4}, {4:f4}";
                    }

                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw,
                            samples.sList[i].cal,
                            stat.aveCal);
                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save digital data");
            }

            return writeStatus;
        }
    }
    #endregion ## digital data ##

    #region ## analog 7 data ##

    //================================================================
    // This class holds analog data from header pin 7
    class Analog7Data : SensorData
    {
        new public Sample1DCollection samples; // override the base class

        // calibration constants
        public double countsPerUnit = 4095/3;
        public double countsOffset = 0;

        // constructor sets up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {
            active = true;
            samples = new Sample1DCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("Analog Input Pin 7");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0;

            // define the y axis for the trace
            chartPage.stripChart.includeRawValues = false;
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, -1, 4); // voltge trace

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "Analog 7" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 1;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be drawn on the background of the chart
            for (int i = 1; i < 5; i++)
                chartPage.stripChart.AddHorizontalLine(i, 0, chartPage.fineGreyPen);

            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

        }

        //================================================================
        // adds data to the Analog7Data class
        public override void Add(DataFromRemote d)
        {
            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 0x15; // Analog 7
            int nSamples = d.sensorData[sensType].Count() / 2;
            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // decode the MSB - LSB into an unsigned integer
                uint i1 = (uint)(d.sensorData[sensType][i * 2 + 0] << 8) + (uint)d.sensorData[sensType][i * 2 + 1];

                // create an instance of Sample1D to put data into
                Sample1D s = new Sample1D();
                s.raw = (Int16)(i1);

                // Apply calibration
                s.cal = ((double)s.raw - countsOffset) / countsPerUnit;
                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
                samples.Add(s);

                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage);

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time, new double[] { stat.aveCal }, new double[] { s.raw });
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In Analog7Data.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal = ((double)samples.sList[i].raw - countsOffset) / countsPerUnit;
                samples.sList[i].time = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
            }

            for (int i = 0; i < nSamples; i++)
            {
                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time, new double[] { stat.aveCal }, new double[] { samples.sList[i].raw });

            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In Analog7Data.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\Analog7_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\Analog7_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time raw cal ave({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3:f4} {4:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index, time, raw, cal, ave({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3:f4}, {4:f4}";
                    }

                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw,
                            samples.sList[i].cal,
                            stat.aveCal);

                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save analog 7 data");
            }

            return writeStatus;
        }
    }
    #endregion ## analog 7 data ##

    #region ## analog 8 data ##

    //================================================================
    // This class holds analog data from header pin 8
    class Analog8Data : SensorData
    {
        new public Sample1DCollection samples; // override the base class

        // calibration constants
        public double countsPerUnit = 4095 / 3;
        public double countsOffset = 0;

        // constructor sets up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {
            active = true;
            samples = new Sample1DCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("Analog Input Pin 8");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0;

            // define the y axis for the trace
            chartPage.stripChart.includeRawValues = false;
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, -1, 4); // voltge trace

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "Analog 8" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 1;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be drawn on the background of the chart
            for (int i = 1; i < 5; i++)
                chartPage.stripChart.AddHorizontalLine(i, 0, chartPage.fineGreyPen);

            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

        }

        //================================================================
        // adds data to the Analog8Data class
        public override void Add(DataFromRemote d)
        {
            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 0x16; // Analog 8
            int nSamples = d.sensorData[sensType].Count() / 2;
            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // decode the MSB - LSB into an unsigned integer
                uint i1 = (uint)(d.sensorData[sensType][i * 2 + 0] << 8) + (uint)d.sensorData[sensType][i * 2 + 1];

                // create an instance of Sample1D to put data into
                Sample1D s = new Sample1D();
                s.raw = (Int16)(i1);

                // Apply calibration
                s.cal = ((double)s.raw - countsOffset) / countsPerUnit;
                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
                samples.Add(s);

                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage);

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time, new double[] { stat.aveCal }, new double[] { s.raw });
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In Analog8Data.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal = ((double)samples.sList[i].raw - countsOffset) / countsPerUnit;
                samples.sList[i].time = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
            }

            for (int i = 0; i < nSamples; i++)
            {
                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time, new double[] { stat.aveCal }, new double[] { samples.sList[i].raw });

            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In Analog8Data.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\Analog8_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\Analog8_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time raw cal ave({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3:f4} {4:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index, time, raw, cal, ave({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3:f4}, {4:f4}";
                    }

                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw,
                            samples.sList[i].cal,
                            stat.aveCal);

                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save analog 8 data");
            }

            return writeStatus;
        }
    }
    #endregion ## analog 8 data ##

    #region ## analog 9 data ##

    //================================================================
    // This class holds analog data from header pin 8
    class Analog9Data : SensorData
    {
        new public Sample1DCollection samples; // override the base class

        // calibration constants
        public double countsPerUnit = 4095 / 3;
        public double countsOffset = 0;

        // constructor sets up sample list and stripchart
        public override void Setup(int nominalSampleRate, int remote = 0)
        {
            active = true;
            samples = new Sample1DCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("Analog Input Pin 9");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0;

            // define the y axis for the trace
            chartPage.stripChart.includeRawValues = false;
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, -1, 4); // voltge trace

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "Analog 9" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 1;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be drawn on the background of the chart
            for (int i = 1; i < 5; i++)
                chartPage.stripChart.AddHorizontalLine(i, 0, chartPage.fineGreyPen);

            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

        }

        //================================================================
        // adds data to the Analog9Data class
        public override void Add(DataFromRemote d)
        {
            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            int sensType = 0x17; // Analog 9
            int nSamples = d.sensorData[sensType].Count() / 2;
            for (int i = 0; i < nSamples; i++)
            {
                // packet timing data
                packets.AnotherSample(i, d);

                // decode the MSB - LSB into an unsigned integer
                uint i1 = (uint)(d.sensorData[sensType][i * 2 + 0] << 8) + (uint)d.sensorData[sensType][i * 2 + 1];

                // create an instance of Sample1D to put data into
                Sample1D s = new Sample1D();
                s.raw = (Int16)(i1);

                // Apply calibration
                s.cal = ((double)s.raw - countsOffset) / countsPerUnit;
                s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
                samples.Add(s);

                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage);

                // add these data to the chart page
                chartPage.stripChart.AddData(s.time, new double[] { stat.aveCal }, new double[] { s.raw });
            }
        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In Analog9Data.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal = ((double)samples.sList[i].raw - countsOffset) / countsPerUnit;
                samples.sList[i].time = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
            }

            for (int i = 0; i < nSamples; i++)
            {
                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time, new double[] { stat.aveCal }, new double[] { samples.sList[i].raw });

            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In Analog9Data.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\Analog9_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\Analog9_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time raw cal ave({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3:f4} {4:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index, time, raw, cal, ave({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3:f4}, {4:f4}";
                    }

                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw,
                            samples.sList[i].cal,
                            stat.aveCal);

                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save analog 9 data");
            }

            return writeStatus;
        }
    }
    #endregion ## analog 9 data ##

    #region ## signal strength ##

    //===============================================================
    // This class holds information about the remote data records themselves
    class RemoteDataInfo : SensorData
    {
        new public Sample1DCollection samples; // override the base class

        public override void Setup(int nominalSampleRate, int remote = 0)
        {

            active = true;
            samples = new Sample1DCollection();
            packets = new PacketTimingCollection();
            chartPage = new ChartPage();
            this.remoteIndex = remote;

            chartPage.Setup("Signal Strength");
            chartPage.SetRecalibrateMethod(ReCalibrate);

            // define the x axis of the chart
            int spanValue = (int)nominalSampleRate * 10;
            chartPage.stripChart.Initialize(spanValue);

            // the nominal sammple rate is passed down during setup and is used to determine the parameters of the chart
            // the actual sample rate is calculated during data acquisition (though it is presumably not too different)
            chartPage.stripChart.samplesPerSecond = nominalSampleRate;
            chartPage.stripChart.timingShift = 0;

            // define the y axis for each trace
            chartPage.stripChart.includeRawValues = false;
            chartPage.stripChart.AddStrip(chartPage.thinRedPen, -20, 100);       // RSSI

            // set the initial names on the checkboxed that enable individual strips
            chartPage.stripChart.SetCheckBoxNames(new string[] { "RSSI" });

            // start the display off averaging the last N measurements to make it smoother
            averageNumber = 1;
            chartPage.SetnAverage(averageNumber);

            // set up the horizontal lines that will be drawn on the background of the chart
            // put a line every 25 from 0 to 250
            for (int i = 25; i < 255; i += 25)
                chartPage.stripChart.AddHorizontalLine(i, 0, chartPage.fineGreyPen);

            // make the line at 0 blacker and thicker
            chartPage.stripChart.AddHorizontalLine(0, 0, chartPage.fineBlackPen);
            chartPage.stripChart.clearBeforeNext = true;

        }

        public override void Add(DataFromRemote d)
        {
            // the elapsed running time in milliseconds
            long elapsedTime = TimingInfo.runTimer.ElapsedMilliseconds;

            // the total number of frames received since we started
            packets.AnotherFrame(elapsedTime);

            // packet timing data
            packets.AnotherSample(0, d);

            // create an instance of Sample1D to put data into
            Sample1D s = new Sample1D();

            // raw RSSI data
            s.raw = d.RSSI;
            // Apply calibration (figure out how some day)
            s.cal = s.raw;

            s.time = (((double)samples.sList.Count()) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
            samples.Add(s);

             // do averaging according to the number selected on the chart page
            Data1DStats stat = samples.TimeAverage(chartPage.nAverage);

            // add these data to the chart page
            chartPage.stripChart.AddData(s.time, new double[] { stat.aveCal }, new double[] { s.raw });

        }

        //============================================
        // recalibrate the data and make a new chart
        public override void ReCalibrate()
        {
            MessLog.AddMessage("In RemoteDataInfo.ReCalibrate:");

            // return if there arent enough samples to do this
            int nSamples = samples.sList.Count;
            if (nSamples < chartPage.nAverage) return;

            AnalyzeTime();
            chartPage.stripChart.Clear(false);

            MessLog.AddMessage("Analysis: nSamples " + nSamples +
                               ", samplesPerSecond " + chartPage.stripChart.samplesPerSecond +
                               ", timingShift " + chartPage.stripChart.timingShift);

            for (int i = 0; i < nSamples; i++)
            {
                samples.sList[i].cal = samples.sList[i].raw;
                samples.sList[i].time = (((double)i) / chartPage.stripChart.samplesPerSecond) - chartPage.stripChart.timingShift;
            }

            for (int i = 0; i < nSamples; i++)
            {
                // do averaging according to the number selected on the chart page
                Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                // add these data to the chart page
                chartPage.stripChart.AddData(samples.sList[i].time, new double[] { stat.aveCal }, new double[] { samples.sList[i].raw });

            }

            chartPage.SetLeftmostToTrackbar();
            chartPage.stripChart.clearBeforeNext = true;
            chartPage.stripChart.Draw();
            
        }

        //==========================================
        // Write data
        public override bool WriteData(string unique, string mode)
        {
            MessLog.AddMessage("In RemoteDataInfo.WriteData()");
            bool writeStatus = false;

            ReCalibrate();

            // figure out the filename
            string dataFileName = "";
            if (mode == "matlab")
                dataFileName = String.Format(@"IOLabData\RSSI_" + unique + ".mat");
            else
                dataFileName = String.Format(@"IOLabData\RSSI_" + unique + ".csv");

            // write the data
            try
            {
                using (StreamWriter sw = new StreamWriter(dataFileName))
                {
                    string formatting = "";
                    if (mode == "matlab")
                    {
                        sw.WriteLine("index time raw cal ave({0})", chartPage.nAverage);
                        formatting = "{0} {1:f4} {2} {3:f4} {4:f4}";
                    }
                    else
                    {
                        sw.WriteLine("index, time, raw, cal, ave({0})", chartPage.nAverage);
                        formatting = "{0}, {1:f4}, {2}, {3:f4}, {4:f4}";
                    }

                    for (int i = 0; i < samples.sList.Count; i++)
                    {
                        // do averaging according to the number selected on the chart page
                        Data1DStats stat = samples.TimeAverage(chartPage.nAverage, "Cal", i);

                        sw.WriteLine(formatting,
                            i, samples.sList[i].time,
                            samples.sList[i].raw,
                            samples.sList[i].cal,
                            stat.aveCal);

                    }
                }
                writeStatus = true;
            }
            catch
            {
                MessLog.AddMessage("Exeption caught: Cant save Signal Strength data");
            }

            return writeStatus;
        }
  
    }

    #endregion ## remote data record info ##

    #region ## calibration specific classes ##

    // This class holds calibration data
    class CalibrationData
    {
        // a, m, g = accelerometer, magnetometer, gyroscope
        // the 6 instances represent x-up, x-dn, y-up, y-dn, z-up, z-dn
        public Vec3Ddouble[] a = new Vec3Ddouble[6] { new Vec3Ddouble(), new Vec3Ddouble(), new Vec3Ddouble(), new Vec3Ddouble(), new Vec3Ddouble(), new Vec3Ddouble() };
        public Vec3Ddouble[] m = new Vec3Ddouble[6] { new Vec3Ddouble(), new Vec3Ddouble(), new Vec3Ddouble(), new Vec3Ddouble(), new Vec3Ddouble(), new Vec3Ddouble() };
        public Vec3Ddouble[] g = new Vec3Ddouble[6] { new Vec3Ddouble(), new Vec3Ddouble(), new Vec3Ddouble(), new Vec3Ddouble(), new Vec3Ddouble(), new Vec3Ddouble() };
        public double[] force = new double[2]{0,0};

        public void FillForce(int orientation, int remote)
        {
            Data1DStats fStats = CPglobal.control.data.R[remote].forceData.samples.TimeAverage(200, "Raw");
            force[orientation] = fStats.aveRaw;
        }

        public void FillAMG(int orientation, int remote)
        {
            // accelerometer
            Data3DStats aStats = CPglobal.control.data.R[remote].accelData.samples.TimeAverage(200, "RawXYZ");
            a[orientation].x = aStats.aveXraw;
            a[orientation].y = aStats.aveYraw;
            a[orientation].z = aStats.aveZraw;

            // magnetometer
            Data3DStats mStats = CPglobal.control.data.R[remote].bfieldData.samples.TimeAverage(200, "RawXYZ");
            m[orientation].x = mStats.aveXraw;
            m[orientation].y = mStats.aveYraw;
            m[orientation].z = mStats.aveZraw;

            // gyroscope
            Data3DStats gStats = CPglobal.control.data.R[remote].gyroData.samples.TimeAverage(200, "RawXYZ");
            g[orientation].x = gStats.aveXraw;
            g[orientation].y = gStats.aveYraw;
            g[orientation].z = gStats.aveZraw;

        }
    }


    #endregion ## calibration specific classes ##

    #region ## other classes that help the sensor classes defined above ##

    //================================================================
    // hold info about records & timing for a given sensor
    class PacketTiming
    {
        public int thisFrame = 0;
        public int thisSample = 0;
        public double thisFrameTimeMS = 0;
    }

    //================================================================
    // holds the packet information list and can do analysis on it
    class PacketTimingCollection
    {
        public double totalFrames = 0;
        public double totalSamples = 0;
        public double elapsedTime = 0;

        public double msPerSample = 0;
        public double msPerFrame = 10;
        public double samplesPerSecond = 0;
        public double framesPerSecond = 0;
        public double samplesPerFrame = 0;

        public List<PacketTiming> packetTimingList = new List<PacketTiming>();

        public void AnotherFrame(long elapsedTime)
        {
            totalFrames++;
            this.elapsedTime = elapsedTime;
        }

        public void AnotherSample(int sample, DataFromRemote d)
        {
            totalSamples++;
            Add(new PacketTiming() { thisFrame = d.frameNumberByte, thisFrameTimeMS = elapsedTime, thisSample = sample + 1 });
        }

        public void Add(PacketTiming pi)
        {
            packetTimingList.Add(pi);
        }

        public void Analyze()
        {
            int len = packetTimingList.Count();
            if (len > 0)
            {
                if (totalSamples > 0)
                    if(CPglobal.control.useFrameTiming)
                        msPerSample = (totalFrames * msPerFrame) / totalSamples;
                    else
                        msPerSample = elapsedTime / totalSamples;
                else
                    msPerSample = 100;

                samplesPerSecond = 1000 / msPerSample;
                framesPerSecond = 1000 / msPerFrame;
                samplesPerFrame = totalSamples / totalFrames;

                MessLog.AddMessage("Analyzing Packets: Samples " + totalSamples + ", Frames " + totalFrames + ", Time " + elapsedTime);
            }
        }

        public void Clear()
        {
            packetTimingList.Clear();
            msPerSample = 0;
            msPerFrame = 0;
            totalFrames = 0;
            totalSamples = 0;
            samplesPerSecond = 0;
            framesPerSecond = 0;
            elapsedTime = 0;
        }

    }

    //================================================================
    // holds the data associated with one 3D sample
    class Sample3D
    {
        public double time = 0;
        public Vec3DInt16 raw = new Vec3DInt16();
        public Vec3Ddouble cal = new Vec3Ddouble();
    }

    //================================================================
    // holds the data associated with one P,T sample
    class SamplePT
    {
        public double time = 0;
        public PTInt16 raw = new PTInt16();
        public PTdouble cal = new PTdouble();
    }

    //================================================================
    // holds the data associated with one Thermometer sample
    class SampleT
    {
        public double time = 0;
        public double raw = 0;
        public double cal = 0;
    }

    //================================================================
    // holds the data associated with one r,v,a sample
    class SampleRVA
    {
        public double time = 0;
        public RVAInt16 raw = new RVAInt16();
        public RVAdouble cal = new RVAdouble();
    }

    //================================================================
    // holds the data associated with one 1D sample
    class Sample1D
    {
        public double time = 0;
        public Int16 raw = 0;
        public double cal = 0;
    }

    //================================================================
    // holds a collection of 1D samples and knows how to do statistics on it
    class Sample1DCollection
    {
        public List<Sample1D> sList = new List<Sample1D>();

        public void Add(Sample1D s)
        {
            sList.Add(s);
        }

        //=============================================================================
        // The "select" string selects which data items are averaged, raw or
        // or calibrated (or both).  
        public Data1DStats TimeAverage(int numberToAverage = 1, string select = "RawCal", int lastIndex = 0)
        {
            // this will hold the results
            Data1DStats results = new Data1DStats();

            // figure out the last index to include in the average.
            int endOfList = sList.Count - 1;
            if ((lastIndex > endOfList) || (lastIndex == 0))
                lastIndex = endOfList;

            // if numberToAverage is 0 or 1 then just return the latest values and bail out
            if ((numberToAverage < 2) && (lastIndex >= 0))
            {
                results.aveRaw = sList[lastIndex].raw;
                results.sigRaw = 0;
                results.aveCal = sList[lastIndex].cal;
                results.sigCal = 0;
                results.nStat = 1;
                return results;
            }

            // if we got here then we will actually do some averaging
            // only do this if there are at least 3 items in the list (so that sigma makes sense)
            if (lastIndex > 1)
            {
                // if lastIndex is the end of the list then start "numberToAverage" to the left of this
                // if lastIndex is not the end of the list then try to spread "numberToAverage" on both sides
                if (lastIndex < endOfList)
                {
                    lastIndex += (numberToAverage / 2);
                    if (lastIndex > endOfList)
                        lastIndex = endOfList;
                }
                int firstIndex = lastIndex - numberToAverage;
                if (firstIndex < 0)
                    firstIndex = 0;

                // see if we want raw data (if not then assume calibrated)
                bool doRaw = select.ToLower().Contains("raw");
                bool doCal = select.ToLower().Contains("cal");

                // first analyze raw data if requested
                if (doRaw)
                {
                    Stat x = new Stat();
                    for (int i = firstIndex; i <= lastIndex; i++)
                        x.Add(sList[i].raw);

                    results.aveRaw = x.Average();
                    results.sigRaw = x.Sigma();
                    results.nStat = x.Number();
                }

                // first analyze raw data if requested
                if (doCal)
                {
                    Stat x = new Stat();
                    for (int i = firstIndex; i <= lastIndex; i++)
                        x.Add(sList[i].cal);

                    results.aveCal = x.Average();
                    results.sigCal = x.Sigma();
                    results.nStat = x.Number();
                }
            }
            return results;

        }//public DataStats TimeAverage
    }

    //================================================================
    // holds a collection of Thermometer samples and knows how to do statistics on it
    class SampleTCollection
    {
        public List<SampleT> sList = new List<SampleT>();

        public void Add(SampleT s)
        {
            sList.Add(s);
        }

        //=============================================================================
        // The "select" string selects which data items are averaged and whether to average raw or
        // or calibrated data. For example, if select = "RawCal" it analyzes both raw and calibtrated variables, 
        // or if select = "Cal" then just the calibrated T variable is analyzed. 
        public DataTStats TimeAverage(int numberToAverage = 1, string select = "RawCal", int lastIndex = 0)
        {
            // this will hold the results
            DataTStats results = new DataTStats();

            // figure out the last index to include in the average.
            int endOfList = sList.Count - 1;
            if ((lastIndex > endOfList) || (lastIndex == 0))
                lastIndex = endOfList;

            // if numberToAverage is 0 or 1 then just return the latest values and bail out
            if ((numberToAverage < 2) && (lastIndex >= 0))
            {
                results.aveTraw = sList[lastIndex].raw;
                results.sigTraw = 0;
                results.aveTcal = sList[lastIndex].cal;
                results.sigTcal = 0;
                results.nStat = 1;
                return results;
            }

            // if we got here then we will actually do some averaging
            // only do this if there are at least 3 items in the list (so that sigma makes sense)
            if (lastIndex > 1)
            {
                // if lastIndex is the end of the list then start "numberToAverage" to the left of this
                // if lastIndex is not the end of the list then try to spread "numberToAverage" on both sides
                if (lastIndex < endOfList)
                {
                    lastIndex += (numberToAverage / 2);
                    if (lastIndex > endOfList)
                        lastIndex = endOfList;
                }
                int firstIndex = lastIndex - numberToAverage;
                if (firstIndex < 0)
                    firstIndex = 0;

                // see if we want raw data (if not then assume calibrated)
                bool doRaw = select.ToLower().Contains("raw");
                bool doCal = select.ToLower().Contains("cal");

                // first analyze raw data if requested
                if (doRaw)
                {
                    Stat t = new Stat();
                    for (int i = firstIndex; i <= lastIndex; i++)
                        t.Add(sList[i].raw);

                    results.aveTraw = t.Average();
                    results.sigTraw = t.Sigma();
                    results.nStat = t.Number();
                }

                // next analyze calibrated data if requested
                if (doCal)
                {
                    Stat t = new Stat();
                    for (int i = firstIndex; i <= lastIndex; i++)
                        t.Add(sList[i].cal);

                    results.aveTcal = t.Average();
                    results.sigTcal = t.Sigma();
                    results.nStat = t.Number();

                }
            }
            return results;

        }//public DataStats TimeAverage
    }

    //================================================================
    // holds a collection of PT samples and knows how to do statistics on it
    class SamplePTCollection
    {
        public List<SamplePT> sList = new List<SamplePT>();

        public void Add(SamplePT s)
        {
            sList.Add(s);
        }

        //=============================================================================
        // The "select" string selects which data items are averaged and whether to average raw or
        // or calibrated data. For example, if select = "RawPT" it analyzes both raw variables, 
        // or if select = "CalT" then just the calibrated T variable is analyzed. 
        public DataPTStats TimeAverage(int numberToAverage = 1, string select = "RawCalPT", int lastIndex = 0)
        {
            // this will hold the results
            DataPTStats results = new DataPTStats();

            // figure out the last index to include in the average.
            int endOfList = sList.Count - 1;
            if ((lastIndex > endOfList) || (lastIndex == 0))
                lastIndex = endOfList;

            // if numberToAverage is 0 or 1 then just return the latest values and bail out
            if ((numberToAverage < 2) && (lastIndex >= 0))
            {
                results.avePraw = sList[lastIndex].raw.P;
                results.aveTraw = sList[lastIndex].raw.T;
                results.sigPraw = 0;
                results.sigTraw = 0;
                results.avePcal = sList[lastIndex].cal.P;
                results.aveTcal = sList[lastIndex].cal.T;
                results.sigPcal = 0;
                results.sigTcal = 0;
                results.nStat = 1;
                return results;
            }

            // if we got here then we will actually do some averaging
            // only do this if there are at least 3 items in the list (so that sigma makes sense)
            if (lastIndex > 1)
            {
                // if lastIndex is the end of the list then start "numberToAverage" to the left of this
                // if lastIndex is not the end of the list then try to spread "numberToAverage" on both sides
                if (lastIndex < endOfList)
                {
                    lastIndex += (numberToAverage / 2);
                    if (lastIndex > endOfList)
                        lastIndex = endOfList;
                }
                int firstIndex = lastIndex - numberToAverage;
                if (firstIndex < 0)
                    firstIndex = 0;

                // see if we want raw data (if not then assume calibrated)
                bool doRaw = select.ToLower().Contains("raw");
                bool doCal = select.ToLower().Contains("cal");
                bool doP = select.ToLower().Contains("p");
                bool doT = select.ToLower().Contains("t");

                // first analyze raw data if requested
                if (doRaw)
                {
                    if (doP)
                    {
                        Stat p = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            p.Add(sList[i].raw.P);

                        results.avePraw = p.Average();
                        results.sigPraw = p.Sigma();
                        results.nStat = p.Number();
                    }
                    if (doT)
                    {
                        Stat t = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            t.Add(sList[i].raw.T);

                        results.aveTraw = t.Average();
                        results.sigTraw = t.Sigma();
                        results.nStat = t.Number();
                    }
                }

                // next analyze calibrated data if requested
                if (doCal)
                {
                    if (doP)
                    {
                        Stat p = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            p.Add(sList[i].cal.P);

                        results.avePcal = p.Average();
                        results.sigPcal = p.Sigma();
                        results.nStat = p.Number();
                    }
                    if (doT)
                    {
                        Stat t = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            t.Add(sList[i].cal.T);

                        results.aveTcal = t.Average();
                        results.sigTcal = t.Sigma();
                        results.nStat = t.Number();
                    }
                }
            }
            return results;

        }//public DataStats TimeAverage
    }

    //================================================================
    // holds a collection of 3D samples and knows how to do statistics on it
    class SampleRVACollection
    {
        public List<SampleRVA> sList = new List<SampleRVA>();

        public void Add(SampleRVA s)
        {
            sList.Add(s);
        }

        //=============================================================================
        // The "select" string selects which data items are averaged and whether to average raw or
        // or calibrated data. For example, if select = "RawRVA" it analyzes all 3 raw variables, 
        // or if select = "CalA" then just the calibrated A variable is analyzed. 
        public DataRVAStats TimeAverage(int numberToAverage = 1, string select = "RawCalRVA", int lastIndex = 0)
        {
            // this will hold the results
            DataRVAStats results = new DataRVAStats();

            // figure out the last index to include in the average.
            int endOfList = sList.Count - 1;
            if ((lastIndex > endOfList) || (lastIndex == 0))
                lastIndex = endOfList;

            // if numberToAverage is 0 or 1 then just return the latest values and bail out
            if ((numberToAverage < 2) && (lastIndex >= 0))
            {
                results.aveRraw = sList[lastIndex].raw.r;
                results.aveVraw = sList[lastIndex].raw.dr;
                results.aveAraw = sList[lastIndex].raw.dv;
                results.sigRraw = 0;
                results.sigVraw = 0;
                results.sigAraw = 0;
                results.aveRcal = sList[lastIndex].cal.r;
                results.aveVcal = sList[lastIndex].cal.v;
                results.aveAcal = sList[lastIndex].cal.a;
                results.sigRcal = 0;
                results.sigVcal = 0;
                results.sigAcal = 0;
                results.nStat = 1;
                return results;
            }

            // if we got here then we will actually do some averaging
            // only do this if there are at least 3 items in the list (so that sigma makes sense)
            if (lastIndex > 1)
            {
                // if lastIndex is the end of the list then start "numberToAverage" to the left of this
                // if lastIndex is not the end of the list then try to spread "numberToAverage" on both sides
                if (lastIndex < endOfList)
                {
                    lastIndex += (numberToAverage / 2);
                    if (lastIndex > endOfList)
                        lastIndex = endOfList;
                }
                int firstIndex = lastIndex - numberToAverage;
                if (firstIndex < 0)
                    firstIndex = 0;

                // see if we want raw data (if not then assume calibrated)
                bool doRaw = select.ToLower().Contains("raw");
                bool doCal = select.ToLower().Contains("cal");
                bool doR = select.ToLower().Contains("r");
                bool doV = select.ToLower().Contains("v");
                bool doA = select.ToLower().Contains("a");

                // first analyze raw data if requested
                if (doRaw)
                {
                    if (doR)
                    {
                        Stat r = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            r.Add(sList[i].raw.r);

                        results.aveRraw = r.Average();
                        results.sigRraw = r.Sigma();
                        results.nStat = r.Number();
                    }
                    if (doV)
                    {
                        Stat v = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            v.Add(sList[i].raw.dr);

                        results.aveVraw = v.Average();
                        results.sigVraw = v.Sigma();
                        results.nStat = v.Number();
                    }
                    if (doA)
                    {
                        Stat a = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            a.Add(sList[i].raw.dv);

                        results.aveAraw = a.Average();
                        results.sigAraw = a.Sigma();
                        results.nStat = a.Number();
                    }
                }

                // next analyze calibrated data if requested
                if (doCal)
                {
                    if (doR)
                    {
                        Stat r = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            r.Add(sList[i].cal.r);

                        results.aveRcal = r.Average();
                        results.sigRcal = r.Sigma();
                        results.nStat = r.Number();
                    }
                    if (doV)
                    {
                        Stat v = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            v.Add(sList[i].cal.v);

                        results.aveVcal = v.Average();
                        results.sigVcal = v.Sigma();
                        results.nStat = v.Number();
                    }
                    if (doA)
                    {
                        Stat a = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            a.Add(sList[i].cal.a);

                        results.aveAcal = a.Average();
                        results.sigAcal = a.Sigma();
                        results.nStat = a.Number();
                    }
                }
            }
            return results;

        }//public DataStats TimeAverage
    }

    //================================================================
    // holds a collection of 3D samples and knows how to do statistics on it
    class Sample3DCollection
    {
        public List<Sample3D> sList = new List<Sample3D>();

        public void Add(Sample3D s)
        {
            sList.Add(s);
        }

        //=============================================================================
        // The "select" string selects which data items are averaged and whether to average raw or
        // or calibrated data. For example, if select = "RawXYZ" it analyzes all 3 raw variables, 
        // or if select = "CalZ" then just the calibrated Z variable is analyzed. 
        public Data3DStats TimeAverage(int numberToAverage = 1, string select = "RawCalXYZ", int lastIndex = 0)
        {
            // this will hold the results
            Data3DStats results = new Data3DStats();

            // figure out the last index to include in the average.
            int endOfList = sList.Count - 1;
            if ((lastIndex > endOfList) || (lastIndex == 0))
                lastIndex = endOfList;

            // if numberToAverage is 0 or 1 then just return the latest values and bail out
            if ((numberToAverage < 2) && (lastIndex >= 0))
            {
                results.aveXraw = sList[lastIndex].raw.x;
                results.aveYraw = sList[lastIndex].raw.y;
                results.aveZraw = sList[lastIndex].raw.z;
                results.sigXraw = 0;
                results.sigYraw = 0;
                results.sigZraw = 0;
                results.aveXcal = sList[lastIndex].cal.x;
                results.aveYcal = sList[lastIndex].cal.y;
                results.aveZcal = sList[lastIndex].cal.z;
                results.sigXcal = 0;
                results.sigYcal = 0;
                results.sigZcal = 0;
                results.nStat = 1;
                return results;
            }

            // if we got here then we will actually do some averaging
            // only do this if there are at least 3 items in the list (so that sigma makes sense)
            if (lastIndex > 1)
            {
                // if lastIndex is the end of the list then start "numberToAverage" to the left of this
                // if lastIndex is not the end of the list then try to spread "numberToAverage" on both sides
                if (lastIndex < endOfList)
                {
                    lastIndex += (numberToAverage / 2);
                    if (lastIndex > endOfList)
                        lastIndex = endOfList;
                }
                int firstIndex = lastIndex - numberToAverage;
                if (firstIndex < 0)
                    firstIndex = 0;

                // see if we want raw data (if not then assume calibrated)
                bool doRaw = select.ToLower().Contains("raw");
                bool doCal = select.ToLower().Contains("cal");
                bool doX = select.ToLower().Contains("x");
                bool doY = select.ToLower().Contains("y");
                bool doZ = select.ToLower().Contains("z");

                // first analyze raw data if requested
                if (doRaw)
                {
                    if (doX)
                    {
                        Stat x = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            x.Add(sList[i].raw.x);

                        results.aveXraw = x.Average();
                        results.sigXraw = x.Sigma();
                        results.nStat = x.Number();
                    }
                    if (doY)
                    {
                        Stat y = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            y.Add(sList[i].raw.y);

                        results.aveYraw = y.Average();
                        results.sigYraw = y.Sigma();
                        results.nStat = y.Number();
                    }
                    if (doZ)
                    {
                        Stat z = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            z.Add(sList[i].raw.z);

                        results.aveZraw = z.Average();
                        results.sigZraw = z.Sigma();
                        results.nStat = z.Number();
                    }
                }

                // next analyze calibrated data if requested
                if (doCal)
                {
                    if (doX)
                    {
                        Stat x = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            x.Add(sList[i].cal.x);

                        results.aveXcal = x.Average();
                        results.sigXcal = x.Sigma();
                        results.nStat = x.Number();
                    }
                    if (doY)
                    {
                        Stat y = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            y.Add(sList[i].cal.y);

                        results.aveYcal = y.Average();
                        results.sigYcal = y.Sigma();
                        results.nStat = y.Number();
                    }
                    if (doZ)
                    {
                        Stat z = new Stat();
                        for (int i = firstIndex; i <= lastIndex; i++)
                            z.Add(sList[i].cal.z);

                        results.aveZcal = z.Average();
                        results.sigZcal = z.Sigma();
                        results.nStat = z.Number();
                    }
                }
            }

            return results;

        }//public DataStats TimeAverage
    }

    //==================================================
    // this class contains time-averaged 1D data
    class Data1DStats
    {
        public double aveRaw = 0;      // raw averages
        public double sigRaw = 0;      // raw standard deviations
        public double aveCal = 0;      // cal averages
        public double sigCal = 0;      // cal standard deviations
        public int nStat = 0;        // how many in sample

    }//class Data1DStats

    //==================================================
    // this class contains time-averaged PT data
    class DataTStats
    {
        public double aveTraw = 0;  // raw averages
        public double sigTraw = 0;  // raw standard deviations
        public double aveTcal = 0;  // cal averages
        public double sigTcal = 0;  // cal standard deviations
        public int nStat = 0;       // how many in sample

    }//class DataPTStats

    //==================================================
    // this class contains time-averaged PT data
    class DataPTStats
    {
        public double avePraw = 0;      // raw averages
        public double aveTraw = 0;
        public double sigPraw = 0;      // raw standard deviations
        public double sigTraw = 0;
        public double avePcal = 0;      // cal averages
        public double aveTcal = 0;
        public double sigPcal = 0;      // cal standard deviations
        public double sigTcal = 0;
        public int nStat = 0;        // how many in sample

    }//class DataPTStats

    //==================================================
    // this class contains time-averaged RVA data
    class DataRVAStats
    {
        public double aveRraw = 0;      // raw averages
        public double aveVraw = 0;
        public double aveAraw = 0;
        public double sigRraw = 0;      // raw standard deviations
        public double sigVraw = 0;
        public double sigAraw = 0;
        public double aveRcal = 0;      // cal averages
        public double aveVcal = 0;
        public double aveAcal = 0;
        public double sigRcal = 0;      // cal standard deviations
        public double sigVcal = 0;
        public double sigAcal = 0;
        public int nStat = 0;        // how many in sample

    }//class DataRVAStats

    //==================================================
    // this class contains time-averaged 3D data
    class Data3DStats
    {
        public double aveXraw = 0;      // raw averages
        public double aveYraw = 0;
        public double aveZraw = 0;
        public double sigXraw = 0;      // raw standard deviations
        public double sigYraw = 0;
        public double sigZraw = 0;
        public double aveXcal = 0;      // cal averages
        public double aveYcal = 0;
        public double aveZcal = 0;
        public double sigXcal = 0;      // cal standard deviations
        public double sigYcal = 0;
        public double sigZcal = 0;
        public int nStat = 0;        // how many in sample

    }//class Data3DStats

    //================================================================
    // This class holds a P,T info in Int16's
    class PTInt16
    {
        public Int16 P = 0;
        public Int16 T = 0;
    }

    //================================================================
    // This class holds a P,T info in double's
    class PTdouble
    {
        public double P = 0;
        public double T = 0;
    }

    //================================================================
    // This class holds a 3D vector of Int16's
    class Vec3DInt16
    {
        public Int16 x = 0;
        public Int16 y = 0;
        public Int16 z = 0;
    }

    //================================================================
    // This class holds a 3D vector of double's
    class Vec3Ddouble
    {
        public double x = 0;
        public double y = 0;
        public double z = 0;
    }

    //================================================================
    // This class holds 3 int numbers representing raw r,v,a
    class RVAInt16
    {
        public int r = 0;
        public int dr = 0;
        public int dv = 0;
    }

    //================================================================
    // This class holds 3 double numbers representing calibtared r,v,a
    class RVAdouble
    {
        public double r = 0;
        public double v = 0;
        public double a = 0;
    }

    //================================================================
    // holds the info associated with one remote data 
    class RemoteDataRecordInfo
    {
        public byte remoteNumber = 0;
        public byte frameNumber = 0;
        public byte RFstats = 0;
        public byte rawRSSI = 0;
        public double calRSSI = 0;
        public double time = 0;
    }


    #endregion ## other classes that help the sensor classes defined above ##

}// namespace
