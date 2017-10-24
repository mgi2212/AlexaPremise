using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PremiseAlexaBridgeService
{

    public static class TemperatureMode
    {
        public static string ModeToString(int mode)
        {
            if (mode == 0)
                return "AUTO";
            if (mode == 1)
                return "HEAT";
            if (mode == 2)
                return "COOL";
            if (mode == 4)
                return "OFF";

            return "ERROR";
        }
    }

    public static class RoomMode
    {
        public static string ModeToString(int mode)
        {
            if (mode == -3)
                return "Freeze";
            if (mode == -2)
                return "Clean";
            if (mode == -1)
                return "Idle";
            if (mode == 0)
                return "Unoccupied";
            if (mode == 1)
                return "Morning";
            if (mode == 2)
                return "Day";
            if (mode == 3)
                return "Evening";
            if (mode == 4)
                return "Night";
            if (mode == 5)
                return "Sleep";

            return "ERROR";
        }
    }



    public class Temperature
    {
        private const double cAbsTempC = 273.15;//absolute temperature in Celcius
        private const double cAbsTempF = 459.67;//absolute temperature in Fahrenheit
        private double _Kelvin = 0.0;
        public Temperature() { }
        public Temperature(double kelvintemp)
        {
            _Kelvin = kelvintemp;
        }
        public double Celcius
        {
            get { return Math.Round(_Kelvin - cAbsTempC, 2); }
            set { _Kelvin = value + cAbsTempC; }
        }
        public double Fahrenheit
        {
            get { return Math.Round(_Kelvin * 9 / 5 - cAbsTempF, 2); }
            set { _Kelvin = (value + cAbsTempF) * 5 / 9; }
        }
        public double Kelvin
        {
            get { return Math.Round(_Kelvin, 2); }
            set { _Kelvin = value; }
        }
    }
}