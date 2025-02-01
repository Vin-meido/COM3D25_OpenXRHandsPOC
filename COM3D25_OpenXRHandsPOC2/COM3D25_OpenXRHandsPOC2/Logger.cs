using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COM3D25_OpenXRHandsPOC2
{
    internal class Logger
    {
        public static void LogInfo(string message)
        {
            COM3D25_OpenXRHandsPOC2Plugin.LogInfo(message);
        }
    }
}
