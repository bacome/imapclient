using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace testharness2
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmStart());
        }

        public const int kCentreStep = 20;

        public static void Centre(Form pThis, Form pOnThis, List<Form> pGivenThese = null)
        {
            int lTop = pOnThis.Top + pOnThis.Height / 2 - pThis.Height / 2;
            int lLeft = pOnThis.Left + pOnThis.Width / 2 - pThis.Width / 2;

            ZCentreAdjust(ref lTop, ref lLeft, pThis.Height, pThis.Width);

            while (ZCentreCollides(pThis, lTop, lLeft, pGivenThese))
            {
                lTop += kCentreStep;
                lLeft += kCentreStep;
                
                if (ZCentreAdjust(ref lTop, ref lLeft, pThis.Height, pThis.Width))
                {
                    foreach (var lScreen in Screen.AllScreens)
                    {
                        if (lScreen.WorkingArea.Top < lTop) lTop = lScreen.WorkingArea.Top;
                        if (lScreen.WorkingArea.Left < lLeft) lLeft = lScreen.WorkingArea.Left;
                    }

                    while (ZCentreCollides(pThis, lTop, lLeft, pGivenThese))
                    {
                        lTop += kCentreStep;
                        lLeft += kCentreStep;

                        if (ZCentreAdjust(ref lTop, ref lLeft, pThis.Height, pThis.Width))
                        {
                            // failed
                            pThis.Top = lTop;
                            pThis.Left = lLeft;
                            return;
                        }
                    }

                    pThis.Top = lTop;
                    pThis.Left = lLeft;
                    return;
                }
            }

            pThis.Top = lTop;
            pThis.Left = lLeft;
        }

        private static bool ZCentreAdjust(ref int rTop, ref int rLeft, int pHeight, int pWidth)
        {
            int lTop = rTop;
            int lLeft = rLeft;

            int lTopDelta;
            int lLeftDelta;

            // check that the bottom right appears on one of the screens

            lTopDelta = int.MaxValue;
            lLeftDelta = int.MaxValue;

            foreach (var lScreen in Screen.AllScreens)
            {
                int lDelta;

                lDelta = Math.Max(rTop + pHeight - (lScreen.WorkingArea.Top + lScreen.WorkingArea.Height), 0);
                if (lDelta < lTopDelta) lTopDelta = lDelta;

                lDelta = Math.Max(rLeft + pWidth - (lScreen.WorkingArea.Left + lScreen.WorkingArea.Width), 0);
                if (lDelta < lLeftDelta) lLeftDelta = lDelta;
            }

            rTop -= lTopDelta;
            rLeft -= lLeftDelta;

            // check that the top left appears on one of the screens

            lTopDelta = int.MaxValue;
            lLeftDelta = int.MaxValue;

            foreach (var lScreen in Screen.AllScreens)
            {
                int lDelta;

                lDelta = Math.Max(lScreen.WorkingArea.Top - rTop, 0);
                if (lDelta < lTopDelta) lTopDelta = lDelta;

                lDelta = Math.Max(lScreen.WorkingArea.Left - rLeft, 0);
                if (lDelta < lLeftDelta) lLeftDelta = lDelta;
            }

            rTop += lTopDelta;
            rLeft += lLeftDelta;

            // return true if we adjusted both

            return (rTop != lTop && rLeft != lLeft);
        }

        public static bool ZCentreCollides(Form pThis, int lTop, int lLeft, List<Form> pGivenThese = null)
        {
            if (pGivenThese == null) return false;
            foreach (var lForm in pGivenThese) if (!ReferenceEquals(pThis, lForm) && Math.Abs(lForm.Top - lTop) < kCentreStep && Math.Abs(lForm.Left - lLeft) < kCentreStep) return true;
            return false;
        }
    }
}
