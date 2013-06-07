using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TTUSAPI_Samples
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
            frmDisableGWAccess disableGWAccess = new frmDisableGWAccess();
            disableGWAccess.initTTUSAPI();
            Application.Run(disableGWAccess);
        }
    }
}
