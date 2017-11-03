using System;

namespace PremiseAlexaBridgeService
{
    public class Temperature
    {
        #region Fields

        private const double CAbsTempC = 273.15;//absolute temperature in Celsius
        private const double CAbsTempF = 459.67;//absolute temperature in Fahrenheit
        private double _kelvin;

        #endregion Fields

        #region Constructors

        public Temperature()
        {
        }

        public Temperature(double kelvintemp)
        {
            _kelvin = kelvintemp;
        }

        public Temperature(string scale, double value)
        {
            switch (scale)
            {
                case "FAHRENHEIT":
                    Fahrenheit = value;
                    break;

                case "CELSIUS":
                    Celsius = value;
                    break;

                case "KELVIN":
                    Kelvin = value;
                    break;
            }
        }

        #endregion Constructors

        #region Properties

        public double Celsius
        {
            get => Math.Round(_kelvin - CAbsTempC, 2);
            set => _kelvin = value + CAbsTempC;
        }

        public double Fahrenheit
        {
            get => Math.Round(_kelvin * 9 / 5 - CAbsTempF, 2);
            set => _kelvin = (value + CAbsTempF) * 5 / 9;
        }

        public double Kelvin
        {
            get => Math.Round(_kelvin, 2);
            set => _kelvin = value;
        }

        #endregion Properties
    }
}