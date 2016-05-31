using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PremiseAlexaBridgeService
{

    internal static class TemperatureMode
    {
        public static string ModeToString(int mode)
        {
            if (mode == 0)
                return "AUTO";
            if (mode == 1)
                return "HEAT";
            if (mode == 2)
                return "COOL";

            return "ERROR";
        }
    }


    internal class Temperature
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
            set { _Kelvin = (value + cAbsTempC) * 5 / 9; }
        }
        public double Kelvin
        {
            get { return Math.Round(_Kelvin,2); }
            set { _Kelvin = value; }
        }
    }
    internal static class HumanFriendlyInteger
    {
        static string[] ones = new string[] { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
        static string[] teens = new string[] { "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
        static string[] tens = new string[] { "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
        static string[] thousandsGroups = { "", " Thousand", " Million", " Billion" };

        private static string FriendlyInteger(int n, string leftDigits, int thousands)
        {
            if (n == 0)
            {
                return leftDigits;
            }
            string friendlyInt = leftDigits;
            if (friendlyInt.Length > 0)
            {
                friendlyInt += " ";
            }

            if (n < 10)
            {
                friendlyInt += ones[n];
            }
            else if (n < 20)
            {
                friendlyInt += teens[n - 10];
            }
            else if (n < 100)
            {
                friendlyInt += FriendlyInteger(n % 10, tens[n / 10 - 2], 0);
            }
            else if (n < 1000)
            {
                friendlyInt += FriendlyInteger(n % 100, (ones[n / 100] + " Hundred"), 0);
            }
            else
            {
                friendlyInt += FriendlyInteger(n % 1000, FriendlyInteger(n / 1000, "", thousands + 1), 0);
            }

            return friendlyInt + thousandsGroups[thousands];
        }

        public static string IntegerToWritten(int n)
        {
            if (n == 0)
            {
                return "Zero";
            }
            else if (n < 0)
            {
                return "Negative " + IntegerToWritten(-n);
            }

            return FriendlyInteger(n, "", 0);
        }
    }

}