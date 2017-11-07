using System.Runtime.InteropServices;

namespace SYSWebSockClient
{
    /// <summary>
    /// Provides fast nearest, lesser and greater power of 2 functions.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct PowerFuncs
    {
        [FieldOffset(0)]
        private float FloatVal;

        [FieldOffset(0)]
        private readonly int IntVal;

        private static PowerFuncs FloatCast;

        #region Bit Twiddlers

        private float GreaterPower2Helper()
        {
            int n = IntVal;

            if (n << 9 == 0)
            {
                // already a power of 2
                return FloatVal;
            }

            n >>= 23; // remove fractional part of floating point number
            n -= 127; // subtract the bias from the exponent

            // regenerate new value
            return 1 << (n + 1);
        }

        private float LesserPower2Helper()
        {
            int n = IntVal;

            // i'm assuming it's quicker to avoid early out test that we did in the GreaterPower2
            // method in this case but it really depends on input data set

            n >>= 23; // remove fractional part of floating point number
            n -= 127; // subtract the bias from the exponent

            // regenerate new value
            return 1 << n;
        }

        private float NearestPower2Helper()
        {
            // not sure if we can do better than this with bit twiddling maybe a LUT would be better
            int n = IntVal;

            if (n << 9 == 0)
            {
                // already a power of 2 so, lower power and upper power are identical
                return FloatVal;
            }

            n >>= 23; // remove fractional part of floating point number
            n -= 127; // subtract the bias from the exponent

            int lower = 1 << n;
            int upper = lower << 1;

            // distance calculation
            float f = FloatVal;

            float dUpper = upper - f;
            float dLower = f - lower;

            if (dUpper > dLower)
                return lower;

            return upper;
        }

        #endregion Bit Twiddlers

        #region API

        /// <summary>
        /// Calculates the next largest (superior) power of 2 of a float.
        /// </summary>
        /// <remarks>
        /// Fractional and negative negative numbers are handled specially. This is very handy for
        /// general purpose grid snapping. 0.12 =&gt; 0.125 and -0.12 =&gt; -0.125 NB. An input value
        /// of 0 is mapped to 0 rather than 1 which is generally more useful.
        /// </remarks>
        /// <param name="f">The input floating point number.</param>
        /// <returns></returns>
        public static float GreaterPower2(float f)
        {
            const float PosEpsilon = float.Epsilon;
            const float NegEpsilon = -float.Epsilon;

            // is it positive?
            if (f > PosEpsilon)
            {
                // fractional?
                if (f < 1.0f)
                {
                    f = 1.0f / f;
                    FloatCast.FloatVal = f;
                    f = FloatCast.LesserPower2Helper();
                    f = 1.0f / f;
                }
                else
                {
                    FloatCast.FloatVal = f;
                    f = FloatCast.GreaterPower2Helper();
                }

                return f;
            }

            if (f < NegEpsilon)
            {
                // negative, so flip the sign
                f = -f;

                // fractional?
                if (f < 1.0f)
                {
                    f = 1.0f / f;
                    FloatCast.FloatVal = f;
                    f = FloatCast.LesserPower2Helper();
                    f = 1.0f / f;
                }
                else
                {
                    FloatCast.FloatVal = f;
                    f = FloatCast.GreaterPower2Helper();
                }

                return -f;
            }

            // close to zero
            return 0.0f;
        }

        /// <summary>
        /// Calculates the next lower (inferior) power of 2 of a float.
        /// </summary>
        /// <remarks>
        /// Fractional and negative negative numbers are handled specially. This is very handy for
        /// general purpose grid snapping. For example 0.127 =&gt; 0.125 and -0.127 =&gt; -0.125 NB.
        /// An input value of 0 is mapped to 0 rather than 1 which is generally more useful.
        /// </remarks>
        /// <param name="f">The input floating point number.</param>
        /// <returns></returns>
        public static float LesserPower2(float f)
        {
            const float PosEpsilon = float.Epsilon;
            const float NegEpsilon = -float.Epsilon;

            // is it positive?
            if (f > PosEpsilon)
            {
                // fractional?
                if (f < 1.0f)
                {
                    f = 1.0f / f;
                    FloatCast.FloatVal = f;
                    f = FloatCast.GreaterPower2Helper();
                    f = 1.0f / f;
                }
                else
                {
                    FloatCast.FloatVal = f;
                    f = FloatCast.LesserPower2Helper();
                }

                return f;
            }

            if (f < NegEpsilon)
            {
                // negative, so flip the sign
                f = -f;

                // fractional?
                if (f < 1.0f)
                {
                    f = 1.0f / f;
                    FloatCast.FloatVal = f;
                    f = FloatCast.GreaterPower2Helper();
                    f = 1.0f / f;
                }
                else
                {
                    FloatCast.FloatVal = f;
                    f = FloatCast.LesserPower2Helper();
                }

                return -f;
            }

            // close to zero
            return 0.0f;
        }

        /// <summary>
        /// Calculates the nearest (closest) power of 2 of a float.
        /// </summary>
        /// <remarks>
        /// Fractional and negative negative numbers are handled specially. This is very handy for
        /// general purpose grid snapping. For example 0.127 =&gt; 0.125 and -0.127 =&gt; -0.125 NB.
        /// An input value of 0 is mapped to 0 rather than 1 which is generally more useful.
        /// </remarks>
        /// <param name="f">The input floating point number.</param>
        /// <returns></returns>
        public static float ClosestPower2(float f)
        {
            const float PosEpsilon = float.Epsilon;
            const float NegEpsilon = -float.Epsilon;

            // is it positive?
            if (f > PosEpsilon)
            {
                // fractional?
                if (f < 1.0f)
                {
                    f = 1.0f / f;
                    FloatCast.FloatVal = f;
                    f = FloatCast.NearestPower2Helper();
                    f = 1.0f / f;
                }
                else
                {
                    FloatCast.FloatVal = f;
                    f = FloatCast.NearestPower2Helper();
                }

                return f;
            }

            if (f < NegEpsilon)
            {
                // negative, so flip the sign
                f = -f;

                // fractional?
                if (f < 1.0f)
                {
                    f = 1.0f / f;
                    FloatCast.FloatVal = f;
                    f = FloatCast.NearestPower2Helper();
                    f = 1.0f / f;
                }
                else
                {
                    FloatCast.FloatVal = f;
                    f = FloatCast.NearestPower2Helper();
                }

                return -f;
            }

            // close to zero
            return 0.0f;
        }

        #endregion API
    }
}