using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace camera_show.MiniClass {
    public class EditValueCamDiv3 {

        public EditValueCamDiv3(Form1 main_) {
            main = main_;
        }

        public Form1 main;

        public int time;

        public bool state1;

        public bool state2;

        public Stopwatch stopwatch = new Stopwatch();

        public void Process() {
            if (main.global.stopWatch.ElapsedMilliseconds >= time &
                    main.global.stopWatch.ElapsedMilliseconds < (time * 2) & !state1)
            {
                state1 = true;
                string sFocus = main.setupPay.read_text(main.setPath.headCsv + Form1.HeadConfig.focus, main.setPath.stepCsv);
                string sExposure = main.setupPay.read_text(main.setPath.headCsv + Form1.HeadConfig.exposure, main.setPath.stepCsv);
                main.setCamera.SetCapture(CapProp.Focus, Convert.ToInt32(sFocus) - 1);
                main.setCamera.SetCapture(CapProp.Exposure, Convert.ToInt32(sExposure) - 1);
                stopwatch.Restart();
            }
            if (stopwatch.ElapsedMilliseconds >= 1000 & !state2)
            {
                state2 = true;
                string sFocus = main.setupPay.read_text(main.setPath.headCsv + Form1.HeadConfig.focus, main.setPath.stepCsv);
                string sExposure = main.setupPay.read_text(main.setPath.headCsv + Form1.HeadConfig.exposure, main.setPath.stepCsv);
                main.setCamera.SetCapture(CapProp.Focus, Convert.ToInt32(sFocus));
                main.setCamera.SetCapture(CapProp.Exposure, Convert.ToInt32(sExposure));
            }
        }
    }
}
