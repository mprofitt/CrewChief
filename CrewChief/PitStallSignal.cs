using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChief
{
    class PitStallSignal
    {
        public static float LapDist { get; set; }
        public static float TrackPitSpeedLimit { get; set; }
        public static float DriverPitTrkPct { get; set; }
        public static float TrackLength { get; set; } // Length of track in km
        public static float meters;

        public static float MetersToPitStall()
        {
            var pitStallLocation = DriverPitPos * ToMeters(TrackLength);
            var driverLocation = LapDist * ToMeters(TrackLength);

            if (pitStallLocation < .5 * ToMeters(TrackLength)) // Pit stall is after start / finish line
            {
                if (driverLocation > pitStallLocation) // Driver past pit stall
                {
                    meters = (ToMeters(TrackLength) - driverLocation) + pitStallLocation;
                }
                else if (driverLocation < pitStallLocation) // Driver between pit stall and start / finish line
                {
                    meters = pitStallLocation - driverLocation;
                }
            }
            if (pitStallLocation > .5 * ToMeters(TrackLength)) // Pit stall is before start / finish line
            {
                if (driverLocation > pitStallLocation && driverLocation < ToMeters(TrackLength)) // Driver between pit stall and start / finish line
                {
                    meters = (ToMeters(TrackLength) - driverLocation) + ToMeters(TrackLength);
                }
                else if (driverLocation < pitStallLocation) // Driver between pit stall and start / finish line
                {
                    meters = pitStallLocation - driverLocation;
                }
            }
            return meters;
        }

        private static float ToMeters(float input)
        {
            return input * 1000;
        }
    }
}
