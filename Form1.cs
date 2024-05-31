using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using ZXing;
using System.IO;
using System.Diagnostics;
using Emgu.CV.CvEnum;
using System.Collections.Generic;
using Emgu.CV.Util;
using Microsoft.VisualBasic.Devices;
using System.Linq;
using System.Threading;
using System.Reflection;
using camera_show.MiniClass;

namespace camera_show {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            editValueCamDiv3 = new EditValueCamDiv3(this);
        }

        private VideoCapture.API captureApi = VideoCapture.API.DShow;

        public SetupPay.FormPay setupPay = new SetupPay.FormPay();
        public SetCamera setCamera;
        public CreateMinMax createMinMax = new CreateMinMax();
        public Flag flag = new Flag();
        public Global global = new Global();
        public FormCancel formCancel = new FormCancel();
        public SetPath setPath = new SetPath();
        public AutoScale autoScale = new AutoScale();
        public ReAdjust reAdjust = new ReAdjust();
        public EditValueCamDiv3 editValueCamDiv3;

        public Rectangle rectSup;
        /// <summary>
        /// For define select address of config gamma and gain
        /// </summary>
        private string address = string.Empty;

        public void SetUpConfig() {
            setupPay.SelectTab = SetupPay.tabPage.TAB1;
            setupPay.set_nameTab(setPath.minmaxCsv);
            setupPay.SelectTab = SetupPay.tabPage.TAB2;
            setupPay.set_nameTab("cameraStep_" + global.stepTest);

            setupPay.setup();

            //มันจำเป็นต้องมองเห็นกันทั้งหมด จึงทำเป็นกรณีพิเศษ
            setCamera = new SetCamera(this);
        }

        private void Form1_Load(object sender, EventArgs e) {
            //int gggg = Convert.ToInt32("edgf");
            GetHead();
            GetStepTest();
            SetAllPath();
            GenFileList();
            GetDebug();
            GetTimeOut();
            GetAutoFocus();

            SetUpConfig();
            ReadFileAddress();
            GetPort();
            WindowCheck();

            GetSetPort();

            if (flag.setPort) {
                SetPortCamera();

            } else {
                OpenCameraList();
            }

            if (flag.closeForm) {
                this.Close();
                return;
            }

            CheckMode();
            ReadConfigCamera();
            ReadConfigFile();
        }

        private void GetHead() {
            global.head = File.ReadAllText(Path.head);
            File.WriteAllText(Path.tricExe, string.Empty);
        }
        private void GetStepTest() {
            try {
                global.stepTest = File.ReadAllText(Path.folder + global.head + Path.getStepTest);

            } catch {
                global.stepTest = Cmd.normal;
            }
        }
        private void SetAllPath() {
            setPath.headTxt = Path.folder + global.head;
            setPath.headCsv = MinMax.head + global.head;
            setPath.minmaxCsv = MinMax.nameFile + global.stepTest;
            setPath.resultTxt = "test_head_" + global.head + "_result.txt";
            setPath.stepCsv = "cameraStep_" + global.stepTest;
        }
        private void GenFileList() {
            if (!File.Exists(Path.list)) {
                File.WriteAllText(Path.list, string.Empty);
            }
        }
        private void GetSetPort() {
            try {
                string setPortSup = File.ReadAllText(Path.setPort);
                File.Delete(Path.setPort);
                flag.setPort = true;
            } catch { }
        }
        private void GetDebug() {
            try {
                flag.debug = Convert.ToBoolean(File.ReadAllText(setPath.headTxt + Path.getDebug));
            } catch { }
        }
        private void GetTimeOut() {
            try {
                global.timeOut = Convert.ToInt32(File.ReadAllText(setPath.headTxt + Path.getTimeOut));
            } catch { }
            SetEditValueCamDiv3();
        }
        private void SetEditValueCamDiv3() {
            editValueCamDiv3.time = Convert.ToInt32(global.timeOut / 3);
            editValueCamDiv3.state1 = false;
            editValueCamDiv3.state2 = false;
        }
        private void GetAutoFocus() {
            try {
                flag.autoFocus = Convert.ToBoolean(File.ReadAllText(setPath.headTxt + Path.getAutoFocus));
            } catch { }
        }
        private void GetPort() {
            try {
                global.port = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.port, setPath.stepCsv));

            } catch {
                global.stepTest = Cmd.normal;
            }
        }
        private void CheckMode() {
            if (global.stepTest.Contains(Cmd.read2d)) {
                flag.read2d = true;
                processToolStripMenuItem.Visible = true;
                digitSNToolStripMenuItem.Visible = true;
                adjustDegreeToolStripMenuItem.Visible = true;
                Application.Idle += Read2dMode;

            } else if (global.stepTest.Contains(Cmd.comPar) || global.stepTest.Contains(Cmd.comPear)) {
                flag.comPear = true;
                addStepComparToolStripMenuItem.Visible = true;
                ctms_RoiCrop_.Visible = true;
                setPath.folderCompare = "../../config/" + global.stepTest + "/";
                if (!Directory.Exists(setPath.folderCompare)) {
                    Directory.CreateDirectory(setPath.folderCompare);
                }
                Application.Idle += CompearImageMode;

            } else if (global.stepTest.Contains(Cmd.checkLed)) {
                flag.checkLed = true;
                addStepLedToolStripMenuItem.Visible = true;
                setPath.folderCheckLed = "../../config/" + global.stepTest + "/";
                if (!Directory.Exists(setPath.folderCheckLed)) {
                    Directory.CreateDirectory(setPath.folderCheckLed);
                }
                Application.Idle += CheckLedMode;

            } else if (global.stepTest.Contains(Cmd.blinkLed)) {
                flag.blinkLed = true;
                setPath.folderBlinkLed = "../../config/" + global.stepTest + "/";
                if (!Directory.Exists(setPath.folderBlinkLed)) {
                    Directory.CreateDirectory(setPath.folderBlinkLed);
                }
                Application.Idle += BlinkLedMode;

            } else {
                setCameraToolStripMenuItem.Visible = false;
                setDebugToolStripMenuItem.Visible = false;
                frameToolStripMenuItem.Visible = false;
                configLeToolStripMenuItem.Visible = false;
                Application.Idle += NormalMode;
            }
        }
        private void WindowCheck() {
            ComputerInfo computerInfo = new ComputerInfo();

            if (computerInfo.OSFullName.Contains("Windows 7")) {
                captureApi = VideoCapture.API.Any;
            }
        }
        private void SetPortCamera() {
            Form formSetPort = new Form();
            formSetPort.Text = Define.setPort;
            formSetPort.Size = new Size(100, 100);
            ComboBox comboBox = new ComboBox();
            comboBox.Size = new Size(60, 7);

            for (int loop = 0; loop < 9; loop++) {
                setCamera.capture = new VideoCapture(loop, captureApi);

                if (setCamera.capture.Width != 0) {
                    comboBox.Items.Add(loop);
                }

                setCamera.capture.Dispose();
            }

            formSetPort.Controls.Add(comboBox);
            formSetPort.ShowDialog();

            if (string.IsNullOrEmpty(comboBox.Text))
            {
                setupPay.write_text(setPath.headCsv + HeadConfig.port, "0", setPath.stepCsv);
            }
            else
            {
                setupPay.write_text(setPath.headCsv + HeadConfig.port, comboBox.Text, setPath.stepCsv);
            }
            setCamera.capture = new VideoCapture(Convert.ToInt32(comboBox.Text), captureApi);
        }
        private void OpenCameraList() {
            formCancel.Show(this);

            WaitFileList();
            this.Text = "[" + global.head + "]." + global.stepTest;
            WaitOpenCamera();

            formCancel.form.Close();
        }
        private void WaitFileList() {
            while (true) {
                if (formCancel.form.IsDisposed) {
                    Application.Exit();
                    return;
                }

                try {
                    string fileList = File.ReadAllText(Path.list);
                    if (fileList == string.Empty) {
                        break;
                    }

                    if (fileList.Substring(0, 1) != global.head) {
                        Thread.Sleep(50);
                        DelaymS(50);
                        continue;
                    }

                } catch {
                    Thread.Sleep(50);
                    DelaymS(50);
                    continue;
                }

                break;
            }
        }
        private void WaitOpenCamera() {
            while (true) {
                if (formCancel.form.IsDisposed) {
                    Application.Exit();
                    return;
                }

                try {
                    setCamera.capture = new VideoCapture(global.port, captureApi);
                    if (address != Address.port)
                    {
                        int gammaCsv = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.gamma, setPath.stepCsv));
                        if (!CheckAddressCamera(gammaCsv))
                        {
                            if (!ScanAddressCamera(gammaCsv))
                            {
                                //ScanAddressCamera(gammaCsv);
                                if (!flag.debug)
                                {
                                    File.WriteAllText(setPath.resultTxt, "Can not find port\r\nFAIL");
                                    this.Close();
                                    Application.Exit();
                                    Application.ExitThread();
                                    Environment.Exit(0);
                                }

                                return;
                            }
                        }
                    }
                    if (setCamera.capture == null || setCamera.capture.Ptr == IntPtr.Zero || setCamera.capture.Width == 0) {
                        DelaymS(50);
                        Thread.Sleep(200);
                        continue;
                    }

                    break;
                } catch { Thread.Sleep(50); }

                DelaymS(50);
                Thread.Sleep(200);
            }
        }
        private void ReadConfigCamera() {
            Frame_height.Text = setupPay.read_text(setPath.headCsv + HeadConfig.frameHeight, setPath.stepCsv);
            Frame_width.Text = setupPay.read_text(setPath.headCsv + HeadConfig.frameWidth, setPath.stepCsv);

            try {
                int zoomSup = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.zoom, setPath.stepCsv));
                int panSup = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.pan, setPath.stepCsv));
                int tiltSup = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.tilt, setPath.stepCsv));
                int contrastSup = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.contrast, setPath.stepCsv));
                int brightnessSup = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.brightness, setPath.stepCsv));
                int focusSup = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.focus, setPath.stepCsv));
                int exposureSup = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.exposure, setPath.stepCsv));
                int saturationSup = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.saturation, setPath.stepCsv));
                int sharpnessSup = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.sharpness, setPath.stepCsv));
                int gainSup = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.gain, setPath.stepCsv));
                //int gammaSup = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.gamma, setPath.stepCsv));

                //TestGetParameter();
                setCamera.SetCapture(CapProp.FrameHeight, Convert.ToInt32(Frame_height.Text));
                setCamera.SetCapture(CapProp.FrameWidth, Convert.ToInt32(Frame_width.Text));
                setCamera.SetCapture(CapProp.Zoom, zoomSup);
                setCamera.SetCapture(CapProp.Pan, panSup);
                setCamera.SetCapture(CapProp.Tilt, tiltSup);
                setCamera.SetCapture(CapProp.Contrast, contrastSup);
                setCamera.SetCapture(CapProp.Brightness, brightnessSup);
                setCamera.SetCapture(CapProp.Focus, focusSup);
                setCamera.SetCapture(CapProp.Exposure, exposureSup);
                setCamera.SetCapture(CapProp.Saturation, saturationSup);
                setCamera.SetCapture(CapProp.Sharpness, sharpnessSup);
                setCamera.SetCapture(CapProp.Gain, gainSup);
                //setCamera.SetCapture(CapProp.Gamma, gammaSup);

            } catch { }

            global.stopWatchHsv.Restart();
            global.stopWatch.Restart();
        }
        private void ReadConfigFile() {
            try {
                setCamera.processInt = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.process, setPath.stepCsv));
                rect.X = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.rectX, setPath.stepCsv));
                rect.Y = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.rectY, setPath.stepCsv));
                rect.Width = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.rectWidth, setPath.stepCsv));
                rect.Height = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.rectHeight, setPath.stepCsv));
                Process_timeout_.Text = setupPay.read_text(setPath.headCsv + HeadConfig.startTimeOut, setPath.stepCsv);
                process_roi_set.Checked = Convert.ToBoolean(setupPay.read_text(setPath.headCsv + HeadConfig.roiMoveSet, setPath.stepCsv));
                Process_roi_.Text = setupPay.read_text(setPath.headCsv + HeadConfig.roiMove, setPath.stepCsv);
                Process_scale_limit_.Text = setupPay.read_text(setPath.headCsv + HeadConfig.autoScaleLimit, setPath.stepCsv);
                Process_scale_next_.Text = setupPay.read_text(setPath.headCsv + HeadConfig.autoScaleNext, setPath.stepCsv);
                global.digitSn = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.digitSN, setPath.stepCsv));
                global.adjustDegree = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.adjustDegree, setPath.stepCsv));
                ctms_setGammaAddressCsv.Text = setupPay.read_text(setPath.headCsv + HeadConfig.gamma, setPath.stepCsv);
            } catch { }


            rectSup = rect;
            ctms_digitSn.Text = global.digitSn.ToString();
            ctms_adjustDegree.Text = global.adjustDegree.ToString();

            if (flag.autoFocus) {
                ctms_focusAutoTrue.Checked = true;
                ctms_focusAutoFalse.Checked = false;
            } else {
                ctms_focusAutoTrue.Checked = false;
                ctms_focusAutoFalse.Checked = true;
            }

            try {
                setCamera.hsvFlag = Convert.ToBoolean(setupPay.read_text(setPath.headCsv + HeadConfig.hsvFlag, setPath.stepCsv));
                setCamera.hsvMask = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.hsvMask, setPath.stepCsv));
                setCamera.hsvTimeout = Convert.ToInt32(setupPay.read_text(setPath.headCsv + HeadConfig.hsvTimeOut, setPath.stepCsv));

                if (setCamera.hsvFlag) {
                    string hsvAll = setupPay.read_text(setPath.headCsv + HeadConfig.hsvFormat, setPath.stepCsv);
                    string[] valueHsv;
                    int[] hsvSplit = new int[6];
                    valueHsv = hsvAll.Split(' ');
                    for (int i = 0; i < 6; i++) {
                        hsvSplit[i] = Convert.ToInt32(valueHsv[i]);
                    }
                    setCamera.hsvLow = new Hsv(hsvSplit[0], hsvSplit[2], hsvSplit[4]);
                    setCamera.hsvHigh = new Hsv(hsvSplit[1], hsvSplit[3], hsvSplit[5]);
                    setCamera.bgrLow = new Bgr();
                    setCamera.bgrHigh = new Bgr();

                } else {
                    string bgrAll = setupPay.read_text(setPath.headCsv + HeadConfig.hsvFormat, setPath.stepCsv);
                    string[] valueBgr;
                    int[] bgrSplit = new int[6];
                    valueBgr = bgrAll.Split(' ');
                    for (int i = 0; i < 6; i++) {
                        bgrSplit[i] = Convert.ToInt32(valueBgr[i]);
                    }
                    setCamera.bgrLow = new Bgr(bgrSplit[0], bgrSplit[2], bgrSplit[4]);
                    setCamera.bgrHigh = new Bgr(bgrSplit[1], bgrSplit[3], bgrSplit[5]);
                    setCamera.hsvLow = new Hsv();
                    setCamera.hsvHigh = new Hsv();
                }
            } catch { }

            this.Text = global.head + "." + global.stepTest;
            this.Size = new Size(setCamera.capture.Width, setCamera.capture.Height);
            pictureBox1.Size = new Size(setCamera.capture.Width, setCamera.capture.Height);

            global.stopWatchHsv.Restart();
            global.stopWatch.Restart();
        }
        /// <summary>
        /// Checlk password of set gamma address
        /// </summary>
        /// <returns>Ture is password success</returns>
        private bool CheckPasswordSetAddress() {
            while (true) {
                KeyPassword form = new KeyPassword();
                DialogResult result = form.ShowDialog();
                if (result != DialogResult.OK) {
                    return false;
                }
                if (form.inputValue != "camera") {
                    MessageBox.Show("Password Error!!");
                    continue;
                }
                break;
            }
            return true;
        }
        /// <summary>
        /// Check address gamma camera in camera and in csv
        /// </summary>
        /// <param name="addressCsv">is gamma address in csv</param>
        /// <returns>True is address success</returns>
        private bool CheckAddressCamera(int addressCsv) {
            double gamma = setCamera.capture.GetCaptureProperty(CapProp.Gamma);
            if (gamma == addressCsv)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Scan find address camera at same address in csv
        /// </summary>
        /// <param name="addressCsv">is gamma address in csv</param>
        private bool ScanAddressCamera(int addressCsv) {
            for (int loop = 0; loop < 100; loop++) {
                try {
                    setCamera.capture.Dispose();
                } catch { }
                //if (loop == global.port) {
                //    continue;
                //}
                setCamera.capture = new VideoCapture(loop, captureApi);
                if (setCamera.capture.Width == 0) {
                    if (flag.debug)
                    {
                        MessageBox.Show("Not find camera address " + address + "[" + addressCsv + "]");
                    }
                    setCamera.capture = new VideoCapture(0, captureApi);
                    return false;
                }
                if (!CheckAddressCamera(addressCsv)) {
                    continue;
                }
                SetPortCameraFollowAddress(loop);
                break;
            }
            return true;
        }
        /// <summary>
        /// Set new port camera follow address in csv
        /// </summary>
        /// <param name="portNew">is new port</param>
        private void SetPortCameraFollowAddress(int portNew) {
            global.port = portNew;
            setupPay.write_text(setPath.headCsv + HeadConfig.port, portNew.ToString(), setPath.stepCsv);
        }
        /// <summary>
        /// Read file set Address
        /// </summary>
        private void ReadFileAddress() {
            address = Address.gamma;
            string headSup = HeadConfig.head + global.head;
            try
            {
                address = setupPay.read_text(headSup + HeadConfig.setAddress, setPath.stepCsv);
            } catch { }
            if (address == Address.gamma)
            {
                ctms_setGammaAddress.Checked = true;
            }
            else if (address == Address.port)
            {
                ctms_usePort.Checked = true;
            }
        }
        /// <summary>
        /// For read value config camera current to csv
        /// </summary>
        private void ReadConfigCameraToCsvFile() {
            string headSup = HeadConfig.head + global.head;

            double zoom = setCamera.capture.GetCaptureProperty(CapProp.Zoom);
            double pan = setCamera.capture.GetCaptureProperty(CapProp.Pan);
            double tilt = setCamera.capture.GetCaptureProperty(CapProp.Tilt);
            double contrast = setCamera.capture.GetCaptureProperty(CapProp.Contrast);
            double brightness = setCamera.capture.GetCaptureProperty(CapProp.Brightness);
            double focus = setCamera.capture.GetCaptureProperty(CapProp.Focus);
            double exposure = setCamera.capture.GetCaptureProperty(CapProp.Exposure);
            double saturation = setCamera.capture.GetCaptureProperty(CapProp.Saturation);
            double sharpness = setCamera.capture.GetCaptureProperty(CapProp.Sharpness);
            double gain = setCamera.capture.GetCaptureProperty(CapProp.Gain);
            double gamma = setCamera.capture.GetCaptureProperty(CapProp.Gamma);

            setupPay.write_text(headSup + HeadConfig.zoom, zoom.ToString(), setPath.stepCsv);
            setupPay.write_text(headSup + HeadConfig.pan, pan.ToString(), setPath.stepCsv);
            setupPay.write_text(headSup + HeadConfig.tilt, tilt.ToString(), setPath.stepCsv);
            setupPay.write_text(headSup + HeadConfig.contrast, contrast.ToString(), setPath.stepCsv);
            setupPay.write_text(headSup + HeadConfig.brightness, brightness.ToString(), setPath.stepCsv);
            setupPay.write_text(headSup + HeadConfig.focus, focus.ToString(), setPath.stepCsv);
            setupPay.write_text(headSup + HeadConfig.exposure, exposure.ToString(), setPath.stepCsv);
            setupPay.write_text(headSup + HeadConfig.saturation, saturation.ToString(), setPath.stepCsv);
            setupPay.write_text(headSup + HeadConfig.sharpness, sharpness.ToString(), setPath.stepCsv);
            setupPay.write_text(headSup + HeadConfig.gain, gain.ToString(), setPath.stepCsv);
            setupPay.write_text(headSup + HeadConfig.gamma, gamma.ToString(), setPath.stepCsv);
        }

        /// <summary>
        /// For test get parameter camera
        /// </summary>
        private void TestGetParameter() {
            double[] test = new double[500];
            test[0] = setCamera.capture.GetCaptureProperty(CapProp.AndroidAntibanding);
            test[1] = setCamera.capture.GetCaptureProperty(CapProp.AndroidExposeLock);
            test[2] = setCamera.capture.GetCaptureProperty(CapProp.AndroidFlashMode);
            test[3] = setCamera.capture.GetCaptureProperty(CapProp.AndroidFocalLength);
            test[4] = setCamera.capture.GetCaptureProperty(CapProp.AndroidFocusDistanceFar);
            test[5] = setCamera.capture.GetCaptureProperty(CapProp.AndroidFocusDistanceNear);
            test[6] = setCamera.capture.GetCaptureProperty(CapProp.AndroidFocusDistanceOptimal);
            test[7] = setCamera.capture.GetCaptureProperty(CapProp.AndroidFocusMode);
            test[8] = setCamera.capture.GetCaptureProperty(CapProp.AndroidWhiteBalance);
            test[9] = setCamera.capture.GetCaptureProperty(CapProp.AndroidWhitebalanceLock);
            test[10] = setCamera.capture.GetCaptureProperty(CapProp.AutoExposure);
            test[11] = setCamera.capture.GetCaptureProperty(CapProp.Autofocus);
            test[12] = setCamera.capture.GetCaptureProperty(CapProp.Autograb);
            test[13] = setCamera.capture.GetCaptureProperty(CapProp.AutoWb);
            test[14] = setCamera.capture.GetCaptureProperty(CapProp.Backend);
            test[15] = setCamera.capture.GetCaptureProperty(CapProp.Backlight);
            test[16] = setCamera.capture.GetCaptureProperty(CapProp.Brightness);
            test[17] = setCamera.capture.GetCaptureProperty(CapProp.Buffersuze);
            test[18] = setCamera.capture.GetCaptureProperty(CapProp.Channel);
            test[19] = setCamera.capture.GetCaptureProperty(CapProp.Contrast);
            test[20] = setCamera.capture.GetCaptureProperty(CapProp.ConvertRgb);
            test[21] = setCamera.capture.GetCaptureProperty(CapProp.DC1394ModeAuto);
            test[22] = setCamera.capture.GetCaptureProperty(CapProp.DC1394ModeManual);
            test[23] = setCamera.capture.GetCaptureProperty(CapProp.DC1394ModeOnePushAuto);
            test[24] = setCamera.capture.GetCaptureProperty(CapProp.DC1394Off);
            test[25] = setCamera.capture.GetCaptureProperty(CapProp.Exposure);
            test[26] = setCamera.capture.GetCaptureProperty(CapProp.Focus);
            test[27] = setCamera.capture.GetCaptureProperty(CapProp.Format);
            test[28] = setCamera.capture.GetCaptureProperty(CapProp.FourCC);
            test[29] = setCamera.capture.GetCaptureProperty(CapProp.Fps);
            test[30] = setCamera.capture.GetCaptureProperty(CapProp.FrameCount);
            test[31] = setCamera.capture.GetCaptureProperty(CapProp.FrameHeight);
            test[32] = setCamera.capture.GetCaptureProperty(CapProp.FrameWidth);
            test[33] = setCamera.capture.GetCaptureProperty(CapProp.Gain);
            test[34] = setCamera.capture.GetCaptureProperty(CapProp.Gamma);
            test[35] = setCamera.capture.GetCaptureProperty(CapProp.GigaFrameHeighMax);
            test[36] = setCamera.capture.GetCaptureProperty(CapProp.GigaFrameOffsetX);
            test[37] = setCamera.capture.GetCaptureProperty(CapProp.GigaFrameOffsetY);
            test[38] = setCamera.capture.GetCaptureProperty(CapProp.GigaFrameSensHeigh);
            test[39] = setCamera.capture.GetCaptureProperty(CapProp.GigaFrameSensWidth);
            test[40] = setCamera.capture.GetCaptureProperty(CapProp.GigaFrameWidthMax);
            test[41] = setCamera.capture.GetCaptureProperty(CapProp.GstreamerQueueLength);
            test[42] = setCamera.capture.GetCaptureProperty(CapProp.Guid);
            test[43] = setCamera.capture.GetCaptureProperty(CapProp.Hue);
            test[44] = setCamera.capture.GetCaptureProperty(CapProp.IntelpercDepthConfidenceThreshold);
            test[45] = setCamera.capture.GetCaptureProperty(CapProp.IntelpercDepthFocalLengthHorz);
            test[46] = setCamera.capture.GetCaptureProperty(CapProp.IntelpercDepthFocalLengthVert);
            test[47] = setCamera.capture.GetCaptureProperty(CapProp.IntelpercDepthGenerator);
            test[48] = setCamera.capture.GetCaptureProperty(CapProp.IntelpercDepthLowConfidenceValue);
            test[49] = setCamera.capture.GetCaptureProperty(CapProp.IntelpercDepthSaturationValue);
            test[50] = setCamera.capture.GetCaptureProperty(CapProp.IntelpercGeneratorsMask);
            test[51] = setCamera.capture.GetCaptureProperty(CapProp.IntelpercImageGenerator);
            test[52] = setCamera.capture.GetCaptureProperty(CapProp.IntelpercProfileCount);
            test[53] = setCamera.capture.GetCaptureProperty(CapProp.IntelpercProfileIdx);
            test[54] = setCamera.capture.GetCaptureProperty(CapProp.IOSDeviceExposure);
            test[55] = setCamera.capture.GetCaptureProperty(CapProp.IOSDeviceFlash);
            test[56] = setCamera.capture.GetCaptureProperty(CapProp.IOSDeviceFocus);
            test[57] = setCamera.capture.GetCaptureProperty(CapProp.IOSDeviceTorch);
            test[58] = setCamera.capture.GetCaptureProperty(CapProp.IOSDeviceWhitebalance);
            test[59] = setCamera.capture.GetCaptureProperty(CapProp.Iris);
            test[60] = setCamera.capture.GetCaptureProperty(CapProp.IsoSpeed);
            test[61] = setCamera.capture.GetCaptureProperty(CapProp.MaxDC1394);
            test[62] = setCamera.capture.GetCaptureProperty(CapProp.Mode);
            test[63] = setCamera.capture.GetCaptureProperty(CapProp.Monochrome);
            test[64] = setCamera.capture.GetCaptureProperty(CapProp.Openni2Mirror);
            test[65] = setCamera.capture.GetCaptureProperty(CapProp.Openni2Sync);
            test[66] = setCamera.capture.GetCaptureProperty(CapProp.OpenniApproxFrameSync);
            test[67] = setCamera.capture.GetCaptureProperty(CapProp.OpenniBaseline);
            test[68] = setCamera.capture.GetCaptureProperty(CapProp.OpenniCircleBuffer);
            test[69] = setCamera.capture.GetCaptureProperty(CapProp.OpenniDepthGenerator);
            test[70] = setCamera.capture.GetCaptureProperty(CapProp.OpenniDepthGeneratorBaseline);
            test[71] = setCamera.capture.GetCaptureProperty(CapProp.OpenniDepthGeneratorFocalLength);
            test[72] = setCamera.capture.GetCaptureProperty(CapProp.OpenniDepthGeneratorPresent);
            test[73] = setCamera.capture.GetCaptureProperty(CapProp.OpenniDepthGeneratorRegistration);
            test[74] = setCamera.capture.GetCaptureProperty(CapProp.OpenniDepthGeneratorRegistrationOn);
            test[75] = setCamera.capture.GetCaptureProperty(CapProp.OpenniFocalLength);
            test[76] = setCamera.capture.GetCaptureProperty(CapProp.OpenniFrameMaxDepth);
            test[77] = setCamera.capture.GetCaptureProperty(CapProp.OpenniGeneratorPresent);
            test[78] = setCamera.capture.GetCaptureProperty(CapProp.OpenniGeneratorsMask);
            test[79] = setCamera.capture.GetCaptureProperty(CapProp.OpenniImageGenerator);
            test[80] = setCamera.capture.GetCaptureProperty(CapProp.OpenniImageGeneratorOutputMode);
            test[81] = setCamera.capture.GetCaptureProperty(CapProp.OpenniImageGeneratorPresent);
            test[82] = setCamera.capture.GetCaptureProperty(CapProp.OpenniIRGenerator);
            test[83] = setCamera.capture.GetCaptureProperty(CapProp.OpenniIRGeneratorPresent);
            test[84] = setCamera.capture.GetCaptureProperty(CapProp.OpenniMaxBufferSize);
            test[85] = setCamera.capture.GetCaptureProperty(CapProp.OpenniMaxTimeDuration);
            test[86] = setCamera.capture.GetCaptureProperty(CapProp.OpenniOutputMode);
            test[87] = setCamera.capture.GetCaptureProperty(CapProp.OpenniRegistration);
            test[88] = setCamera.capture.GetCaptureProperty(CapProp.OpenniRegistrationOn);
            test[89] = setCamera.capture.GetCaptureProperty(CapProp.Pan);
            test[90] = setCamera.capture.GetCaptureProperty(CapProp.PosAviRatio);
            test[91] = setCamera.capture.GetCaptureProperty(CapProp.PosFrames);
            test[92] = setCamera.capture.GetCaptureProperty(CapProp.PosMsec);
            test[93] = setCamera.capture.GetCaptureProperty(CapProp.PreviewFormat);
            test[94] = setCamera.capture.GetCaptureProperty(CapProp.PvapiBinningX);
            test[95] = setCamera.capture.GetCaptureProperty(CapProp.PvapiBinningY);
            test[96] = setCamera.capture.GetCaptureProperty(CapProp.PvapiDecimationHorizontal);
            test[97] = setCamera.capture.GetCaptureProperty(CapProp.PvapiDecimationVertical);
            test[98] = setCamera.capture.GetCaptureProperty(CapProp.PvapiFrameStartTriggerMode);
            test[99] = setCamera.capture.GetCaptureProperty(CapProp.PvapiMulticastip);
            test[100] = setCamera.capture.GetCaptureProperty(CapProp.PvapiPixelFormat);
            test[101] = setCamera.capture.GetCaptureProperty(CapProp.Rectification);
            test[102] = setCamera.capture.GetCaptureProperty(CapProp.Roll);
            test[103] = setCamera.capture.GetCaptureProperty(CapProp.SarDen);
            test[104] = setCamera.capture.GetCaptureProperty(CapProp.SarNum);
            test[105] = setCamera.capture.GetCaptureProperty(CapProp.Saturation);
            test[106] = setCamera.capture.GetCaptureProperty(CapProp.Settings);
            test[107] = setCamera.capture.GetCaptureProperty(CapProp.Sharpness);
            test[108] = setCamera.capture.GetCaptureProperty(CapProp.SupportedPreviewSizesString);
            test[109] = setCamera.capture.GetCaptureProperty(CapProp.Temperature);
            test[110] = setCamera.capture.GetCaptureProperty(CapProp.Tilt);
            test[111] = setCamera.capture.GetCaptureProperty(CapProp.Trigger);
            test[112] = setCamera.capture.GetCaptureProperty(CapProp.TriggerDelay);
            test[113] = setCamera.capture.GetCaptureProperty(CapProp.WbTemperature);
            test[114] = setCamera.capture.GetCaptureProperty(CapProp.WhiteBalanceBlueU);
            test[115] = setCamera.capture.GetCaptureProperty(CapProp.WhiteBalanceRedV);
            test[116] = setCamera.capture.GetCaptureProperty(CapProp.XiAcqBufferSize);
            test[117] = setCamera.capture.GetCaptureProperty(CapProp.XiAcqBufferSizeUnit);
            test[118] = setCamera.capture.GetCaptureProperty(CapProp.XiAcqFrameBurstCount);
            test[119] = setCamera.capture.GetCaptureProperty(CapProp.XiAcqTimingMode);
            test[120] = setCamera.capture.GetCaptureProperty(CapProp.XiAcqTransportBufferCommit);
            test[121] = setCamera.capture.GetCaptureProperty(CapProp.XiAcqTransportBufferSize);
            test[122] = setCamera.capture.GetCaptureProperty(CapProp.XiAeag);
            test[123] = setCamera.capture.GetCaptureProperty(CapProp.XiAeagLevel);
            test[124] = setCamera.capture.GetCaptureProperty(CapProp.XiAeagRoiHeight);
            test[125] = setCamera.capture.GetCaptureProperty(CapProp.XiAeagRoiOffsetX);
            test[126] = setCamera.capture.GetCaptureProperty(CapProp.XiAeagRoiOffsetY);
            test[127] = setCamera.capture.GetCaptureProperty(CapProp.XiAeagRoiWidth);
            test[128] = setCamera.capture.GetCaptureProperty(CapProp.XiAeMaxLimit);
            test[129] = setCamera.capture.GetCaptureProperty(CapProp.XiAgMaxLimit);
            test[130] = setCamera.capture.GetCaptureProperty(CapProp.XiApplyCms);
            test[131] = setCamera.capture.GetCaptureProperty(CapProp.XiAutoBandwidthCalculation);
            test[132] = setCamera.capture.GetCaptureProperty(CapProp.XiAutoWb);
            test[133] = setCamera.capture.GetCaptureProperty(CapProp.XiAvailableBandwidth);
            test[134] = setCamera.capture.GetCaptureProperty(CapProp.XiBinningHorizontal);
            test[135] = setCamera.capture.GetCaptureProperty(CapProp.XiBinningPattern);
            test[136] = setCamera.capture.GetCaptureProperty(CapProp.XiBinningSelector);
            test[137] = setCamera.capture.GetCaptureProperty(CapProp.XiBinningVertical);
            test[138] = setCamera.capture.GetCaptureProperty(CapProp.XiBpc);
            test[139] = setCamera.capture.GetCaptureProperty(CapProp.XiBufferPolicy);
            test[140] = setCamera.capture.GetCaptureProperty(CapProp.XiBuffersQueueSize);
            test[141] = setCamera.capture.GetCaptureProperty(CapProp.XiCcMatrix00);
            test[142] = setCamera.capture.GetCaptureProperty(CapProp.XiCcMatrix01);
            test[143] = setCamera.capture.GetCaptureProperty(CapProp.XiChipTemp);
            test[144] = setCamera.capture.GetCaptureProperty(CapProp.XiCms);
            test[145] = setCamera.capture.GetCaptureProperty(CapProp.XiColorFilterArray);
            test[146] = setCamera.capture.GetCaptureProperty(CapProp.XiColumnFpnCorrection);
            test[147] = setCamera.capture.GetCaptureProperty(CapProp.XiCooling);
            test[148] = setCamera.capture.GetCaptureProperty(CapProp.XiCounterSelector);
            test[149] = setCamera.capture.GetCaptureProperty(CapProp.XiCounterValue);
            test[150] = setCamera.capture.GetCaptureProperty(CapProp.XiDataFormat);
            test[151] = setCamera.capture.GetCaptureProperty(CapProp.XiDebounceEn);
            test[152] = setCamera.capture.GetCaptureProperty(CapProp.XiDebouncePol);
            test[153] = setCamera.capture.GetCaptureProperty(CapProp.XiDebounceT0);
            test[154] = setCamera.capture.GetCaptureProperty(CapProp.XiDebounceT1);
            test[155] = setCamera.capture.GetCaptureProperty(CapProp.XiDebugLevel);
            test[156] = setCamera.capture.GetCaptureProperty(CapProp.XiDecimationHorizontal);
            test[157] = setCamera.capture.GetCaptureProperty(CapProp.XiDecimationPattern);
            test[158] = setCamera.capture.GetCaptureProperty(CapProp.XiDecimationSelector);
            test[159] = setCamera.capture.GetCaptureProperty(CapProp.XiDecimationVertical);
            test[160] = setCamera.capture.GetCaptureProperty(CapProp.XiDefaultCcMatrix);
            test[161] = setCamera.capture.GetCaptureProperty(CapProp.XiDeviceModelId);
            test[162] = setCamera.capture.GetCaptureProperty(CapProp.XiDeviceReset);
            test[163] = setCamera.capture.GetCaptureProperty(CapProp.XiDeviceSn);
            test[164] = setCamera.capture.GetCaptureProperty(CapProp.XiDownsampling);
            test[165] = setCamera.capture.GetCaptureProperty(CapProp.XiDownsamplingType);
            test[166] = setCamera.capture.GetCaptureProperty(CapProp.XiExposure);
            test[167] = setCamera.capture.GetCaptureProperty(CapProp.XiExposureBurstCount);
            test[168] = setCamera.capture.GetCaptureProperty(CapProp.XiExpPriority);
            test[169] = setCamera.capture.GetCaptureProperty(CapProp.XiFfsAccessKey);
            test[170] = setCamera.capture.GetCaptureProperty(CapProp.XiFfsFileId);
            test[171] = setCamera.capture.GetCaptureProperty(CapProp.XiFfsFileSize);
            test[172] = setCamera.capture.GetCaptureProperty(CapProp.XiFramerate);
            test[173] = setCamera.capture.GetCaptureProperty(CapProp.XiFreeFfsSize);
            test[174] = setCamera.capture.GetCaptureProperty(CapProp.XiGain);
            test[175] = setCamera.capture.GetCaptureProperty(CapProp.XiGainSelector);
            test[176] = setCamera.capture.GetCaptureProperty(CapProp.XiGammac);
            test[177] = setCamera.capture.GetCaptureProperty(CapProp.XiGammay);
            test[178] = setCamera.capture.GetCaptureProperty(CapProp.XiGpiLevel);
            test[179] = setCamera.capture.GetCaptureProperty(CapProp.XiGpiMode);
            test[180] = setCamera.capture.GetCaptureProperty(CapProp.XiGpiSelector);
            test[181] = setCamera.capture.GetCaptureProperty(CapProp.XiGpoMode);
            test[182] = setCamera.capture.GetCaptureProperty(CapProp.XiGpoSelector);
            test[183] = setCamera.capture.GetCaptureProperty(CapProp.XiHdr);
            test[184] = setCamera.capture.GetCaptureProperty(CapProp.XiHdrKneepointCount);
            test[185] = setCamera.capture.GetCaptureProperty(CapProp.XiHdrT1);
            test[186] = setCamera.capture.GetCaptureProperty(CapProp.XiHdrT2);
            test[187] = setCamera.capture.GetCaptureProperty(CapProp.XiHeight);
            test[188] = setCamera.capture.GetCaptureProperty(CapProp.XiHousTemp);
            test[189] = setCamera.capture.GetCaptureProperty(CapProp.XiHwRevision);
            test[190] = setCamera.capture.GetCaptureProperty(CapProp.XiImageBlackLevel);
            test[191] = setCamera.capture.GetCaptureProperty(CapProp.XiImageDataBitDepth);
            test[192] = setCamera.capture.GetCaptureProperty(CapProp.XiImageDataFormat);
            test[193] = setCamera.capture.GetCaptureProperty(CapProp.XiImageDataFormatRgb32Alpha);
            test[194] = setCamera.capture.GetCaptureProperty(CapProp.XiImageIsColor);
            test[195] = setCamera.capture.GetCaptureProperty(CapProp.XiImagePayloadSize);
            test[196] = setCamera.capture.GetCaptureProperty(CapProp.XiIsCooled);
            test[197] = setCamera.capture.GetCaptureProperty(CapProp.XiIsDeviceExist);
            test[198] = setCamera.capture.GetCaptureProperty(CapProp.XiKneepoint1);
            test[199] = setCamera.capture.GetCaptureProperty(CapProp.XiKneepoint2);
            test[200] = setCamera.capture.GetCaptureProperty(CapProp.XiLedMode);
            test[201] = setCamera.capture.GetCaptureProperty(CapProp.XiLedSelector);
            test[202] = setCamera.capture.GetCaptureProperty(CapProp.XiLensApertureValue);
            test[203] = setCamera.capture.GetCaptureProperty(CapProp.XiLensFeature);
            test[204] = setCamera.capture.GetCaptureProperty(CapProp.XiLensFeatureSelector);
            test[205] = setCamera.capture.GetCaptureProperty(CapProp.XiLensFocalLength);
            test[206] = setCamera.capture.GetCaptureProperty(CapProp.XiLensFocusDistance);
            test[207] = setCamera.capture.GetCaptureProperty(CapProp.XiLensFocusMove);
            test[208] = setCamera.capture.GetCaptureProperty(CapProp.XiLensFocusMovementValue);
            test[209] = setCamera.capture.GetCaptureProperty(CapProp.XiLensMode);
            test[210] = setCamera.capture.GetCaptureProperty(CapProp.XiLimitBandwidth);
            test[211] = setCamera.capture.GetCaptureProperty(CapProp.XiLutEn);
            test[212] = setCamera.capture.GetCaptureProperty(CapProp.XiLutIndex);
            test[213] = setCamera.capture.GetCaptureProperty(CapProp.XiLutValue);
            test[214] = setCamera.capture.GetCaptureProperty(CapProp.XiManualWb);
            test[215] = setCamera.capture.GetCaptureProperty(CapProp.XiOffsetX);
            test[216] = setCamera.capture.GetCaptureProperty(CapProp.XiOffsetY);
            test[217] = setCamera.capture.GetCaptureProperty(CapProp.XiOutputDataBitDepth);
            test[218] = setCamera.capture.GetCaptureProperty(CapProp.XiOutputDataPacking);
            test[219] = setCamera.capture.GetCaptureProperty(CapProp.XiOutputDataPackingType);
            test[220] = setCamera.capture.GetCaptureProperty(CapProp.XiRecentFrame);
            test[221] = setCamera.capture.GetCaptureProperty(CapProp.XiRegionMode);
            test[222] = setCamera.capture.GetCaptureProperty(CapProp.XiRegionSelector);
            test[223] = setCamera.capture.GetCaptureProperty(CapProp.XiRowFpnCorrection);
            test[224] = setCamera.capture.GetCaptureProperty(CapProp.XiSensorBoardTemp);
            test[225] = setCamera.capture.GetCaptureProperty(CapProp.XiSensorClockFreqHz);
            test[226] = setCamera.capture.GetCaptureProperty(CapProp.XiSensorClockFreqIndex);
            test[227] = setCamera.capture.GetCaptureProperty(CapProp.XiSensorDataBitDepth);
            test[228] = setCamera.capture.GetCaptureProperty(CapProp.XiSensorFeatureSelector);
            test[229] = setCamera.capture.GetCaptureProperty(CapProp.XiSensorFeatureValue);
            test[230] = setCamera.capture.GetCaptureProperty(CapProp.XiSensorMode);
            test[231] = setCamera.capture.GetCaptureProperty(CapProp.XiSensorOutputChannelCount);
            test[232] = setCamera.capture.GetCaptureProperty(CapProp.XiSensorTaps);
            test[233] = setCamera.capture.GetCaptureProperty(CapProp.XiSharpness);
            test[234] = setCamera.capture.GetCaptureProperty(CapProp.XiShutterType);
            test[235] = setCamera.capture.GetCaptureProperty(CapProp.XiWidth);
            test[236] = setCamera.capture.GetCaptureProperty(CapProp.XiWbKr);
            test[237] = setCamera.capture.GetCaptureProperty(CapProp.XiUsedFfsSize);
            test[238] = setCamera.capture.GetCaptureProperty(CapProp.XiTsRstSource);
            test[239] = setCamera.capture.GetCaptureProperty(CapProp.XiTsRstMode);
            test[240] = setCamera.capture.GetCaptureProperty(CapProp.XiTrgSoftware);
            test[241] = setCamera.capture.GetCaptureProperty(CapProp.XiTrgSelector);
            test[242] = setCamera.capture.GetCaptureProperty(CapProp.XiTrgDelay);
            test[243] = setCamera.capture.GetCaptureProperty(CapProp.XiTransportPixelFormat);
            test[244] = setCamera.capture.GetCaptureProperty(CapProp.XiTimeout);
            test[245] = setCamera.capture.GetCaptureProperty(CapProp.XiTestPatternGeneratorSelector);
        }

        private void NormalMode(object sender, EventArgs e) {
            if (isMouseDown == true) {
                return;
            }

            if (setCamera.capture == null || setCamera.capture.Ptr == IntPtr.Zero || setCamera.capture.Width == 0) {
                try {
                    setCamera.capture.Dispose();
                } catch { }

                try {
                    setCamera.capture = new VideoCapture(global.port, captureApi);
                } catch { }

                Thread.Sleep(250);
                this.Size = new Size(setCamera.capture.Width, setCamera.capture.Height);
                pictureBox1.Size = new Size(setCamera.capture.Width, setCamera.capture.Height);
                ReadConfigCamera();
                return;
            }

            Mat frame;
            try {
                frame = setCamera.capture.QueryFrame();
                global.imgage = frame.ToImage<Bgr, Byte>();

            } catch {
                MessageBox.Show(Define.canNotOpenCamera);
                flag.closeForm = true;
                Application.Exit();
                return;
            }

            pictureBox1.Image = global.imgage.Bitmap;
            Thread.Sleep(100);
        }
        private void Read2dMode(object sender, EventArgs e) {
            if (flag.clearStopWatch) {
                flag.clearStopWatch = false;
                global.stopWatch.Restart();
            }

            if (global.stopWatch.ElapsedMilliseconds >= global.timeOut) {
                StampFailRead2d();
                if (!flag.debug) {
                    return;
                }
            }

            if (isMouseDown) {
                return;
            }

            if (setCamera.capture == null || setCamera.capture.Ptr == IntPtr.Zero || setCamera.capture.Width == 0) {
                try {
                    setCamera.capture.Dispose();
                } catch { }

                try {
                    setCamera.capture = new VideoCapture(global.port, captureApi);
                } catch { }

                Thread.Sleep(250);
                this.Size = new Size(setCamera.capture.Width, setCamera.capture.Height);
                pictureBox1.Size = new Size(setCamera.capture.Width, setCamera.capture.Height);
                ReadConfigCamera();
                flag.clearStopWatch = true;
                return;
            }

            try {
                Mat frame = setCamera.capture.QueryFrame();
                global.imgage = frame.ToImage<Bgr, Byte>();
                Bgr sup = new Bgr();
                global.imgage = global.imgage.Rotate(global.adjustDegree, sup);

            } catch {
                MessageBox.Show(Define.canNotOpenCamera);
                Application.Exit();
                return;
            }

            Graphics graphics = Graphics.FromImage(global.imgage.Bitmap);
            graphics.DrawRectangle(Pens.Red, rect);
            Image<Bgr, byte> imageCut = null;
            imageCut = global.imgage.Copy();

            if (rect.Width == 0) {
                rect.Width = 100;
                rect.Height = 100;
            }

            try {
                imageCut.ROI = rect;
            } catch { }

            Image<Bgr, byte> temp = imageCut.Copy();

            #region Find triangles and rectangles
            double cannyThresholdLinking = 120.0;
            double cannyThreshold = 180.0;
            Image<Gray, byte> imgOutput = temp.Convert<Gray, byte>().ThresholdBinary(new Gray(150), new Gray(255));
            UMat cannyEdges = new UMat();
            CvInvoke.Canny(imgOutput, cannyEdges, cannyThreshold, cannyThresholdLinking);
            List<RotatedRect> boxList = new List<RotatedRect>(); //a box is a rotated rectangle

            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint()) {
                CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                int count = contours.Size;
                for (int i = 0; i < count; i++) {
                    using (VectorOfPoint contour = contours[i])
                    using (VectorOfPoint approxContour = new VectorOfPoint()) {
                        CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.1, true);
                        if (CvInvoke.ContourArea(approxContour, false) > 50) //only consider contours with area greater than 250
                        {
                            if (approxContour.Size == 4) //The contour has 4 vertices.
                            {
                                #region determine if all the angles in the contour are within [80, 100] degree
                                bool isRectangle = true;
                                Point[] pts = approxContour.ToArray();
                                LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                                for (int j = 0; j < edges.Length; j++) {
                                    double angle = Math.Abs(edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));

                                    if (angle < 70 || angle > 100) {
                                        isRectangle = false;
                                        break;
                                    }
                                }
                                #endregion

                                if (isRectangle) {
                                    boxList.Add(CvInvoke.MinAreaRect(approxContour));
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            BarcodeReader reader = new BarcodeReader();
            Result result = reader.Decode(temp.Bitmap);
            Bgr a = new Bgr();

            for (int i = 1; i <= 17; i++) {
                result = reader.Decode(temp.Bitmap);

                if (result != null) {
                    break;
                }

                temp = temp.Rotate(5, a);
            }

            if (result == null) {
                foreach (RotatedRect box in boxList) {
                    Rectangle brect = CvInvoke.BoundingRectangle(new VectorOfPointF(box.GetVertices()));
                    Image<Bgr, byte> img_cut_canny = null;
                    img_cut_canny = temp.Copy();
                    img_cut_canny.ROI = brect;
                    Image<Bgr, byte> temp_canny = img_cut_canny.Copy();

                    for (int i = 1; i <= 17; i++) {
                        result = reader.Decode(temp_canny.Bitmap);

                        if (result != null) {
                            break;
                        }

                        temp_canny = temp_canny.Rotate(i * 5, a);
                        result = reader.Decode(temp_canny.Bitmap);
                    }

                    if (result != null) {
                        break;
                    }
                }
            }

            if (result != null) {
                CvInvoke.PutText(global.imgage, result.Text, new Point(20, 30), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);

                if (result.Text.Length == global.digitSn) {
                    if (!setCamera.flagOpen && !flag.debug) {
                        File.WriteAllText(setPath.resultTxt, result + "\r\nPASS");
                        this.Close();
                    }

                    flag.resultPass = true;
                    global.resultBlackup = result.ToString();

                } else {
                    if (!setCamera.flagOpen) {
                        autoScale.Run(this);
                    }

                    flag.resultPass = false;
                }

            } else {
                CvInvoke.PutText(global.imgage, "not read", new Point(20, 30), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);

                if (!setCamera.flagOpen) {
                    autoScale.Run(this);
                }

                editValueCamDiv3.Process();

                flag.resultPass = false;
            }

            CvInvoke.PutText(global.imgage, boxList.Count.ToString(), new Point(20, 60), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(global.imgage, "Port = " + global.port, new Point(20, 90), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            pictureBox1.Image = global.imgage.Bitmap;
            Thread.Sleep(100);//100
        }
        private void CompearImageMode(object sender, EventArgs e) {
            if (flag.clearStopWatch) {
                flag.clearStopWatch = false;
                global.stopWatch.Restart();
            }

            if (global.stopWatch.ElapsedMilliseconds >= global.timeOut) {
                StampFailCompare();
                if (!flag.debug) {
                    return;
                }
            }

            if (isMouseDown == true) {
                return;
            }

            if (setCamera.capture == null || setCamera.capture.Ptr == IntPtr.Zero || setCamera.capture.Width == 0) {
                try {
                    setCamera.capture.Dispose();
                } catch { }

                try {
                    setCamera.capture = new VideoCapture(global.port, captureApi);
                } catch { }

                Thread.Sleep(250);
                this.Size = new Size(setCamera.capture.Width, setCamera.capture.Height);
                pictureBox1.Size = new Size(setCamera.capture.Width, setCamera.capture.Height);
                ReadConfigCamera();
                flag.clearStopWatch = true;
                return;
            }

            if (setCamera.capture != null && setCamera.capture.Ptr != IntPtr.Zero) {
                Mat frame = setCamera.capture.QueryFrame();

                try {
                    global.imgage = frame.ToImage<Bgr, Byte>();

                } catch {
                    MessageBox.Show(Define.canNotOpenCamera);
                    Application.Exit();
                    return;
                }
            }
            Graphics graphics = Graphics.FromImage(global.imgage.Bitmap);
            graphics.DrawRectangle(Pens.Red, rect);
            Image<Bgr, byte> imageCut = null;
            imageCut = global.imgage.Copy();
            imageCut.ROI = rect;
            Image<Bgr, Byte> imageSup = imageCut.Copy();

            long matchTime;
            try {
                using (Mat modelImage = CvInvoke.Imread(setPath.folderCompare + global.head + "Image" + global.numberCompare + ".png", ImreadModes.Grayscale)) {
                    using (Mat observedImage = imageSup.Mat) {
                        Mat result = DrawMatches.Draw(modelImage, observedImage, out matchTime);
                        CvInvoke.PutText(global.imgage, DrawMatches.get_num_object().ToString(), new Point(20, 30),
                            FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
                        CvInvoke.PutText(global.imgage, "Port = " + global.port, new Point(20, 60), 
                            FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
                        pictureBox1.Image = global.imgage.Bitmap;
                    }
                }

                if (!DrawMatches.get_num_object()) {
                    reAdjust.Run(this);
                    flag.resultPass = false;

                    editValueCamDiv3.Process();

                } else {
                    if (!setCamera.flagOpen && !flag.debug) {
                        if (Convert.ToBoolean(File.ReadAllText(setPath.headTxt + Path.getDebug))) {
                            flag.debug = true;

                        } else {
                            if (!global.stopWatchShow.IsRunning) {
                                global.stopWatchShow.Restart();
                            }

                            if (global.stopWatchShow.ElapsedMilliseconds < 50) {
                                return;
                            }
                        }

                        global.stopWatchShow.Stop();
                        global.numberCompare++;
                        ctms_compareNumber.Text = global.numberCompare.ToString();
                        if (File.Exists(setPath.folderCompare + global.head + "Image" + global.numberCompare + ".png")) {
                            List<string> rectangleX = File.ReadAllLines(setPath.folderCompare + global.head + Path.rectangleX).ToList();
                            List<string> rectangleY = File.ReadAllLines(setPath.folderCompare + global.head + Path.rectangleY).ToList();
                            List<string> rectangleWidth = File.ReadAllLines(setPath.folderCompare + global.head + Path.rectangleWidth).ToList();
                            List<string> rectangleHeight = File.ReadAllLines(setPath.folderCompare + global.head + Path.rectangleHeight).ToList();

                            try {
                                rect.X = Convert.ToInt32(rectangleX[global.numberCompare - 1]);
                                rect.Y = Convert.ToInt32(rectangleY[global.numberCompare - 1]);
                                rect.Width = Convert.ToInt32(rectangleWidth[global.numberCompare - 1]);
                                rect.Height = Convert.ToInt32(rectangleHeight[global.numberCompare - 1]);
                            } catch { }

                            global.numStep++;
                            return;
                        }

                        File.WriteAllText(setPath.resultTxt, "Image Detected\r\nPASS");
                        this.Close();
                    }

                    flag.resultPass = true;
                    global.resultBlackup = "Image Detected";
                }

            } catch {
                CvInvoke.PutText(global.imgage, "Port = " + global.port, new Point(20, 60), 
                    FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
                pictureBox1.Image = global.imgage.Bitmap;
            }

            Thread.Sleep(250);//250
        }
        private void CheckLedMode_(object sender, EventArgs e) {
            if (flag.clearStopWatch) {
                flag.clearStopWatch = false;
                global.stopWatch.Restart();
            }

            if (global.stopWatch.ElapsedMilliseconds >= global.timeOut) {
                StampFailCheckLed();
                if (!flag.debug) {
                    return;
                }
            }

            if (isMouseDown) {
                return;
            }

            if (setCamera.capture == null || setCamera.capture.Ptr == IntPtr.Zero || setCamera.capture.Width == 0) {
                try {
                    setCamera.capture.Dispose();
                } catch { }

                try {
                    setCamera.capture = new VideoCapture(global.port, captureApi);
                } catch { }

                Thread.Sleep(250);
                this.Size = new Size(setCamera.capture.Width, setCamera.capture.Height);
                pictureBox1.Size = new Size(setCamera.capture.Width, setCamera.capture.Height);
                ReadConfigCamera();
                flag.clearStopWatch = true;
                return;
            }

            Mat frame;
            try {
                if (!setCamera.hsvTestFlag) {
                    frame = setCamera.capture.QueryFrame();

                } else {
                    frame = new Mat("../../config/hsv_test.png");
                }

                global.imgage = frame.ToImage<Bgr, Byte>();
                global.imgageHsv = frame.ToImage<Hsv, Byte>();

            } catch {
                MessageBox.Show(Define.canNotOpenCamera);
                Application.Exit();
                return;
            }

            Graphics graphics = Graphics.FromImage(global.imgage.Bitmap);
            graphics.DrawRectangle(Pens.Red, rect);
            Image<Bgr, byte> imageCut = null;
            Image<Hsv, byte> imageCutHsv = null;
            Image<Bgr, byte> imageSup = null;
            Image<Hsv, byte> imageSupHsv = null;
            int redpixels = 0;

            if (!setCamera.hsvFlag) {
                imageCut = global.imgage.Copy();
                try {
                    imageCut.ROI = rect;
                } catch { }

                imageSup = imageCut.Copy();
                try {
                    redpixels = imageSup.InRange(setCamera.bgrLow, setCamera.bgrHigh).CountNonzero()[0];
                } catch { }

            } else {
                imageCutHsv = global.imgageHsv.Copy();
                try {
                    imageCutHsv.ROI = rect;
                } catch { }

                imageSupHsv = imageCutHsv.Copy();
                try {
                    redpixels = imageSupHsv.InRange(setCamera.hsvLow, setCamera.hsvHigh).CountNonzero()[0];
                } catch { }
            }

            bool mask = false;
            if (redpixels >= setCamera.hsvMask) {
                if (global.stopWatchHsv.ElapsedMilliseconds >= setCamera.hsvTimeout) {
                    mask = true;
                }

            } else {
                global.stopWatchHsv.Restart();
                mask = false;
            }

            CvInvoke.PutText(global.imgage, redpixels.ToString(), new Point(20, 30), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(global.imgage, mask.ToString(), new Point(20, 60), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(global.imgage, global.stopWatchHsv.ElapsedMilliseconds.ToString(), new Point(pictureBox1.Size.Width - 100, 30),
                FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(global.imgage, "Port = " + global.port, new Point(20, 90), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            pictureBox1.Image = global.imgage.Bitmap;

            if (mask) {
                if (!setCamera.flagOpen && !flag.debug) {
                    GetDebug();
                    global.numberCheckLed++;
                    ctms_checkLedNumber.Text = global.numberCheckLed.ToString();
                    if (File.Exists(setPath.folderCheckLed + global.head + "Image" + global.numberCheckLed + ".png")) {
                        List<string> rectangleX = File.ReadAllLines(setPath.folderCheckLed + global.head + Path.rectangleX).ToList();
                        List<string> rectangleY = File.ReadAllLines(setPath.folderCheckLed + global.head + Path.rectangleY).ToList();
                        List<string> rectangleWidth = File.ReadAllLines(setPath.folderCheckLed + global.head + Path.rectangleWidth).ToList();
                        List<string> rectangleHeight = File.ReadAllLines(setPath.folderCheckLed + global.head + Path.rectangleHeight).ToList();

                        try {
                            rect.X = Convert.ToInt32(rectangleX[global.numberCheckLed - 1]);
                            rect.Y = Convert.ToInt32(rectangleY[global.numberCheckLed - 1]);
                            rect.Width = Convert.ToInt32(rectangleWidth[global.numberCheckLed - 1]);
                            rect.Height = Convert.ToInt32(rectangleHeight[global.numberCheckLed - 1]);
                        } catch { }

                        global.numStep++;
                        global.stopWatchHsv.Restart();
                        return;
                    }

                    Thread.Sleep(50);
                    File.WriteAllText(setPath.resultTxt, "Color Detected\r\nPASS");
                    this.Close();
                }

                flag.resultPass = true;
                global.resultBlackup = "Color Detected";

            } else {
                reAdjust.Run(this);
                flag.resultPass = false;

                editValueCamDiv3.Process();
            }

            Thread.Sleep(100);
        }
        private void BlinkLedMode(object sender, EventArgs e) {
            if (flag.clearStopWatch) {
                flag.clearStopWatch = false;
                global.stopWatch.Restart();
            }

            if (global.stopWatch.ElapsedMilliseconds >= global.timeOut) {
                StampFailBlinkLed();
                if (!flag.debug) {
                    return;
                }
            }

            if (isMouseDown) {
                return;
            }

            if (setCamera.capture == null || setCamera.capture.Ptr == IntPtr.Zero || setCamera.capture.Width == 0) {
                try {
                    setCamera.capture.Dispose();
                } catch { }

                try {
                    setCamera.capture = new VideoCapture(global.port, captureApi);
                } catch { }

                Thread.Sleep(250);
                this.Size = new Size(setCamera.capture.Width, setCamera.capture.Height);
                pictureBox1.Size = new Size(setCamera.capture.Width, setCamera.capture.Height);
                ReadConfigCamera();
                flag.clearStopWatch = true;
                return;
            }

            Mat frame;
            try {
                if (!setCamera.hsvTestFlag) {
                    frame = setCamera.capture.QueryFrame();

                } else {
                    frame = new Mat("../../config/hsv_test.png");
                }

                global.imgage = frame.ToImage<Bgr, Byte>();
                global.imgageHsv = frame.ToImage<Hsv, Byte>();

            } catch {
                MessageBox.Show(Define.canNotOpenCamera);
                Application.Exit();
                return;
            }

            Graphics graphics = Graphics.FromImage(global.imgage.Bitmap);
            graphics.DrawRectangle(Pens.Red, rect);
            Image<Bgr, byte> imageCut = null;
            Image<Hsv, byte> imageCutHsv = null;
            Image<Bgr, byte> imageSup = null;
            Image<Hsv, byte> imageSupHsv = null;
            int redpixels = 0;

            if (!setCamera.hsvFlag) {
                imageCut = global.imgage.Copy();
                try {
                    imageCut.ROI = rect;
                } catch { }

                imageSup = imageCut.Copy();
                try {
                    redpixels = imageSup.InRange(setCamera.bgrLow, setCamera.bgrHigh).CountNonzero()[0];
                } catch { }

            } else {
                imageCutHsv = global.imgageHsv.Copy();
                try {
                    imageCutHsv.ROI = rect;
                } catch { }

                imageSupHsv = imageCutHsv.Copy();
                try {
                    redpixels = imageSupHsv.InRange(setCamera.hsvLow, setCamera.hsvHigh).CountNonzero()[0];
                } catch { }
            }

            bool mask = false;
            if (redpixels >= setCamera.hsvMask) {
                if (global.blinkLed.tricCal) {
                    int countSup = (int)(2000 / (global.blinkLed.timeLow + global.blinkLed.timeHigh));
                    global.blinkLed.counterMax = (int)Map(countSup, 0, 2000, 1, 1200);

                    global.blinkLed.tricCal = false;
                    global.blinkLed.frequencySup += 1 / ((global.blinkLed.timeLow + global.blinkLed.timeHigh) / 1000);
                    global.blinkLed.dutySup += (global.blinkLed.timeHigh * 100) / (global.blinkLed.timeLow + global.blinkLed.timeHigh);
                    global.blinkLed.counter++;

                    if (global.blinkLed.counter >= global.blinkLed.counterMax) {
                        global.blinkLed.frequency = global.blinkLed.frequencySup / global.blinkLed.counter;
                        global.blinkLed.duty = global.blinkLed.dutySup / global.blinkLed.counter;
                        global.blinkLed.FindResult();
                        global.blinkLed.counter = 0;
                        global.blinkLed.frequencySup = 0;
                        global.blinkLed.dutySup = 0;
                    }
                }

                mask = true;
                global.blinkLed.timeHigh = global.blinkLed.stopWatchHigh.ElapsedMilliseconds;
                global.blinkLed.stopWatchLow.Restart();

            } else {
                global.blinkLed.timeLow = global.blinkLed.stopWatchLow.ElapsedMilliseconds;
                global.blinkLed.stopWatchHigh.Restart();
                mask = false;
                global.blinkLed.tricCal = true;
            }

            CvInvoke.PutText(global.imgage, redpixels.ToString(), new Point(20, 30), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(global.imgage, mask.ToString(), new Point(20, 60), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(global.imgage, "Frequency=" + global.blinkLed.frequency.ToString("0.0000"), new Point(pictureBox1.Size.Width - 200, 30),
                FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(global.imgage, "Duty=" + global.blinkLed.duty.ToString("0.0000"), new Point(pictureBox1.Size.Width - 200, 60),
                FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(global.imgage, "Port = " + global.port, new Point(20, 90), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            pictureBox1.Image = global.imgage.Bitmap;

            if (!setCamera.flagOpen && !flag.debug) {
                if (global.blinkLed.flagResult) {
                    Thread.Sleep(50);
                    File.WriteAllText(setPath.resultTxt, global.blinkLed.result.ToString("0.0000") + "\r\nPASS");
                    this.Close();
                }
            }
        }

        private int flagRunNumber = 1;
        private void CheckLedMode(object sender, EventArgs e) {
            if (flag.clearStopWatch)
            {
                flag.clearStopWatch = false;
                global.stopWatch.Restart();
            }

            if (global.stopWatch.ElapsedMilliseconds >= global.timeOut)
            {
                StampFailCheckLed();
                if (!flag.debug)
                {
                    return;
                }
            }

            if (isMouseDown)
            {
                return;
            }

            if (setCamera.capture == null || setCamera.capture.Ptr == IntPtr.Zero || setCamera.capture.Width == 0)
            {
                try
                {
                    setCamera.capture.Dispose();
                } catch { }

                try
                {
                    setCamera.capture = new VideoCapture(global.port, captureApi);
                } catch { }

                Thread.Sleep(250);
                this.Size = new Size(setCamera.capture.Width, setCamera.capture.Height);
                pictureBox1.Size = new Size(setCamera.capture.Width, setCamera.capture.Height);
                ReadConfigCamera();
                flag.clearStopWatch = true;
                return;
            }

            Mat frame;
            try
            {
                if (!setCamera.hsvTestFlag)
                {
                    frame = setCamera.capture.QueryFrame();

                }
                else
                {
                    frame = new Mat("../../config/hsv_test.png");
                }

                global.imgage = frame.ToImage<Bgr, Byte>();
                global.imgageHsv = frame.ToImage<Hsv, Byte>();

            } catch
            {
                MessageBox.Show(Define.canNotOpenCamera);
                Application.Exit();
                return;
            }

            Graphics graphics = Graphics.FromImage(global.imgage.Bitmap);
            // graphics.DrawRectangle(Pens.Red, rect);
            Image<Bgr, byte> imageCut = null;
            Image<Hsv, byte> imageCutHsv = null;
            Image<Bgr, byte> imageSup = null;
            Image<Hsv, byte> imageSupHsv = null;
            int redpixels = 0;

            if (flagRunNumber == 1) // Red
            {
                graphics.DrawRectangle(Pens.Red, rect);
                imageCutHsv = global.imgageHsv.Copy();
                try
                {
                    imageCutHsv.ROI = rect;
                } catch { }

                imageSupHsv = imageCutHsv.Copy();
                try
                {
                    redpixels = imageSupHsv.InRange(new Hsv(0, 0, 150), new Hsv(60, 255, 255)).CountNonzero()[0];
                } catch { }
            }
            else if(flagRunNumber == 2) // Green
            {
                graphics.DrawRectangle(Pens.Lime, rect);
                imageCut = global.imgage.Copy();
                try
                {
                    imageCut.ROI = rect;
                } catch { }

                imageSup = imageCut.Copy();
                try
                {
                    redpixels = imageSup.InRange(new Bgr(0, 100, 0), new Bgr(100, 255, 50)).CountNonzero()[0];
                } catch { }
            }
            else // Blue
            {
                graphics.DrawRectangle(Pens.Blue, rect);
                imageCut = global.imgage.Copy();
                try
                {
                    imageCut.ROI = rect;
                } catch { }

                imageSup = imageCut.Copy();
                try
                {
                    redpixels = imageSup.InRange(new Bgr(100, 0, 0), new Bgr(255, 0, 0)).CountNonzero()[0];
                } catch { }
            }
            

            bool mask = false;
            if (redpixels >= setCamera.hsvMask)
            {
                if (global.stopWatchHsv.ElapsedMilliseconds >= setCamera.hsvTimeout)
                {
                    mask = true;
                }

            }
            else
            {
                global.stopWatchHsv.Restart();
                mask = false;
            }

            CvInvoke.PutText(global.imgage, redpixels.ToString(), new Point(20, 30), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(global.imgage, mask.ToString(), new Point(20, 60), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(global.imgage, global.stopWatchHsv.ElapsedMilliseconds.ToString(), new Point(pictureBox1.Size.Width - 100, 30),
                FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(global.imgage, "Port = " + global.port, new Point(20, 90), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            pictureBox1.Image = global.imgage.Bitmap;

            if (mask)
            {
                if (!setCamera.flagOpen && !flag.debug)
                {
                    GetDebug();
                    global.numberCheckLed++;
                    ctms_checkLedNumber.Text = global.numberCheckLed.ToString();
                    if (File.Exists(setPath.folderCheckLed + global.head + "Image" + global.numberCheckLed + ".png"))
                    {
                        List<string> rectangleX = File.ReadAllLines(setPath.folderCheckLed + global.head + Path.rectangleX).ToList();
                        List<string> rectangleY = File.ReadAllLines(setPath.folderCheckLed + global.head + Path.rectangleY).ToList();
                        List<string> rectangleWidth = File.ReadAllLines(setPath.folderCheckLed + global.head + Path.rectangleWidth).ToList();
                        List<string> rectangleHeight = File.ReadAllLines(setPath.folderCheckLed + global.head + Path.rectangleHeight).ToList();

                        try
                        {
                            rect.X = Convert.ToInt32(rectangleX[global.numberCheckLed - 1]);
                            rect.Y = Convert.ToInt32(rectangleY[global.numberCheckLed - 1]);
                            rect.Width = Convert.ToInt32(rectangleWidth[global.numberCheckLed - 1]);
                            rect.Height = Convert.ToInt32(rectangleHeight[global.numberCheckLed - 1]);
                        } catch { }

                        global.numStep++;
                        global.stopWatchHsv.Restart();
                        return;
                    }

                    if (flagRunNumber != 3)
                    {
                        flagRunNumber++;
                        ctms_checkLedNumber.Text = global.numberCheckLed.ToString();
                        global.stopWatchHsv.Restart();
                        return;
                    }

                    Thread.Sleep(50);
                    File.WriteAllText(setPath.resultTxt, "Color Detected\r\nPASS");
                    this.Close();
                }

                flag.resultPass = true;
                global.resultBlackup = "Color Detected";

            }
            else
            {
                reAdjust.Run(this);
                flag.resultPass = false;

                editValueCamDiv3.Process();
            }

            Thread.Sleep(100);
        }

        private void StampFailRead2d() {
            if (flag.debug) {
                global.stopWatch.Reset();
                return;
            }

            File.WriteAllText(setPath.resultTxt, "Unreadable\r\nFAIL");
            this.Close();
        }
        private void StampFailCompare() {
            if (flag.debug) {
                global.stopWatch.Reset();
                return;
            }

            File.WriteAllText(setPath.resultTxt, "(" + global.numStep + ")time over\r\nFAIL");
            this.Close();
        }
        private void StampFailCheckLed() {
            if (flag.debug) {
                global.stopWatch.Reset();
                return;
            }

            File.WriteAllText(setPath.resultTxt, "(" + global.numStep + ")time over\r\nFAIL");
            this.Close();
        }
        private void StampFailBlinkLed() {
            if (flag.debug) {
                global.stopWatch.Reset();
                return;
            }

            File.WriteAllText(setPath.resultTxt, "(" + global.numStep + ")time over\r\nFAIL");
            this.Close();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e) {
            if (flag.resultPass) {
                File.WriteAllText(setPath.resultTxt, global.resultBlackup + "\r\nPASS");

            } else {
                File.WriteAllText(setPath.resultTxt, "Unreadable\r\nFAIL");
            }

            this.Close();
        }
        private void setCameraToolStripMenuItem_Click(object sender, EventArgs e) {
            setCamera.Show();
        }
        private void runToolStripMenuItem_Click(object sender, EventArgs e) {
            createMinMax.Run(this);
        }
        private void SetCameraFormClosed(object sender, FormClosedEventArgs e) {
            string headSup = HeadConfig.head + global.head;

            //setupPay.write_text(headSup + HeadConfig.zoom, setCamera.zoomHScroll.Value.ToString(), setPath.stepCsv);
            //setupPay.write_text(headSup + HeadConfig.pan, setCamera.panHScroll.Value.ToString(), setPath.stepCsv);
            //setupPay.write_text(headSup + HeadConfig.tilt, setCamera.tiltHScroll.Value.ToString(), setPath.stepCsv);
            //setupPay.write_text(headSup + HeadConfig.contrast, setCamera.contrastHScroll.Value.ToString(), setPath.stepCsv);
            //setupPay.write_text(headSup + HeadConfig.brightness, setCamera.brightnessHScroll.Value.ToString(), setPath.stepCsv);
            //setupPay.write_text(headSup + HeadConfig.focus, setCamera.focusHScroll.Value.ToString(), setPath.stepCsv);
            //setupPay.write_text(headSup + HeadConfig.exposure, setCamera.exposureHScroll.Value.ToString(), setPath.stepCsv);
            //setupPay.write_text(headSup + HeadConfig.saturation, setCamera.saturationHScroll.Value.ToString(), setPath.stepCsv);
            //setupPay.write_text(headSup + HeadConfig.sharpness, setCamera.sharpnessHScroll.Value.ToString(), setPath.stepCsv);
            //setupPay.write_text(headSup + HeadConfig.gain, setCamera.gainHScroll.Value.ToString(), setPath.stepCsv);
            //setupPay.write_text(headSup + HeadConfig.gamma, setCamera.gammaHScroll.Value.ToString(), setPath.stepCsv);
            //setupPay.write_text(headSup + HeadConfig.process, setCamera.processHScroll.Value.ToString(), setPath.stepCsv);

            if (flag.checkLed || flag.blinkLed) {
                if (setCamera.hsvFlag) {
                    string hsvSup = setCamera.hsvLow.Hue.ToString() + " " + setCamera.hsvHigh.Hue.ToString() + " " +
                        setCamera.hsvLow.Satuation.ToString() + " " + setCamera.hsvHigh.Satuation.ToString() + " " +
                        setCamera.hsvLow.Value.ToString() + " " + setCamera.hsvHigh.Value.ToString();
                    setupPay.write_text(headSup + HeadConfig.hsvFormat, hsvSup, setPath.stepCsv);

                } else {
                    string bgrSup = setCamera.bgrLow.Blue.ToString() + " " + setCamera.bgrHigh.Blue.ToString() + " " +
                        setCamera.bgrLow.Green.ToString() + " " + setCamera.bgrHigh.Green.ToString() + " " +
                        setCamera.bgrLow.Red.ToString() + " " + setCamera.bgrHigh.Red.ToString();
                    setupPay.write_text(headSup + HeadConfig.hsvFormat, bgrSup, setPath.stepCsv);
                }

                setupPay.write_text(headSup + HeadConfig.hsvFlag, setCamera.hsvFlag.ToString(), setPath.stepCsv);
                setupPay.write_text(headSup + HeadConfig.hsvMask, setCamera.hsvMask.ToString(), setPath.stepCsv);
                setupPay.write_text(headSup + HeadConfig.hsvTimeOut, setCamera.hsvTimeout.ToString(), setPath.stepCsv);
            }

            setCamera.flagOpen = false;
            autoScale.flagIntro = false;
        }

        private void DelaymS(int mS) {
            Stopwatch stopwatchDelaymS = new Stopwatch();
            stopwatchDelaymS.Restart();
            while (mS > stopwatchDelaymS.ElapsedMilliseconds) {
                if (!stopwatchDelaymS.IsRunning)
                    stopwatchDelaymS.Start();
                Application.DoEvents();
            }
            stopwatchDelaymS.Stop();
        }
        private float Map(float value, float from1, float to1, float from2, float to2) {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        /// <summary>
        /// Log program catch to csv file
        /// </summary>
        /// <param name="text"></param>
        private void LogProgramCatch(string text) {
            string path = "D:\\LogError\\CameraCatch";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            DateTime now = DateTime.Now;
            StreamWriter swOut = new StreamWriter(path + "\\" + now.Year + "_" + now.Month + ".csv", true);
            string time = now.Day.ToString("00") + ":" + now.Hour.ToString("00") + ":" + now.Minute.ToString("00") + ":" + now.Second.ToString("00");
            swOut.WriteLine(time + "," + text);
            swOut.Close();
        }

        /// <summary>
        /// Event Exception Catch Program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            LogProgramCatch(e.ExceptionObject.ToString());
        }



        #region ================================ Roi Picture ================================
        bool isMouseDown = false;
        Point startLocation;
        Point endLcation;
        private static Rectangle rect;
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e) {
            if (!setCamera.flagOpen) {
                return;
            }

            if (e.Button != MouseButtons.Left) {
                return;
            }

            isMouseDown = true;
            startLocation = e.Location;
        }
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e) {
            if (isMouseDown) {
                endLcation = e.Location;
                pictureBox1.Invalidate();
            }
        }
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e) {
            if (!isMouseDown) {
                return;
            }

            int crop = 10;
            try
            {
                string txtCrop = File.ReadAllText("../../config/CameraShow_RoiCrop.txt");
                if (!string.IsNullOrEmpty(txtCrop))
                {
                    crop = Convert.ToInt32(txtCrop);
                }
            } catch { }

            if (rect.Size.Width < crop || rect.Size.Height < crop) {
                return;
            }

            Image<Bgr, byte> imgInput;
            endLcation = e.Location;
            isMouseDown = false;

            if (global.stepTest.Contains(Cmd.read2d)) {
                if (rect != null) {
                    imgInput = global.imgage.Copy();
                    imgInput.ROI = rect;
                    Image<Bgr, byte> temp = imgInput.Copy();
                    BarcodeReader reader = new BarcodeReader();
                    Result result = reader.Decode(temp.Bitmap);

                    if (result != null) {
                        MessageBox.Show(result.ToString());
                    }
                }

            } else if (flag.comPear) {
                if (rect != null) {
                    imgInput = global.imgage.Copy();
                    imgInput.ROI = rect;
                    Image<Bgr, byte> temp = imgInput.Copy();
                    temp.Save(setPath.folderCompare + global.head + "Image" + global.numberCompare + ".png");
                }

                rect.X -= crop;
                rect.Y -= crop;
                rect.Width = rect.Width + (crop * 2);
                rect.Height = rect.Height + (crop * 2);

                SaveRectangle(setPath.folderCompare, global.numberCompare);
                ctms_compareNext.Visible = true;
                if (global.numberCompare != 1) {
                    return;
                }

            } else if (flag.checkLed) {
                if (rect != null) {
                    Bitmap bitmap = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    bitmap.Save(setPath.folderCheckLed + global.head + "Image" + global.numberCheckLed + ".png");
                    SaveRectangle(setPath.folderCheckLed, global.numberCheckLed);
                    ctms_checkLedNext.Visible = true;
                    if (global.numberCheckLed != 1) {
                        return;
                    }
                }
            }

            setupPay.write_text(setPath.headCsv + HeadConfig.rectX, rect.X.ToString(), setPath.stepCsv);
            setupPay.write_text(setPath.headCsv + HeadConfig.rectY, rect.Y.ToString(), setPath.stepCsv);
            setupPay.write_text(setPath.headCsv + HeadConfig.rectWidth, rect.Width.ToString(), setPath.stepCsv);
            setupPay.write_text(setPath.headCsv + HeadConfig.rectHeight, rect.Height.ToString(), setPath.stepCsv);
            rectSup = rect;
        }
        private void pictureBox1_Paint(object sender, PaintEventArgs e) {
            if (rect != null && isMouseDown) {
                e.Graphics.DrawRectangle(Pens.Red, GetRectangle());
            }
        }
        private Rectangle GetRectangle() {
            rect.X = Math.Min(startLocation.X, endLcation.X);
            rect.Y = Math.Min(startLocation.Y, endLcation.Y);
            rect.Width = Math.Abs(startLocation.X - endLcation.X);
            rect.Height = Math.Abs(startLocation.Y - endLcation.Y);

            return rect;
        }
        private void SaveRectangle(string path, int number) {
            List<string> rectangleX = new List<string>();
            List<string> rectangleY = new List<string>();
            List<string> rectangleWidth = new List<string>();
            List<string> rectangleHeight = new List<string>();

            try {
                rectangleX = File.ReadAllLines(path + global.head + Path.rectangleX).ToList();
                rectangleY = File.ReadAllLines(path + global.head + Path.rectangleY).ToList();
                rectangleWidth = File.ReadAllLines(path + global.head + Path.rectangleWidth).ToList();
                rectangleHeight = File.ReadAllLines(path + global.head + Path.rectangleHeight).ToList();

            } catch {
                for (int loop = 0; loop < 100; loop++) {
                    rectangleX.Add("");
                    rectangleY.Add("");
                    rectangleWidth.Add("");
                    rectangleHeight.Add("");
                }
            }

            rectangleX[number - 1] = rect.X.ToString();
            rectangleY[number - 1] = rect.Y.ToString();
            rectangleWidth[number - 1] = rect.Width.ToString();
            rectangleHeight[number - 1] = rect.Height.ToString();

            File.WriteAllLines(path + global.head + Path.rectangleX, rectangleX);
            File.WriteAllLines(path + global.head + Path.rectangleY, rectangleY);
            File.WriteAllLines(path + global.head + Path.rectangleWidth, rectangleWidth);
            File.WriteAllLines(path + global.head + Path.rectangleHeight, rectangleHeight);
        }
        #endregion

        #region ================================ Event Click ================================
        private void setDebugToolStripMenuItem_Click(object sender, EventArgs e) {
            flag.debug = false;
        }
        private void setPortToolStripMenuItem_Click(object sender, EventArgs e) {
            File.WriteAllText(Path.setPort, string.Empty);
            setCamera.capture.Dispose();
            Application.Restart();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            try {
                setCamera.capture.Dispose();
            } catch { }

            while (true) {
                try {
                    string fileList = File.ReadAllText(Path.list);
                    File.WriteAllText(Path.list, fileList.Trim().Replace(global.head, String.Empty));
                    break;

                } catch {
                    Thread.Sleep(50);
                    continue;
                }
            }
        }
        private void ctms_compareNext_Click(object sender, EventArgs e) {
            global.numberCompare++;
            ctms_compareNumber.Text = global.numberCompare.ToString();

            ctms_compareNext.Visible = false;
        }
        private void ctms_compareNumber_Click(object sender, EventArgs e) {
            int result = 1;

            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Keys Number Step in Compare Image\r\nInteger = 1 - 100",
                    "Number Compare Image",
                    ctms_compareNumber.Text, 500, 300);
                if (input == String.Empty) {
                    return;
                }

                try {
                    result = Convert.ToInt32(input);

                } catch {
                    MessageBox.Show("not format");
                    continue;
                }

                if (result < 1 || result > 100) {
                    MessageBox.Show("not format");
                    continue;
                }

                break;
            }

            global.numberCompare = result;
            ctms_compareNumber.Text = result.ToString();
        }
        private void Frame_height_Click(object sender, EventArgs e) {
            int result = 1;

            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Kays Frame Height\r\nDefault = 800", "Frame Height", Frame_height.Text, 500, 300);
                if (input == String.Empty) {
                    return;
                }

                try {
                    result = Convert.ToInt32(input);

                } catch {
                    MessageBox.Show("not format");
                    continue;
                }

                break;
            }

            Frame_height.Text = result.ToString();
            setupPay.write_text(setPath.headCsv + HeadConfig.frameHeight, result.ToString(), setPath.stepCsv);
        }
        private void Frame_width_Click(object sender, EventArgs e) {
            int result = 1;

            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Keys Frame Width\r\nDefault = 600", "Frame Width", Frame_width.Text, 500, 300);
                if (input == String.Empty) {
                    return;
                }

                try {
                    result = Convert.ToInt32(input);

                } catch {
                    MessageBox.Show("not format");
                    continue;
                }

                break;
            }
            Frame_width.Text = result.ToString();
            setupPay.write_text(setPath.headCsv + HeadConfig.frameWidth, result.ToString(), setPath.stepCsv);
        }
        private void Process_timeout__Click(object sender, EventArgs e) {
            int result = 1;

            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Keys Process Timeout\r\nDefault = 500", "Process Timeout", Process_timeout_.Text, 500, 300);
                if (input == String.Empty) {
                    return;
                }

                try {
                    result = Convert.ToInt32(input);

                } catch {
                    MessageBox.Show("not format");
                    continue;
                }

                break;
            }

            Process_timeout_.Text = result.ToString();
            setupPay.write_text(setPath.headCsv + HeadConfig.startTimeOut, result.ToString(), setPath.stepCsv);
        }
        private void Process_roi__Click(object sender, EventArgs e) {
            int result = 1;

            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Keys Process Roi\r\nDefault = 10", "Process Roi", Process_roi_.Text, 500, 300);
                if (input == String.Empty) {
                    return;
                }

                try {
                    result = Convert.ToInt32(input);

                } catch {
                    MessageBox.Show("not format");
                    continue;
                }

                break;
            }

            Process_roi_.Text = result.ToString();
            setupPay.write_text(setPath.headCsv + HeadConfig.roiMove, result.ToString(), setPath.stepCsv);
        }
        private void Process_scale_limit__Click(object sender, EventArgs e) {
            int result = 1;

            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Keys Process Scale Limit\r\nDefault = 40", "Process Scale Limit",
                    Process_scale_limit_.Text, 500, 300);
                if (input == String.Empty) {
                    return;
                }

                try {
                    result = Convert.ToInt32(input);

                } catch {
                    MessageBox.Show("not format");
                    continue;
                }

                break;
            }

            Process_scale_limit_.Text = result.ToString();
            setupPay.write_text(setPath.headCsv + HeadConfig.autoScaleLimit, result.ToString(), setPath.stepCsv);
        }
        private void Process_scale_next__Click(object sender, EventArgs e) {
            int result = 1;

            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Keys Process Scale Next\r\nDefault = 2", "Process Scale Next",
                    Process_scale_next_.Text, 500, 300);
                if (input == String.Empty) {
                    return;
                }

                try {
                    result = Convert.ToInt32(input);

                } catch {
                    MessageBox.Show("not format");
                    continue;
                }

                break;
            }

            Process_scale_next_.Text = result.ToString();
            setupPay.write_text(setPath.headCsv + HeadConfig.autoScaleNext, result.ToString(), setPath.stepCsv);
        }
        private void process_roi_set_Click(object sender, EventArgs e) {
            setupPay.write_text(setPath.headCsv + HeadConfig.roiMoveSet, process_roi_set.Checked.ToString(), setPath.stepCsv);
        }
        private void ctms_focusAutoTrue_Click(object sender, EventArgs e) {
            ctms_focusAutoTrue.Checked = true;
            ctms_focusAutoFalse.Checked = false;
            File.WriteAllText(setPath.headTxt + Path.getAutoFocus, true.ToString());
            flag.autoFocus = true;
        }
        private void ctms_focusAutoFalse_Click(object sender, EventArgs e) {
            ctms_focusAutoTrue.Checked = false;
            ctms_focusAutoFalse.Checked = true;
            File.WriteAllText(setPath.headTxt + Path.getAutoFocus, false.ToString());
            flag.autoFocus = false;
        }
        private void ctms_digitSn_Click(object sender, EventArgs e) {
            int result = 1;

            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Keys Digit SN\r\nDefault = 13", "Digit SN", Process_scale_limit_.Text, 500, 300);
                if (input == String.Empty) {
                    return;
                }

                try {
                    result = Convert.ToInt32(input);

                } catch {
                    MessageBox.Show("not format");
                    continue;
                }

                break;
            }

            ctms_digitSn.Text = result.ToString();
            global.digitSn = result;
            setupPay.write_text(setPath.headCsv + HeadConfig.digitSN, result.ToString(), setPath.stepCsv);
        }
        private void ctms_adjustDegree_Click(object sender, EventArgs e) {
            int result = 1;

            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Keys Adjust Degree\r\nDefault = 0", "Adjust Degree", ctms_adjustDegree.Text, 500, 300);
                if (input == String.Empty) {
                    return;
                }

                try {
                    result = Convert.ToInt32(input);

                } catch {
                    MessageBox.Show("not format");
                    continue;
                }

                break;
            }

            ctms_adjustDegree.Text = result.ToString();
            global.adjustDegree = result;
            setupPay.write_text(setPath.headCsv + HeadConfig.adjustDegree, result.ToString(), setPath.stepCsv);
        }
        private void ctms_checkLedNext_Click(object sender, EventArgs e) {
            global.numberCheckLed++;
            ctms_checkLedNumber.Text = global.numberCheckLed.ToString();

            ctms_checkLedNext.Visible = false;
        }
        private void ctms_checkLedNumber_Click(object sender, EventArgs e) {
            int result = 1;

            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Keys Number Step in Check Led\r\nInteger = 1 - 100",
                    "Number Check Led",
                    ctms_checkLedNumber.Text, 500, 300);
                if (input == String.Empty) {
                    return;
                }

                try {
                    result = Convert.ToInt32(input);

                } catch {
                    MessageBox.Show("not format");
                    continue;
                }

                if (result < 1 || result > 100) {
                    MessageBox.Show("not format");
                    continue;
                }

                break;
            }

            global.numberCheckLed = result;
            ctms_checkLedNumber.Text = result.ToString();
        }
        private void ctms_setGammaAddressCsv_Click(object sender, EventArgs e) {
            string address = String.Empty;
            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Set gamma address in csv", "Gamma Address",
                    ctms_setGammaAddressCsv.Text, 500, 300);
                if (input == "") return;
                if (!CheckPasswordSetAddress()) {
                    return;
                }
                address = input;
                break;
            }
            ctms_setGammaAddressCsv.Text = address;
            setupPay.write_text(HeadConfig.head + global.head + HeadConfig.gamma, address, setPath.stepCsv);
        }
        private void ctms_propertySetting_Click(object sender, EventArgs e) {
            if (!CheckPasswordSetAddress()) {
                return;
            }
            setCamera.SetCapture(CapProp.Settings, 0);
        }
        private void ctms_setGammaAddress_Click(object sender, EventArgs e) {
            if (!CheckPasswordSetAddress())
            {
                return;
            }
            ctms_setGammaAddress.Checked = true;
            ctms_usePort.Checked = false;
            string headSup = HeadConfig.head + global.head;
            setupPay.write_text(headSup + HeadConfig.setAddress, Address.gamma, setPath.stepCsv);
        }
        private void ctms_usePort_Click(object sender, EventArgs e) {
            if (!CheckPasswordSetAddress())
            {
                return;
            }
            ctms_setGammaAddress.Checked = false;
            ctms_usePort.Checked = true;
            string headSup = HeadConfig.head + global.head;
            setupPay.write_text(headSup + HeadConfig.setAddress, Address.port, setPath.stepCsv);
        }
        private void ctms_saveConfig_Click(object sender, EventArgs e) {
            if (!CheckPasswordSetAddress())
            {
                return;
            }
            ReadConfigCameraToCsvFile();
        }
        private void ctms_roiCrop_Click(object sender, EventArgs e) {
            int result = 10;

            while (true)
            {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Kays Roi Crop\r\nDefault = 10", "Roi Crop", "10", 500, 300);
                if (input == String.Empty)
                {
                    return;
                }

                try
                {
                    result = Convert.ToInt32(input);

                } catch
                {
                    MessageBox.Show("not format");
                    continue;
                }

                break;
            }

            ctms_roiCrop.Text = result.ToString();
            File.WriteAllText("../../config/CameraShow_RoiCrop.txt", ctms_roiCrop.Text);
        }

        public void ButtonSetPortClick(object sender, EventArgs e) {
            File.WriteAllText(Path.setPort, string.Empty);
            flag.closeForm = true;
            Application.Restart();
        }
        public void ButtonCancelClick(object sender, EventArgs e) {
            flag.closeForm = true;
            formCancel.form.Close();
        }
        #endregion

        #region ================================ Class ================================
        public class SetCamera {
            #region ================================ Define ================================
            public VideoCapture capture { get; set; }
            public Form form { get; set; }
            public HScrollBar zoomHScroll { get; set; }
            public Label zoomLabel { get; set; }
            public Label zoomValue { get; set; }
            public HScrollBar panHScroll { get; set; }
            public Label panLabel { get; set; }
            public Label panValue { get; set; }
            public HScrollBar tiltHScroll { get; set; }
            public Label tiltLabel { get; set; }
            public Label tiltValue { get; set; }
            public HScrollBar contrastHScroll { get; set; }
            public Label contrastLabel { get; set; }
            public Label contrastValue { get; set; }
            public HScrollBar brightnessHScroll { get; set; }
            public Label brightnessLabel { get; set; }
            public Label brightnessValue { get; set; }
            public HScrollBar focusHScroll { get; set; }
            public Label focusLabel { get; set; }
            public Label focusValue { get; set; }
            public int processInt { get; set; }
            public bool processFlag { get; set; }
            public HScrollBar processHScroll { get; set; }
            public Label processLabel { get; set; }
            public Label processValue { get; set; }
            public Button processButton { get; set; }
            public HScrollBar exposureHScroll { get; set; }
            public Label exposureLabel { get; set; }
            public Label exposureValue { get; set; }
            public HScrollBar saturationHScroll { get; set; }
            public Label saturationLabel { get; set; }
            public Label saturationValue { get; set; }
            public HScrollBar sharpnessHScroll { get; set; }
            public Label sharpnessLabel { get; set; }
            public Label sharpnessValue { get; set; }
            public HScrollBar gainHScroll { get; set; }
            public Label gainLabel { get; set; }
            public Label gainValue { get; set; }
            public HScrollBar gammaHScroll { get; set; }
            public Label gammaLabel { get; set; }
            public Label gammaValue { get; set; }
            public Bgr bgrLow { get; set; }
            public Bgr bgrHigh { get; set; }
            public Label bgrLabel { get; set; }
            public TextBox bgrTextBox { get; set; }
            public Hsv hsvLow { get; set; }
            public Hsv hsvHigh { get; set; }
            public bool hsvFlag { get; set; }
            public int hsvMask { get; set; }
            public bool hsvTestFlag { get; set; }
            public Label hsvLabel { get; set; }
            public TextBox hsvTextBox { get; set; }
            public Button hsvButton { get; set; }
            public TextBox hsvMaskTextBox { get; set; }
            public Label maskLabel { get; set; }
            public Label hsvTestLabel { get; set; }
            public int hsvTimeout { get; set; }
            public Label timeoutLabel { get; set; }
            public TextBox timeoutTextBox { get; set; }
            public Button exampleButton { get; set; }
            public CheckBox showAllCheckBox { get; set; }
            #endregion
            public bool flagOpen { get; set; }
            /// <summary>
            /// For define form main
            /// </summary>
            private Form1 main { get; set; }

            public SetCamera(Form1 form) {
                main = form;
                processInt = 170;
                hsvMask = 10;
                hsvTimeout = 100;
            }
            public void Show() {
                #region ================================ Define ================================
                form = new Form();
                form.FormClosed += main.SetCameraFormClosed;
                form.Size = new Size(400, 400);
                zoomHScroll = new HScrollBar();
                zoomLabel = new Label();
                zoomValue = new Label();
                panHScroll = new HScrollBar();
                panLabel = new Label();
                panValue = new Label();
                tiltHScroll = new HScrollBar();
                tiltLabel = new Label();
                tiltValue = new Label();
                contrastHScroll = new HScrollBar();
                contrastLabel = new Label();
                contrastValue = new Label();
                brightnessHScroll = new HScrollBar();
                brightnessLabel = new Label();
                brightnessValue = new Label();
                focusHScroll = new HScrollBar();
                focusLabel = new Label();
                focusValue = new Label();
                processHScroll = new HScrollBar();
                processLabel = new Label();
                processValue = new Label();
                processButton = new Button();
                bgrLabel = new Label();
                bgrTextBox = new TextBox();
                hsvLabel = new Label();
                hsvTextBox = new TextBox();
                hsvButton = new Button();
                hsvMaskTextBox = new TextBox();
                maskLabel = new Label();
                hsvTestLabel = new Label();
                timeoutLabel = new Label();
                timeoutTextBox = new TextBox();
                exampleButton = new Button();
                showAllCheckBox = new CheckBox();
                exposureHScroll = new HScrollBar();
                exposureLabel = new Label();
                exposureValue = new Label();
                saturationHScroll = new HScrollBar();
                saturationLabel = new Label();
                saturationValue = new Label();
                sharpnessHScroll = new HScrollBar();
                sharpnessLabel = new Label();
                sharpnessValue = new Label();
                gainHScroll = new HScrollBar();
                gainLabel = new Label();
                gainValue = new Label();
                gammaHScroll = new HScrollBar();
                gammaLabel = new Label();
                gammaValue = new Label();
                #endregion

                string headSup = HeadConfig.head + main.global.head;
                bool trySup = false;
                string valueScroll = string.Empty;

                #region ================================ Zoom ================================
                zoomLabel.Text = HeadConfig.zoom;
                zoomLabel.Size = new Size(50, 15);
                zoomLabel.Location = new Point(1, 1);
                form.Controls.Add(zoomLabel);
                zoomHScroll.Scroll += ZoomScroll;
                zoomHScroll.LargeChange = 1;
                //zoomHScroll.Minimum = GetMinMaxValue(main, MinMax.zoom, MinMax.min);
                //zoomHScroll.Maximum = GetMinMaxValue(main, MinMax.zoom, MinMax.max);
                //valueScroll = main.setupPay.read_text(headSup + HeadConfig.zoom, main.setPath.stepCsv);
                //try {
                //    zoomHScroll.Value = Convert.ToInt32(valueScroll);
                //    if (!SetCapture(CapProp.Zoom, zoomHScroll.Value)) {
                //        trySup = true;
                //    }
                //} catch { trySup = true; }
                //if (trySup) {
                //    try {
                //        zoomHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Zoom);
                //    } catch { }
                //    trySup = false;
                //}
                zoomHScroll.Minimum = -999;
                zoomHScroll.Maximum = 999;
                try
                {
                    zoomHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Zoom);
                } catch { }
                zoomHScroll.Enabled = false;
                zoomHScroll.Size = new Size(300, zoomHScroll.Height);
                zoomHScroll.Location = new Point(1, 15);
                form.Controls.Add(zoomHScroll);
                zoomValue.Text = zoomHScroll.Value.ToString();
                zoomValue.Size = new Size(50, 15);
                zoomValue.Location = new Point(zoomHScroll.Size.Width + 5, zoomHScroll.Location.Y + 2);
                form.Controls.Add(zoomValue);
                #endregion
                #region ================================ Pan ================================
                panLabel.Text = HeadConfig.pan;
                panLabel.Size = new Size(300, 15);
                panLabel.Location = new Point(1, 40);
                form.Controls.Add(panLabel);
                panHScroll.Scroll += PanScroll;
                panHScroll.LargeChange = 1;
                //panHScroll.Minimum = GetMinMaxValue(main, MinMax.pan, MinMax.min);
                //panHScroll.Maximum = GetMinMaxValue(main, MinMax.pan, MinMax.max);
                //valueScroll = main.setupPay.read_text(headSup + HeadConfig.pan, main.setPath.stepCsv);
                //try {
                //    panHScroll.Value = Convert.ToInt32(valueScroll);
                //    if (!SetCapture(CapProp.Pan, panHScroll.Value)) {
                //        trySup = true;
                //    }
                //} catch { trySup = true; }
                //if (trySup) {
                //    try {
                //        panHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Pan);
                //    } catch { }
                //    trySup = false;
                //}
                panHScroll.Minimum = -999;
                panHScroll.Maximum = 999;
                try
                {
                    panHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Pan);
                } catch { }
                panHScroll.Enabled = false;
                panHScroll.Size = new Size(300, panHScroll.Height);
                panHScroll.Location = new Point(1, 55);
                form.Controls.Add(panHScroll);
                panValue.Text = panHScroll.Value.ToString();
                panValue.Size = new Size(300, 15);
                panValue.Location = new Point(panHScroll.Size.Width + 5, panHScroll.Location.Y + 2);
                form.Controls.Add(panValue);
                #endregion
                #region ================================ Tilt ================================
                tiltLabel.Text = HeadConfig.tilt;
                tiltLabel.Size = new Size(300, 15);
                tiltLabel.Location = new Point(1, 80);
                form.Controls.Add(tiltLabel);
                tiltHScroll.Scroll += TiltScroll;
                tiltHScroll.LargeChange = 1;
                //tiltHScroll.Minimum = GetMinMaxValue(main, MinMax.tilt, MinMax.min);
                //tiltHScroll.Maximum = GetMinMaxValue(main, MinMax.tilt, MinMax.max);
                //valueScroll = main.setupPay.read_text(headSup + HeadConfig.tilt, main.setPath.stepCsv);
                //try {
                //    tiltHScroll.Value = Convert.ToInt32(valueScroll);
                //    if (!SetCapture(CapProp.Tilt, tiltHScroll.Value)) {
                //        trySup = true;
                //    }
                //} catch { trySup = true; }
                //if (trySup) {
                //    try {
                //        tiltHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Tilt);
                //    } catch { }
                //    trySup = false;
                //}
                tiltHScroll.Minimum = -999;
                tiltHScroll.Maximum = 999;
                try
                {
                    tiltHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Tilt);
                } catch { }
                tiltHScroll.Enabled = false;
                tiltHScroll.Size = new Size(300, tiltHScroll.Height);
                tiltHScroll.Location = new Point(1, 95);
                form.Controls.Add(tiltHScroll);
                tiltValue.Text = tiltHScroll.Value.ToString();
                tiltValue.Size = new Size(300, 15);
                tiltValue.Location = new Point(tiltHScroll.Size.Width + 5, tiltHScroll.Location.Y + 2);
                form.Controls.Add(tiltValue);
                #endregion
                #region ================================ Contrast ================================
                contrastLabel.Text = HeadConfig.contrast;
                contrastLabel.Size = new Size(300, 15);
                contrastLabel.Location = new Point(1, 120);
                form.Controls.Add(contrastLabel);
                contrastHScroll.Scroll += ContrastScroll;
                contrastHScroll.LargeChange = 1;
                //contrastHScroll.Minimum = GetMinMaxValue(main, MinMax.contrast, MinMax.min);
                //contrastHScroll.Maximum = GetMinMaxValue(main, MinMax.contrast, MinMax.max);
                //valueScroll = main.setupPay.read_text(headSup + HeadConfig.contrast, main.setPath.stepCsv);
                //try {
                //    contrastHScroll.Value = Convert.ToInt32(valueScroll);
                //    if (!SetCapture(CapProp.Contrast, contrastHScroll.Value)) {
                //        trySup = true;
                //    }
                //} catch { trySup = true; }
                //if (trySup) {
                //    try {
                //        contrastHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Contrast);
                //    } catch { }
                //    trySup = false;
                //}
                contrastHScroll.Minimum = -999;
                contrastHScroll.Maximum = 999;
                try
                {
                    contrastHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Contrast);
                } catch { }
                contrastHScroll.Enabled = false;
                contrastHScroll.Size = new Size(300, contrastHScroll.Height);
                contrastHScroll.Location = new Point(1, 135);
                form.Controls.Add(contrastHScroll);
                contrastValue.Text = contrastHScroll.Value.ToString();
                contrastValue.Size = new Size(300, 15);
                contrastValue.Location = new Point(contrastHScroll.Size.Width + 5, contrastHScroll.Location.Y + 2);
                form.Controls.Add(contrastValue);
                #endregion
                #region ================================ Brightness ================================
                brightnessLabel.Text = HeadConfig.brightness;
                brightnessLabel.Size = new Size(300, 15);
                brightnessLabel.Location = new Point(1, 160);
                form.Controls.Add(brightnessLabel);
                brightnessHScroll.Scroll += BrightnessScroll;
                brightnessHScroll.LargeChange = 1;
                //brightnessHScroll.Minimum = GetMinMaxValue(main, MinMax.brightness, MinMax.min);
                //brightnessHScroll.Maximum = GetMinMaxValue(main, MinMax.brightness, MinMax.max);
                //valueScroll = main.setupPay.read_text(headSup + HeadConfig.brightness, main.setPath.stepCsv);
                //try {
                //    brightnessHScroll.Value = Convert.ToInt32(valueScroll);
                //    if (!SetCapture(CapProp.Brightness, brightnessHScroll.Value)) {
                //        trySup = true;
                //    }
                //} catch { trySup = true; }
                //if (trySup) {
                //    try {
                //        brightnessHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Brightness);
                //    } catch { }
                //    trySup = false;
                //}
                brightnessHScroll.Minimum = -999;
                brightnessHScroll.Maximum = 999;
                try
                {
                    brightnessHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Brightness);
                } catch { }
                brightnessHScroll.Enabled = false;
                brightnessHScroll.Size = new Size(300, brightnessHScroll.Height);
                brightnessHScroll.Location = new Point(1, 175);
                form.Controls.Add(brightnessHScroll);
                brightnessValue.Text = brightnessHScroll.Value.ToString();
                brightnessValue.Size = new Size(300, 15);
                brightnessValue.Location = new Point(brightnessHScroll.Size.Width + 5, brightnessHScroll.Location.Y + 2);
                form.Controls.Add(brightnessValue);
                #endregion
                #region ================================ Focus ================================
                focusLabel.Text = HeadConfig.focus;
                focusLabel.Size = new Size(300, 15);
                focusLabel.Location = new Point(1, 200);
                form.Controls.Add(focusLabel);
                focusHScroll.Scroll += FocusScroll;
                focusHScroll.SmallChange = 5;
                focusHScroll.LargeChange = 5;
                //focusHScroll.Minimum = GetMinMaxValue(main, MinMax.focus, MinMax.min);
                //focusHScroll.Maximum = GetMinMaxValue(main, MinMax.focus, MinMax.max);
                //valueScroll = main.setupPay.read_text(headSup + HeadConfig.focus, main.setPath.stepCsv);
                //try {
                //    focusHScroll.Value = Convert.ToInt32(valueScroll);
                //    if (!SetCapture(CapProp.Focus, focusHScroll.Value)) {
                //        trySup = true;
                //    }
                //} catch { trySup = true; }
                //if (trySup) {
                //    try {
                //        focusHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Focus);
                //    } catch { }
                //    trySup = false;
                //}
                focusHScroll.Minimum = -999;
                focusHScroll.Maximum = 999;
                try
                {
                    focusHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Focus);
                } catch { }
                focusHScroll.Enabled = false;
                focusHScroll.Size = new Size(300, focusHScroll.Height);
                focusHScroll.Location = new Point(1, 215);
                form.Controls.Add(focusHScroll);
                focusValue.Text = focusHScroll.Value.ToString();
                focusValue.Size = new Size(30, 15);
                focusValue.Location = new Point(focusHScroll.Size.Width + 5, focusHScroll.Location.Y + 2);
                form.Controls.Add(focusValue);
                Button b_focus = new Button();
                b_focus.Click += FocusClick;
                b_focus.Text = "Auto";
                b_focus.Visible = false;
                b_focus.Size = new Size(40, 20);
                b_focus.Location = new Point(focusHScroll.Size.Width + 40, focusHScroll.Location.Y);
                form.Controls.Add(b_focus);
                #endregion
                #region ================================ Process ================================
                processLabel.Text = HeadConfig.process;
                processLabel.Size = new Size(300, 15);
                processLabel.Location = new Point(1, 240);
                form.Controls.Add(processLabel);
                processHScroll.Scroll += ProcessScroll;
                processHScroll.LargeChange = 1;
                processHScroll.Minimum = 0;
                processHScroll.Maximum = 255;
                processHScroll.Value = processInt;
                processHScroll.Size = new Size(300, processHScroll.Height);
                processHScroll.Location = new Point(1, 255);
                processHScroll.Enabled = false;
                form.Controls.Add(processHScroll);
                processValue.Text = processHScroll.Value.ToString();
                processValue.Size = new Size(30, 15);
                processValue.Location = new Point(processHScroll.Size.Width + 5, processHScroll.Location.Y + 2);
                form.Controls.Add(processValue);
                processButton.Click += ProcessClick;
                processButton.Text = processFlag.ToString();
                processButton.Size = new Size(40, 20);
                processButton.Visible = false;
                processButton.Location = new Point(processHScroll.Size.Width + 40, processHScroll.Location.Y);
                form.Controls.Add(processButton);
                #endregion
                #region ================================ Bgr ================================
                bgrLabel.Text = "bgr: \"BlurLow BlueHigh GreenLow GreenHigh RedLow RedHigh\"";
                bgrLabel.Size = new Size(400, 15);
                bgrLabel.Location = new Point(1, 280);
                form.Controls.Add(bgrLabel);
                bgrTextBox.Text = bgrLow.Blue.ToString() + " " + bgrHigh.Blue.ToString() + " " +
                             bgrLow.Green.ToString() + " " + bgrHigh.Green.ToString() + " " +
                             bgrLow.Red.ToString() + " " + bgrHigh.Red.ToString();
                bgrTextBox.Size = new Size(180, 20);
                bgrTextBox.Location = new Point(1, bgrLabel.Location.Y + 15);
                bgrTextBox.KeyDown += BgrKeyDown;
                form.Controls.Add(bgrTextBox);
                maskLabel.Text = "Mask :";
                maskLabel.Size = new Size(40, 15);
                maskLabel.Location = new Point(bgrTextBox.Size.Width + 85, bgrTextBox.Location.Y + 2);
                form.Controls.Add(maskLabel);
                hsvMaskTextBox.Text = hsvMask.ToString();
                hsvMaskTextBox.Size = new Size(75, 20);
                hsvMaskTextBox.Location = new Point(bgrTextBox.Size.Width + 125, bgrTextBox.Location.Y);
                hsvMaskTextBox.KeyDown += HsvMaskKeyDown;
                form.Controls.Add(hsvMaskTextBox);
                exampleButton.Click += ExampleButtonClick;
                exampleButton.Text = "Example";
                exampleButton.Size = new Size(60, 20);
                exampleButton.Location = new Point(bgrTextBox.Size.Width + 10, bgrTextBox.Location.Y);
                form.Controls.Add(exampleButton);
                #endregion
                #region ================================ Hsv ================================
                hsvLabel.Text = "hsv: \"HueLow HueHigh SatuationLow SatuationHigh ValueLow ValueHigh\"";
                hsvLabel.Size = new Size(400, 15);
                hsvLabel.Location = new Point(1, 320);
                form.Controls.Add(hsvLabel);
                hsvTextBox.Text = hsvLow.Hue.ToString() + " " + hsvHigh.Hue.ToString() + " " +
                             hsvLow.Satuation.ToString() + " " + hsvHigh.Satuation.ToString() + " " +
                             hsvLow.Value.ToString() + " " + hsvHigh.Value.ToString();
                hsvTextBox.Size = new Size(180, 20);
                hsvTextBox.Location = new Point(1, hsvLabel.Location.Y + 15);
                hsvTextBox.KeyDown += HsvKeyDown;
                form.Controls.Add(hsvTextBox);
                timeoutLabel.Text = "Timeout :";
                timeoutLabel.Size = new Size(47, 15);
                timeoutLabel.Location = new Point(hsvTextBox.Size.Width + 10, hsvTextBox.Location.Y + 2);
                form.Controls.Add(timeoutLabel);
                timeoutTextBox.Text = hsvTimeout.ToString();
                timeoutTextBox.Size = new Size(60, 20);
                timeoutTextBox.Location = new Point(hsvTextBox.Size.Width + 57, hsvTextBox.Location.Y);
                timeoutTextBox.KeyDown += TimeoutKeyDown;
                form.Controls.Add(timeoutTextBox);
                hsvTestLabel.Text = "ms";
                hsvTestLabel.Size = new Size(30, 15);
                hsvTestLabel.Location = new Point(hsvTextBox.Size.Width + 120, hsvTextBox.Location.Y + 2);
                form.Controls.Add(hsvTestLabel);
                hsvButton.Click += HsvClick;
                hsvButton.Text = hsvTestFlag.ToString();
                hsvButton.Size = new Size(40, 20);
                hsvButton.Location = new Point(hsvTextBox.Size.Width + 160, hsvTextBox.Location.Y);
                form.Controls.Add(hsvButton);
                #endregion

                showAllCheckBox.Location = new Point(365, 0);
                showAllCheckBox.Click += ShowAllClick;
                form.Controls.Add(showAllCheckBox);

                #region ================================ Exposure ================================
                exposureLabel.Text = HeadConfig.exposure;
                exposureLabel.Size = new Size(300, 15);
                exposureLabel.Location = new Point(1, 360);
                form.Controls.Add(exposureLabel);
                exposureHScroll.Scroll += ExposureScroll;
                exposureHScroll.LargeChange = 1;
                //exposureHScroll.Minimum = GetMinMaxValue(main, MinMax.exposure, MinMax.min);
                //exposureHScroll.Maximum = GetMinMaxValue(main, MinMax.exposure, MinMax.max);
                //valueScroll = main.setupPay.read_text(headSup + HeadConfig.exposure, main.setPath.stepCsv);
                //try {
                //    exposureHScroll.Value = Convert.ToInt32(valueScroll);
                //    if (!SetCapture(CapProp.Exposure, exposureHScroll.Value)) {
                //        trySup = true;
                //    }
                //} catch { trySup = true; }
                //if (trySup) {
                //    try {
                //        exposureHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Exposure);
                //    } catch { }
                //    trySup = false;
                //}
                exposureHScroll.Minimum = -999;
                exposureHScroll.Maximum = 999;
                try
                {
                    exposureHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Exposure);
                } catch { }
                exposureHScroll.Enabled = false;
                exposureHScroll.Size = new Size(300, exposureHScroll.Height);
                exposureHScroll.Location = new Point(1, 375);
                form.Controls.Add(exposureHScroll);
                exposureValue.Text = exposureHScroll.Value.ToString();
                exposureValue.Size = new Size(300, 15);
                exposureValue.Location = new Point(exposureHScroll.Size.Width + 5, exposureHScroll.Location.Y + 2);
                form.Controls.Add(exposureValue);
                #endregion
                #region ================================ Saturation ================================
                saturationLabel.Text = HeadConfig.saturation + " (Address In Camera)";
                saturationLabel.Size = new Size(300, 15);
                saturationLabel.Location = new Point(1, 400);
                form.Controls.Add(saturationLabel);
                saturationHScroll.Scroll += SaturationScroll;
                saturationHScroll.LargeChange = 1;
                //saturationHScroll.Minimum = GetMinMaxValue(main, MinMax.saturation, MinMax.min);
                //saturationHScroll.Maximum = GetMinMaxValue(main, MinMax.saturation, MinMax.max);
                saturationHScroll.Minimum = -999;
                saturationHScroll.Maximum = 999;
                try
                {
                    saturationHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Saturation);
                } catch { }
                saturationHScroll.Size = new Size(300, saturationHScroll.Height);
                saturationHScroll.Location = new Point(1, 415);
                saturationHScroll.Enabled = false;
                form.Controls.Add(saturationHScroll);
                saturationValue.Text = saturationHScroll.Value.ToString();
                saturationValue.Size = new Size(300, 15);
                saturationValue.Location = new Point(saturationHScroll.Size.Width + 5, saturationHScroll.Location.Y + 2);
                form.Controls.Add(saturationValue);
                #endregion
                #region ================================ Sharpness ================================
                sharpnessLabel.Text = HeadConfig.sharpness;
                sharpnessLabel.Size = new Size(300, 15);
                sharpnessLabel.Location = new Point(1, 440);
                form.Controls.Add(sharpnessLabel);
                sharpnessHScroll.Scroll += SharpnessScroll;
                sharpnessHScroll.LargeChange = 1;
                //sharpnessHScroll.Minimum = GetMinMaxValue(main, MinMax.sharpness, MinMax.min);
                //sharpnessHScroll.Maximum = GetMinMaxValue(main, MinMax.sharpness, MinMax.max);
                //valueScroll = main.setupPay.read_text(headSup + HeadConfig.sharpness, main.setPath.stepCsv);
                //try {
                //    sharpnessHScroll.Value = Convert.ToInt32(valueScroll);
                //    if (!SetCapture(CapProp.Sharpness, sharpnessHScroll.Value)) {
                //        trySup = true;
                //    }
                //} catch { trySup = true; }
                //if (trySup) {
                //    try {
                //        sharpnessHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Sharpness);
                //    } catch { }
                //    trySup = false;
                //}
                sharpnessHScroll.Minimum = -999;
                sharpnessHScroll.Maximum = 999;
                try
                {
                    sharpnessHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Sharpness);
                } catch { }
                sharpnessHScroll.Enabled = false;
                sharpnessHScroll.Size = new Size(300, sharpnessHScroll.Height);
                sharpnessHScroll.Location = new Point(1, 455);
                form.Controls.Add(sharpnessHScroll);
                sharpnessValue.Text = sharpnessHScroll.Value.ToString();
                sharpnessValue.Size = new Size(300, 15);
                sharpnessValue.Location = new Point(sharpnessHScroll.Size.Width + 5, sharpnessHScroll.Location.Y + 2);
                form.Controls.Add(sharpnessValue);
                #endregion
                #region ================================ Gain ================================
                gainLabel.Text = HeadConfig.gain + " (Address In Camera)";
                gainLabel.Size = new Size(300, 15);
                gainLabel.Location = new Point(1, 480);
                form.Controls.Add(gainLabel);
                gainHScroll.Scroll += GainScroll;
                gainHScroll.LargeChange = 1;
                //gainHScroll.Minimum = GetMinMaxValue(main, MinMax.gain, MinMax.min);
                //gainHScroll.Maximum = GetMinMaxValue(main, MinMax.gain, MinMax.max);
                gainHScroll.Minimum = -999;
                gainHScroll.Maximum = 999;
                try
                {
                    gainHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Gain);
                } catch { }
                gainHScroll.Size = new Size(300, gainHScroll.Height);
                gainHScroll.Location = new Point(1, 495);
                gainHScroll.Enabled = false;
                form.Controls.Add(gainHScroll);
                gainValue.Text = gainHScroll.Value.ToString();
                gainValue.Size = new Size(300, 15);
                gainValue.Location = new Point(gainHScroll.Size.Width + 5, gainHScroll.Location.Y + 2);
                form.Controls.Add(gainValue);
                #endregion
                #region ================================ Gamma ================================
                gammaLabel.Text = HeadConfig.gamma + " (Address In Camera)";
                gammaLabel.Size = new Size(300, 15);
                gammaLabel.Location = new Point(1, 520);
                form.Controls.Add(gammaLabel);
                gammaHScroll.Scroll += GammaScroll;
                gammaHScroll.LargeChange = 1;
                //gammaHScroll.Minimum = GetMinMaxValue(main, MinMax.gamma, MinMax.min);
                //gammaHScroll.Maximum = GetMinMaxValue(main, MinMax.gamma, MinMax.max);
                gammaHScroll.Minimum = -999;
                gammaHScroll.Maximum = 999;
                try {
                    gammaHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Gamma);
                } catch { }
                gammaHScroll.Size = new Size(300, gammaHScroll.Height);
                gammaHScroll.Location = new Point(1, 535);
                gammaHScroll.Enabled = false;
                form.Controls.Add(gammaHScroll);
                gammaValue.Text = gammaHScroll.Value.ToString();
                gammaValue.Size = new Size(300, 15);
                gammaValue.Location = new Point(gammaHScroll.Size.Width + 5, gammaHScroll.Location.Y + 2);
                form.Controls.Add(gammaValue);
                #endregion

                //string ggg = CvInvoke.OclGetPlatformsSummary();
                //File.WriteAllText("zzzzzzzz.txt", ggg);

                form.Show();
                flagOpen = true;
            }
            public int GetMinMaxValue(Form1 form, string type, string minmax) {
                int result;

                string resultSup = form.setupPay.read_text(form.setPath.headCsv + type + minmax, form.setPath.minmaxCsv);
                result = Convert.ToInt32(resultSup);

                return result;
            }
            /// <summary>
            /// For gen log error set camera capProp
            /// </summary>
            /// <param name="value">is Value at want set capProp</param>
            /// <param name="capProp">is Type of value at want set</param>
            private void LogSendError(string value, string capProp) {
                string path = "D:\\LogError\\CameraSetCapProp";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                DateTime now = DateTime.Now;
                StreamWriter swOut = new StreamWriter(path + "\\" + now.Year + "_" + now.Month + ".csv", true);
                string time = now.Day.ToString("00") + ":" + now.Hour.ToString("00") + ":" + now.Minute.ToString("00") + ":" + now.Second.ToString("00");
                swOut.WriteLine(time + ",Value=" + value + ",Head=" + main.global.head + ",Step=" + main.global.stepTest + "," + capProp);
                swOut.Close();
            }
            /// <summary>
            /// Function set camera for set and check error to gen log error csv
            /// </summary>
            /// <param name="capProp">is Type of value at want set</param>
            /// <param name="value">is Value at set in camera</param>
            public bool SetCapture(CapProp capProp, double value) {
                if (value == -1)
                {
                    return true;
                }
                if (!capture.SetCaptureProperty(capProp, value))
                {
                    LogSendError(value.ToString(), capProp.ToString());
                    return false;
                }
                return true;
            }


            #region ================================ Event Click ================================
            public void ZoomScroll(object sender, ScrollEventArgs e) {
                zoomValue.Text = zoomHScroll.Value.ToString();
                SetCapture(CapProp.Zoom, zoomHScroll.Value);
            }
            public void PanScroll(object sender, ScrollEventArgs e) {
                panValue.Text = panHScroll.Value.ToString();
                SetCapture(CapProp.Pan, panHScroll.Value);
            }
            public void TiltScroll(object sender, ScrollEventArgs e) {
                tiltValue.Text = tiltHScroll.Value.ToString();
                SetCapture(CapProp.Tilt, tiltHScroll.Value);
            }
            public void ContrastScroll(object sender, ScrollEventArgs e) {
                contrastValue.Text = contrastHScroll.Value.ToString();
                SetCapture(CapProp.Contrast, contrastHScroll.Value);
            }
            public void BrightnessScroll(object sender, ScrollEventArgs e) {
                brightnessValue.Text = brightnessHScroll.Value.ToString();
                SetCapture(CapProp.Brightness, brightnessHScroll.Value);
            }
            public void FocusScroll(object sender, ScrollEventArgs e) {
                focusValue.Text = focusHScroll.Value.ToString();
                SetCapture(CapProp.Focus, focusHScroll.Value);
            }
            public void FocusClick(object sender, EventArgs e) {
                SetCapture(CapProp.Autofocus, 1);
                try {
                    focusHScroll.Value = (int)capture.GetCaptureProperty(CapProp.Focus);
                } catch { }
                focusValue.Text = focusHScroll.Value.ToString();
            }
            public void ProcessScroll(object sender, ScrollEventArgs e) {
                processValue.Text = processHScroll.Value.ToString();
                processInt = processHScroll.Value;
            }
            public void ProcessClick(object sender, EventArgs e) {
                if (processButton.Text == Define.True) {
                    processButton.Text = Define.False;
                    processFlag = false;

                } else {
                    processButton.Text = Define.True;
                    processFlag = true;
                }
            }
            public void ExposureScroll(object sender, ScrollEventArgs e) {
                exposureValue.Text = exposureHScroll.Value.ToString();
                SetCapture(CapProp.Exposure, exposureHScroll.Value);
            }
            public void SaturationScroll(object sender, ScrollEventArgs e) {
                saturationValue.Text = saturationHScroll.Value.ToString();
                SetCapture(CapProp.Saturation, saturationHScroll.Value);
            }
            public void SharpnessScroll(object sender, ScrollEventArgs e) {
                sharpnessValue.Text = sharpnessHScroll.Value.ToString();
                SetCapture(CapProp.Sharpness, sharpnessHScroll.Value);
            }
            public void GainScroll(object sender, ScrollEventArgs e) {
                //gainValue.Text = gainHScroll.Value.ToString();
                //SetCapture(CapProp.Gain, gainHScroll.Value);
            }
            public void GammaScroll(object sender, ScrollEventArgs e) {
                //gammaValue.Text = gammaHScroll.Value.ToString();
                //SetCapture(CapProp.Gamma, gammaHScroll.Value);
            }
            public void BgrKeyDown(object sender, KeyEventArgs e) {
                if (e.KeyValue != 13) return;

                string data = bgrTextBox.Text;
                string[] dataSplit;
                int[] bgr = { 0, 0, 0, 0, 0, 0 };

                try {
                    dataSplit = data.Split(' ');
                } catch {
                    MessageBox.Show(Define.notFormath);
                    return;
                }

                if (dataSplit.Length != 6) {
                    MessageBox.Show(Define.notFormath);
                    return;
                }

                try {
                    for (int i = 0; i < 6; i++) {
                        bgr[i] = Convert.ToInt32(dataSplit[i]);
                    }
                } catch {
                    MessageBox.Show(Define.notFormath);
                    return;
                }

                bgrLow = new Bgr(bgr[0], bgr[2], bgr[4]);
                bgrHigh = new Bgr(bgr[1], bgr[3], bgr[5]);
                hsvFlag = false;
            }
            public void HsvKeyDown(object sender, KeyEventArgs e) {
                if (e.KeyValue != 13) return;

                string data = hsvTextBox.Text;
                string[] dataSplit;
                int[] hsv = { 0, 0, 0, 0, 0, 0 };

                try {
                    dataSplit = data.Split(' ');
                } catch {
                    MessageBox.Show(Define.notFormath);
                    return;
                }

                if (dataSplit.Length != 6) {
                    MessageBox.Show(Define.notFormath);
                    return;
                }

                try {
                    for (int i = 0; i < 6; i++) {
                        hsv[i] = Convert.ToInt32(dataSplit[i]);
                    }
                } catch {
                    MessageBox.Show(Define.notFormath);
                    return;
                }

                hsvLow = new Hsv(hsv[0], hsv[2], hsv[4]);
                hsvHigh = new Hsv(hsv[1], hsv[3], hsv[5]);
                hsvFlag = true;
            }
            public void HsvMaskKeyDown(object sender, KeyEventArgs e) {
                if (e.KeyValue != 13) return;

                string data = hsvMaskTextBox.Text;
                int value;

                try {
                    value = Convert.ToInt32(data);
                } catch {
                    MessageBox.Show(Define.notFormath);
                    return;
                }

                hsvMask = value;
            }
            public void HsvClick(object sender, EventArgs e) {
                if (hsvButton.Text == Define.True) {
                    hsvButton.Text = Define.False;
                    hsvTestFlag = false;

                } else {
                    hsvButton.Text = Define.True;
                    hsvTestFlag = true;
                }
            }
            public void TimeoutKeyDown(object sender, KeyEventArgs e) {
                if (e.KeyValue != 13) return;

                string data = timeoutTextBox.Text;
                int value;

                try {
                    value = Convert.ToInt32(data);
                } catch {
                    MessageBox.Show(Define.notFormath);
                    return;
                }

                hsvTimeout = value;
            }
            public void ExampleButtonClick(object sender, EventArgs e) {
                MessageBox.Show("green : bgr : 0 100 100 255 0 50" + "\r\n" +
                                "red : hsv : 0 60 0 255 150 255");
            }
            public void ShowAllClick(object sender, EventArgs e) {
                if (showAllCheckBox.Checked) {
                    form.Size = new Size(400, 600);
                    exposureHScroll.Visible = true;
                    exposureLabel.Visible = true;
                    exposureValue.Visible = true;
                    saturationHScroll.Visible = true;
                    saturationLabel.Visible = true;
                    saturationValue.Visible = true;
                    sharpnessHScroll.Visible = true;
                    sharpnessLabel.Visible = true;
                    sharpnessValue.Visible = true;
                    gainHScroll.Visible = true;
                    gainLabel.Visible = true;
                    gainValue.Visible = true;
                    gammaHScroll.Visible = true;
                    gammaLabel.Visible = true;
                    gammaValue.Visible = true;

                } else {
                    form.Size = new Size(400, 400);
                    exposureHScroll.Visible = false;
                    exposureLabel.Visible = false;
                    exposureValue.Visible = false;
                    saturationHScroll.Visible = false;
                    saturationLabel.Visible = false;
                    saturationValue.Visible = false;
                    sharpnessHScroll.Visible = false;
                    sharpnessLabel.Visible = false;
                    sharpnessValue.Visible = false;
                    gainHScroll.Visible = false;
                    gainLabel.Visible = false;
                    gainValue.Visible = false;
                    gammaHScroll.Visible = false;
                    gammaLabel.Visible = false;
                    gammaValue.Visible = false;
                }
            }
            #endregion

            public static class Define {
                public static readonly string True = "True";
                public static readonly string False = "False";
                public static readonly string notFormath = "Not Formath";
            }
        }
        public class CreateMinMax {
            public void Run(Form1 form) {
                string nameForm = form.Text;
                form.Text = "...";
                int valueBegin;
                double min, max;
                int valueRun = 1000;
                CapProp[] emguCvEnum = GetCapProp();
                string type = string.Empty;

                for (int loop = 0; loop < emguCvEnum.Length; loop++) {
                    type = GetType(loop);
                    min = 0;
                    max = 0;
                    valueBegin = 0;
                    try {
                        valueBegin = (int)form.setCamera.capture.GetCaptureProperty(emguCvEnum[loop]);
                    } catch { }

                    min = GetMin(form, valueBegin, valueRun, emguCvEnum[loop]);
                    max = GetMax(form, valueBegin, valueRun, emguCvEnum[loop]);

                    for (int headSup = 1; headSup <= 10; headSup++) {
                        if (CheckHead(form, headSup)) {
                            form.setupPay.write_text(MinMax.head + headSup + type + MinMax.min, min.ToString(), form.setPath.minmaxCsv);
                            form.setupPay.write_text(MinMax.head + headSup + type + MinMax.max, max.ToString(), form.setPath.minmaxCsv);
                        }
                    }

                    form.setCamera.SetCapture(emguCvEnum[loop], valueBegin);
                }

                GetFocus(form);

                form.Text = nameForm;
            }
            private CapProp[] GetCapProp() {
                CapProp[] emguCvEnum = new[] { CapProp.Zoom,
                                         CapProp.Exposure,
                                         CapProp.Pan,
                                         CapProp.Tilt,
                                         CapProp.Contrast,
                                         CapProp.Brightness,
                                         CapProp.Sharpness};

                return emguCvEnum;
            }
            private string GetType(int loop) {
                string type = string.Empty;

                switch (loop) {
                    case 0: type = MinMax.zoom; break;
                    case 1: type = MinMax.exposure; break;
                    case 2: type = MinMax.pan; break;
                    case 3: type = MinMax.tilt; break;
                    case 4: type = MinMax.contrast; break;
                    case 5: type = MinMax.brightness; break;
                    case 6: type = MinMax.sharpness; break;
                }

                return type;
            }
            private bool CheckHead(Form1 form, int head) {
                ToolStripMenuItem checkBox = new ToolStripMenuItem();

                switch (head) {
                    case 1: checkBox = form.config_cam1; break;
                    case 2: checkBox = form.config_cam2; break;
                    case 3: checkBox = form.config_cam3; break;
                    case 4: checkBox = form.config_cam4; break;
                    case 5: checkBox = form.config_cam5; break;
                    case 6: checkBox = form.config_cam6; break;
                    case 7: checkBox = form.config_cam7; break;
                    case 8: checkBox = form.config_cam8; break;
                    case 9: checkBox = form.config_cam9; break;
                    case 10: checkBox = form.config_cam10; break;
                }

                if (checkBox.Checked) {
                    return true;
                }

                return false;
            }
            private int GetMin(Form1 form, int valueBegin, int valueRun, CapProp capProp) {
                int valueStack = valueBegin;
                int getGetGet = 3;

                while (true) {
                    if (valueRun < 1) {
                        if (getGetGet > 0) {
                            getGetGet--;
                            valueRun = 1;

                        } else {
                            break;
                        }
                    }

                    form.setCamera.SetCapture(capProp, valueStack - valueRun);
                    double resultRun = form.setCamera.capture.GetCaptureProperty(capProp);

                    if ((int)resultRun != (valueStack - valueRun)) {
                        valueRun /= 2;
                        continue;
                    }

                    valueStack += 0 - valueRun;
                    valueRun /= 2;
                }
                return valueStack;
            }
            private int GetMax(Form1 form, int valueBegin, int valueRun, CapProp capProp) {
                int valueStack = valueBegin;
                int getGetGet = 3;

                while (true) {
                    if (valueRun < 1) {
                        if (getGetGet > 0) {
                            getGetGet--;
                            valueRun = 1;

                        } else {
                            break;
                        }
                    }

                    form.setCamera.SetCapture(capProp, valueStack + valueRun);
                    double resultRun = form.setCamera.capture.GetCaptureProperty(capProp);

                    if ((int)resultRun != (valueStack + valueRun)) {
                        valueRun /= 2;
                        continue;
                    }

                    valueStack += valueRun;
                    valueRun /= 2;
                }
                return valueStack;
            }
            private void GetFocus(Form1 form) {
                int min = 0;
                int max = 0;
                int valueBegin = 0;
                try {
                    valueBegin = (int)form.setCamera.capture.GetCaptureProperty(CapProp.Focus);
                } catch { }

                if ((valueBegin % 5) != 0) {
                    int sup = valueBegin / 5;
                    valueBegin = sup * 5;
                }

                int loop = 0;
                while (true) {
                    bool result = form.setCamera.SetCapture(CapProp.Focus, valueBegin + (loop * 5));

                    if (!result) {
                        break;
                    }

                    max = valueBegin + (loop * 5);
                    loop++;
                }

                loop = 0;
                while (true) {
                    bool result = form.setCamera.SetCapture(CapProp.Focus, valueBegin - (loop * 5));

                    if (!result) {
                        break;
                    }

                    min = valueBegin - (loop * 5);
                    loop++;
                }

                for (int headSup = 1; headSup <= 10; headSup++) {
                    if (CheckHead(form, headSup)) {
                        form.setupPay.write_text(MinMax.head + headSup + MinMax.focus + MinMax.min, min.ToString(), form.setPath.minmaxCsv);
                        form.setupPay.write_text(MinMax.head + headSup + MinMax.focus + MinMax.max, max.ToString(), form.setPath.minmaxCsv);
                    }
                }

                form.setCamera.SetCapture(CapProp.Focus, valueBegin);
            }
        }
        public class AutoScale {
            private int contrast { get; set; }
            private int contrastMin { get; set; }
            private int contrastMax { get; set; }
            private int contrastSupMin { get; set; }
            private int contrastSupMax { get; set; }
            private int brightness { get; set; }
            private int brightnessMin { get; set; }
            private int brightnessMax { get; set; }
            private int brightnessSupMin { get; set; }
            private int brightnessSupMax { get; set; }
            private int focus { get; set; }
            private int focusMin { get; set; }
            private int focusMax { get; set; }
            private int focusSupMin { get; set; }
            private int focusSupMax { get; set; }
            private int sharpness { get; set; }
            private int sharpnessMin { get; set; }
            private int sharpnessMax { get; set; }
            private int sharpnessSupMin { get; set; }
            private int sharpnessSupMax { get; set; }
            private int stepNumber { get; set; }
            private int scaleLimit { get; set; }
            private int scaleNext { get; set; }
            private int roiMove { get; set; }
            private int startTimeOut { get; set; }
            public bool flagIntro { get; set; }
            private int[] rectX { get; set; }
            private int[] rectY { get; set; }
            private int rectNumber { get; set; }
            private Stopwatch stopwatchStart { get; set; }

            public AutoScale() {
                contrastSupMin = -999;
                contrastSupMax = 999;
                brightnessSupMin = -999;
                brightnessSupMax = 999;
                focusSupMin = -999;
                focusSupMax = 999;
                sharpnessSupMin = -999;
                sharpnessSupMax = 999;
                scaleLimit = 40;
                scaleNext = 2;
                roiMove = 10;
                startTimeOut = 500;
                rectX = new int[]{
                    -1,  0,  1, -1, 1, -1, 0, 1, -2, -1,  0,  1,  2, -2, -1,  0,  1,  2, -2, -1, 1, 2, -2, -1, 0, 1, 2, -2, -1,
                    0, 1, 2, -3, -2, -1,  0,  1,  2,  3, -3, -2, -1,  0,  1,  2,  3, -3, -2, -1,  0,  1,  2,  3, -3, -2, -1, 1,
                    2, 3, -3, -2, -1, 0, 1, 2, 3, -3, -2, -1, 0, 1, 2, 3, -3, -2, -1, 0, 1, 2, 3 };
                rectY = new int[]{
                    -1, -1, -1,  0, 0,  1, 1, 1, -2, -2, -2, -2, -2, -1, -1, -1, -1, -1,  0,  0, 0, 0,  1,  1, 1, 1, 1,  2,  2,
                    2, 2, 2, -3, -3, -3, -3, -3, -3, -3, -2, -2, -2, -2, -2, -2, -2, -1, -1, -1, -1, -1, -1, -1,  0,  0,  0, 0,
                    0, 0,  1,  1,  1, 1, 1, 1, 1,  2,  2,  2, 2, 2, 2, 2,  3,  3,  3, 3, 3, 3, 3 };
                stopwatchStart = new Stopwatch();
            }
            public void Run(Form1 form) {
                if (!flagIntro) {
                    try {
                        contrast = Convert.ToInt32(form.setupPay.read_text(HeadConfig.contrast, form.setPath.stepCsv));
                        brightness = Convert.ToInt32(form.setupPay.read_text(HeadConfig.brightness, form.setPath.stepCsv));
                        focus = Convert.ToInt32(form.setupPay.read_text(HeadConfig.focus, form.setPath.stepCsv));
                        sharpness = Convert.ToInt32(form.setupPay.read_text(HeadConfig.sharpness, form.setPath.stepCsv));

                    } catch {
                        contrast = (int)form.setCamera.capture.GetCaptureProperty(CapProp.Contrast);
                        brightness = (int)form.setCamera.capture.GetCaptureProperty(CapProp.Brightness);
                        focus = (int)form.setCamera.capture.GetCaptureProperty(CapProp.Focus);
                        sharpness = (int)form.setCamera.capture.GetCaptureProperty(CapProp.Sharpness);
                    }

                    form.Process_timeout_.Text = form.setupPay.read_text(form.setPath.headCsv + HeadConfig.startTimeOut, form.setPath.stepCsv);
                    form.Process_roi_.Text = form.setupPay.read_text(form.setPath.headCsv + HeadConfig.roiMove, form.setPath.stepCsv);
                    form.Process_scale_limit_.Text = form.setupPay.read_text(form.setPath.headCsv + HeadConfig.autoScaleLimit, form.setPath.stepCsv);
                    form.Process_scale_next_.Text = form.setupPay.read_text(form.setPath.headCsv + HeadConfig.autoScaleNext, form.setPath.stepCsv);

                    contrastSupMin = Convert.ToInt32(form.setupPay.read_text(form.setPath.headCsv + MinMax.contrast + MinMax.min, form.setPath.minmaxCsv));
                    contrastSupMax = Convert.ToInt32(form.setupPay.read_text(form.setPath.headCsv + MinMax.contrast + MinMax.max, form.setPath.minmaxCsv));
                    brightnessSupMin = Convert.ToInt32(form.setupPay.read_text(form.setPath.headCsv + MinMax.brightness + MinMax.min, form.setPath.minmaxCsv));
                    brightnessSupMax = Convert.ToInt32(form.setupPay.read_text(form.setPath.headCsv + MinMax.brightness + MinMax.max, form.setPath.minmaxCsv));
                    focusSupMin = Convert.ToInt32(form.setupPay.read_text(form.setPath.headCsv + MinMax.focus + MinMax.min, form.setPath.minmaxCsv));
                    focusSupMax = Convert.ToInt32(form.setupPay.read_text(form.setPath.headCsv + MinMax.focus + MinMax.max, form.setPath.minmaxCsv));
                    sharpnessSupMin = Convert.ToInt32(form.setupPay.read_text(form.setPath.headCsv + MinMax.sharpness + MinMax.min, form.setPath.minmaxCsv));
                    sharpnessSupMax = Convert.ToInt32(form.setupPay.read_text(form.setPath.headCsv + MinMax.sharpness + MinMax.max, form.setPath.minmaxCsv));
                    startTimeOut = Convert.ToInt32(form.Process_timeout_.Text);
                    roiMove = Convert.ToInt32(form.Process_roi_.Text);
                    scaleLimit = Convert.ToInt32(form.Process_scale_limit_.Text);
                    scaleNext = Convert.ToInt32(form.Process_scale_next_.Text);

                    contrastMin = contrast - scaleLimit;
                    contrastMax = contrast + scaleLimit;
                    brightnessMin = brightness - scaleLimit;
                    brightnessMax = brightness + scaleLimit;
                    focusMin = focus - scaleLimit;
                    focusMax = focus + scaleLimit;
                    sharpnessMin = sharpness - scaleLimit;
                    sharpnessMax = sharpness + scaleLimit;

                    if (contrastMin < contrastSupMin) { contrastMin = contrastSupMin; }
                    if (contrastMax > contrastSupMax) { contrastMax = contrastSupMax; }
                    if (brightnessMin < brightnessSupMin) { brightnessMin = brightnessSupMin; }
                    if (brightnessMax > brightnessSupMax) { brightnessMax = brightnessSupMax; }
                    if (focusMin < focusSupMin) { focusMin = focusSupMin; }
                    if (focusMax > focusSupMax) { focusMax = focusSupMax; }
                    if (sharpnessMin < sharpnessSupMin) { sharpnessMin = sharpnessSupMin; }
                    if (sharpnessMax > sharpnessSupMax) { sharpnessMax = sharpnessSupMax; }

                    flagIntro = true;
                    stepNumber = 0;
                    stopwatchStart.Restart();
                }

                if (roiMove == 0) {
                    rectNumber = rectX.Length;
                }

                switch (stepNumber) {
                    case 0:
                        if (stopwatchStart.ElapsedMilliseconds > startTimeOut) {
                            stopwatchStart.Stop();
                            stepNumber++;
                        }
                        break;
                    case 1:
                        if (!form.process_roi_set.Checked) {

                            //Sklip auto scale 
                            //stepNumber++;
                            stepNumber = 0;

                            break;
                        }

                        if (rectNumber == rectX.Length) {
                            rect = form.rectSup;

                            //Sklip auto scale
                            //stepNumber++;
                            stepNumber = 0;

                            rectNumber = 0;
                            break;
                        }

                        rect.X = form.rectSup.X + (rectX[rectNumber] * roiMove);
                        rect.Y = form.rectSup.Y + (rectY[rectNumber] * roiMove);
                        rectNumber++;
                        break;
                    case 2:
                        if (!form.flag.autoFocus) {
                            form.setCamera.SetCapture(CapProp.Focus, focus);
                            stepNumber++;
                            break;
                        }

                        focus -= scaleNext;
                        if (focus <= focusMin) {
                            focus += scaleLimit;
                            stepNumber++;
                        }

                        form.setCamera.SetCapture(CapProp.Focus, focus);
                        break;
                    case 3:
                        if (!form.flag.autoFocus) {
                            form.setCamera.SetCapture(CapProp.Focus, focus);
                            stepNumber++;
                            break;
                        }

                        focus += scaleNext;
                        if (focus >= focusMax) {
                            focus -= scaleLimit;
                            stepNumber++;
                        }

                        form.setCamera.SetCapture(CapProp.Focus, focus);
                        break;
                    case 4:
                        contrast -= scaleNext;
                        if (contrast <= contrastMin) {
                            contrast += scaleLimit;
                            stepNumber++;
                        }

                        form.setCamera.SetCapture(CapProp.Contrast, contrast);
                        break;
                    case 5:
                        contrast += scaleNext;
                        if (contrast >= contrastMax) {
                            contrast -= scaleLimit;
                            stepNumber++;
                        }

                        form.setCamera.SetCapture(CapProp.Contrast, contrast);
                        break;
                    case 6:
                        brightness -= scaleNext;
                        if (brightness <= brightnessMin) {
                            brightness += scaleLimit;
                            stepNumber++;
                        }

                        form.setCamera.SetCapture(CapProp.Brightness, brightness);
                        break;
                    case 7:
                        brightness += scaleNext;
                        if (brightness >= brightnessMax) {
                            brightness -= scaleLimit;
                            stepNumber++;
                        }

                        form.setCamera.SetCapture(CapProp.Brightness, brightness);
                        break;
                    case 8:
                        sharpness -= scaleNext;
                        if (sharpness <= sharpnessMin) {
                            sharpness += scaleLimit;
                            stepNumber++;
                        }

                        form.setCamera.SetCapture(CapProp.Sharpness, sharpness);
                        break;
                    case 9:
                        sharpness += scaleNext;
                        if (sharpness >= sharpnessMax) {
                            sharpness -= scaleLimit;
                            stepNumber = 0;
                        }

                        form.setCamera.SetCapture(CapProp.Sharpness, sharpness);
                        break;
                }
            }
        }
        public class Flag {
            public bool setPort { get; set; }
            public bool read2d { get; set; }
            public bool comPear { get; set; }
            public bool checkLed { get; set; }
            public bool blinkLed { get; set; }
            public bool resultPass { get; set; }
            public bool debug { get; set; }
            public bool closeForm { get; set; }
            public bool autoFocus { get; set; }
            public bool clearStopWatch = false;

            public Flag() {
                debug = true;
            }
        }
        public class Global {
            public string head { get; set; }
            public string stepTest { get; set; }
            public int port { get; set; }
            public Image<Bgr, Byte> imgage { get; set; }
            public Image<Hsv, Byte> imgageHsv { get; set; }
            public Stopwatch stopWatch { get; set; }
            public Stopwatch stopWatchShow { get; set; }
            public int timeOut { get; set; }
            public Stopwatch stopWatchHsv { get; set; }
            public string resultBlackup { get; set; }
            public int digitSn { get; set; }
            public int numberCompare { get; set; }
            public int adjustDegree { get; set; }
            public int numberCheckLed { get; set; }
            public int numStep { get; set; }
            public BlinkLed blinkLed { get; set; }

            public Global() {
                head = "1";
                stepTest = Cmd.normal;
                stopWatch = new Stopwatch();
                stopWatchShow = new Stopwatch();
                timeOut = 10000;
                stopWatchHsv = new Stopwatch();
                resultBlackup = string.Empty;
                digitSn = 13;
                numberCompare = 1;
                numberCheckLed = 1;
                numStep = 1;
                blinkLed = new BlinkLed();
            }

            public class BlinkLed {
                public double timeHigh { get; set; }
                public double timeLow { get; set; }
                public double duty { get; set; }
                public double dutySup { get; set; }
                public double frequency { get; set; }
                public double frequencySup { get; set; }
                public List<double> listResult { get; set; }
                /// <summary>กำหนดค่า error ของ result (%)</summary>
                private double limitMaxDiff { get; set; }
                public double result { get; set; }
                public bool flagResult { get; set; }
                public Stopwatch stopWatchHigh { get; set; }
                public Stopwatch stopWatchLow { get; set; }
                public bool tricCal { get; set; }
                public int counter { get; set; }
                public int counterMax { get; set; }
                /// <summary>
                /// Value = 2000
                /// เอาไว้กำหนดค่าน้อยที่สุดในการแสดงผลความถี่ (mS)
                /// </summary>
                public double timeAckMin { get; set; }

                public BlinkLed() {
                    timeHigh = 0;
                    timeLow = 0;
                    duty = 0;
                    frequency = 0;
                    stopWatchHigh = new Stopwatch();
                    stopWatchLow = new Stopwatch();
                    timeAckMin = 2000;
                    limitMaxDiff = 5;
                    listResult = new List<double>();
                    listResult.Add(0);
                    listResult.Add(0);
                    listResult.Add(0);
                }
                public void FindResult() {
                    if (frequency > 999999) {
                        return;
                    }

                    listResult.Add(frequency);
                    listResult.RemoveAt(0);

                    result = (listResult[0] + listResult[1] + listResult[2]) / 3;
                    for (int loop = 0; loop < 3; loop++) {
                        double diff = Math.Abs(result - listResult[0]);
                        double persenDiff = diff / result * 100;

                        if (persenDiff > limitMaxDiff) {
                            flagResult = false;
                            return;
                        }
                    }

                    flagResult = true;
                }
            }
        }
        public class FormCancel {
            public Form form = new Form();

            public void Show(Form1 main) {
                form = new Form();
                form.Icon = Properties.Resources.icon;
                form.Size = new Size(200, 70);
                form.ControlBox = false;
                form.Text = main.global.stepTest;

                Button buttonCancel = new Button();
                buttonCancel.Click += main.ButtonCancelClick;
                buttonCancel.Size = new Size(75, 30);
                buttonCancel.Location = new Point(0, 0);
                buttonCancel.Text = "cancel";

                Button buttonSetPort = new Button();
                buttonSetPort.Click += main.ButtonSetPortClick;
                buttonSetPort.Size = new Size(75, 30);
                buttonSetPort.Location = new Point(80, 0);
                buttonSetPort.Text = Define.setPort;

                form.Controls.Add(buttonCancel);
                form.Controls.Add(buttonSetPort);
                form.Show();
            }
        }
        public class SetPath {
            public string headTxt { get; set; }
            public string headCsv { get; set; }
            public string minmaxCsv { get; set; }
            public string resultTxt { get; set; }
            public string stepCsv { get; set; }
            public string folderCompare { get; set; }
            public string folderCheckLed { get; set; }
            public string folderBlinkLed { get; set; }
        }
        public class ReAdjust {
            private bool fristTric { get; set; }
            private int contrast { get; set; }
            private int brightness { get; set; }
            private int focus { get; set; }
            private int sharpness { get; set; }
            private int exposure { get; set; }
            private int contrastMin { get; set; }
            private int contrastMax { get; set; }
            private int brightnessMin { get; set; }
            private int brightnessMax { get; set; }
            private int focusMin { get; set; }
            private int focusMax { get; set; }
            private int sharpnessMin { get; set; }
            private int sharpnessMax { get; set; }
            private int exposureMin { get; set; }
            private int exposureMax { get; set; }
            private int contrastMiddle { get; set; }
            private int brightnessMiddle { get; set; }
            private int focusMiddle { get; set; }
            private int sharpnessMiddle { get; set; }
            private int exposureMiddle { get; set; }

            public void Run(Form1 form) {
                if (form.flag.debug) {
                    return;
                }

                if (!fristTric) {
                    fristTric = true;
                    contrast = Convert.ToInt32(form.setupPay.read_text(form.setPath.headCsv + HeadConfig.contrast, form.setPath.stepCsv));
                    brightness = Convert.ToInt32(form.setupPay.read_text(form.setPath.headCsv + HeadConfig.brightness, form.setPath.stepCsv));
                    focus = Convert.ToInt32(form.setupPay.read_text(form.setPath.headCsv + HeadConfig.focus, form.setPath.stepCsv));
                    sharpness = Convert.ToInt32(form.setupPay.read_text(form.setPath.headCsv + HeadConfig.sharpness, form.setPath.stepCsv));
                    exposure = Convert.ToInt32(form.setupPay.read_text(form.setPath.headCsv + HeadConfig.exposure, form.setPath.stepCsv));

                    contrastMin = form.setCamera.GetMinMaxValue(form, MinMax.contrast, MinMax.min);
                    contrastMax = form.setCamera.GetMinMaxValue(form, MinMax.contrast, MinMax.max);
                    brightnessMin = form.setCamera.GetMinMaxValue(form, MinMax.brightness, MinMax.min);
                    brightnessMax = form.setCamera.GetMinMaxValue(form, MinMax.brightness, MinMax.max);
                    focusMin = form.setCamera.GetMinMaxValue(form, MinMax.focus, MinMax.min);
                    focusMax = form.setCamera.GetMinMaxValue(form, MinMax.focus, MinMax.max);
                    sharpnessMin = form.setCamera.GetMinMaxValue(form, MinMax.sharpness, MinMax.min);
                    sharpnessMax = form.setCamera.GetMinMaxValue(form, MinMax.sharpness, MinMax.max);
                    exposureMin = form.setCamera.GetMinMaxValue(form, MinMax.exposure, MinMax.min);
                    exposureMax = form.setCamera.GetMinMaxValue(form, MinMax.exposure, MinMax.max);

                    contrastMiddle = (contrastMin + contrastMax) / 2;
                    brightnessMiddle = (brightnessMin + brightnessMax) / 2;
                    focusMiddle = (focusMin + focusMax) / 2;
                    sharpnessMiddle = (sharpnessMin + sharpnessMax) / 2;
                    exposureMiddle = (exposureMin + exposureMax) / 2;
                }

                //if (form.flag.autoFocus) {
                //    form.setCamera.SetCapture(CapProp.Focus, focusMiddle);
                //    form.setCamera.SetCapture(CapProp.Focus, focus);
                //}

                //เอาค่ากลางมาปรับก่อนแล้วมันน่าจะบัคอยู่
                //form.setCamera.SetCapture(CapProp.Contrast, contrastMiddle);
                form.setCamera.SetCapture(CapProp.Contrast, contrast);
                //form.setCamera.SetCapture(CapProp.Brightness, brightnessMiddle);
                form.setCamera.SetCapture(CapProp.Brightness, brightness);
                //form.setCamera.SetCapture(CapProp.Sharpness, sharpnessMiddle);
                form.setCamera.SetCapture(CapProp.Sharpness, sharpness);
                //form.setCamera.SetCapture(CapProp.Exposure, exposureMiddle);
                form.setCamera.SetCapture(CapProp.Exposure, exposure);

                form.setCamera.SetCapture(CapProp.Focus, focus);
            }
        }

        public static class Cmd {
            public static readonly string normal = "normal";
            public static readonly string read2d = "read2d";
            public static readonly string comPar = "compar_image";
            public static readonly string comPear = "compare_image";
            public static readonly string checkLed = "check_led";
            public static readonly string blinkLed = "BlinkLed";
        }
        public static class MinMax {
            public static readonly string nameFile = "cameraMinMax_";
            public static readonly string head = "Head ";
            public static readonly string min = " Min";
            public static readonly string max = " Max";
            public static readonly string read2d = " Read2d";
            public static readonly string zoom = " Zoom";
            public static readonly string pan = " Pan";
            public static readonly string tilt = " Tilt";
            public static readonly string contrast = " Contrast";
            public static readonly string brightness = " Brightness";
            public static readonly string focus = " Focus";
            public static readonly string exposure = " Exposure";
            public static readonly string saturation = " Saturation";
            public static readonly string sharpness = " Sharpness";
            public static readonly string gain = " Gain";
            public static readonly string gamma = " Gamma";
        }
        public static class HeadConfig {
            public static readonly string head = "Head ";
            public static readonly string port = " Port";
            public static readonly string zoom = " Zoom";
            public static readonly string pan = " Pan";
            public static readonly string tilt = " Tilt";
            public static readonly string contrast = " Contrast";
            public static readonly string brightness = " Brightness";
            public static readonly string focus = " Focus";
            public static readonly string process = " Process";
            public static readonly string exposure = " Exposure";
            public static readonly string saturation = " Saturation";
            public static readonly string sharpness = " Sharpness";
            public static readonly string gain = " Gain";
            public static readonly string gamma = " Gamma";
            public static readonly string hsvFlag = " Hsv Flag";
            public static readonly string hsvFormat = " Hsv Format";
            public static readonly string hsvMask = " Hsv Mask";
            public static readonly string hsvTimeOut = " Hsv TimeOut";
            public static readonly string frameHeight = " Frame Height";
            public static readonly string frameWidth = " Frame Width";
            public static readonly string rectHeight = " Rect Height";
            public static readonly string rectWidth = " Rect Width";
            public static readonly string rectX = " Rect X";
            public static readonly string rectY = " Rect Y";
            public static readonly string roiMove = " Roi Move";
            public static readonly string roiMoveSet = " Roi Move Set";
            public static readonly string startTimeOut = " Start Time Out";
            public static readonly string autoScaleLimit = " Auto Scale Limit";
            public static readonly string autoScaleNext = " Auto Scale Next";
            public static readonly string digitSN = " Digit SN";
            public static readonly string adjustDegree = " Adjust Degree";
            public static readonly string setAddress = " Set Address";
        }
        public static class Path {
            public static readonly string head = "../../config/head.txt";
            public static readonly string tricExe = "call_exe_tric.txt";
            public static readonly string setPort = "CameraSetPort.txt";
            public static readonly string getStepTest = "_steptest.txt";
            public static readonly string getDebug = "_debug.txt";
            public static readonly string getTimeOut = "_timeout.txt";
            public static readonly string getAutoFocus = "_autoFocus.txt";
            public static readonly string folder = "../../config/test_head_";
            public static readonly string portRead2d = "_port_read2d.txt";
            public static readonly string port = "_port.txt";
            public static readonly string list = "camera_show_list.txt";
            public static readonly string rectangleX = "RectangleX.txt";
            public static readonly string rectangleY = "RectangleY.txt";
            public static readonly string rectangleWidth = "RectangleWidth.txt";
            public static readonly string rectangleHeight = "RectangleHeight.txt";
        }
        public static class Define {
            public static readonly string setPort = "Set Port";
            public static readonly string canNotOpenCamera = "Can Not Open Camera!!";
        }
        /// <summary>
        /// For select set address
        /// </summary>
        public static class Address {
            public static readonly string gamma = "Gamma";
            public static readonly string port = "Port";
        }
        #endregion

        
    }
}
