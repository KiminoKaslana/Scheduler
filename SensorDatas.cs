using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler
{
    class SensorDatas
    {
        public static SensorDatas? instance = null;
        public bool IsDataValid { get; set; } = false;
        public double Humidity { get; set; } = 0;
        public double Temperature { get; set; } = 0;
        public int AirQuality { get; set; } = 0;
    }
}
