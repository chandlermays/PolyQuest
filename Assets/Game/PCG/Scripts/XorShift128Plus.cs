using System;
//---------------------------------

namespace PolyQuest.PCG
{
    public class XorShift128Plus
    {
        private ulong[] m_state = new ulong[2];

        /*---------------------------------------------------------------------
        | --- Constructor: Initialize the generator with an optional seed --- |
        ---------------------------------------------------------------------*/
        public XorShift128Plus(ulong seed = 0)
        {
            Seed(seed);
        }

        /*----------------------------------------------------------------
        | --- RandomUInt32: Generate next 32-bit pseudorandom number --- |
        ----------------------------------------------------------------*/
        public uint RandomUInt32()
        {
            return (uint)(RandomUInt64() >> 32);
        }

        /*----------------------------------------------------------------
        | --- RandomUInt64: Generate next 64-bit pseudorandom number --- |
        ----------------------------------------------------------------*/
        public ulong RandomUInt64()
        {
            ulong x = m_state[0];
            ulong y = m_state[1];
            m_state[0] = y;

            x ^= x << 23;
            m_state[1] = x ^ y ^ (x >> 17) ^ (y >> 26);
            return m_state[1] + y;
        }

        /*-----------------------------------------------------------------------
        | --- RandomDouble: Generate next pseudorandom double in [0.0, 1.0] --- |
        -----------------------------------------------------------------------*/
        public double RandomDouble()
        {
            return (RandomUInt64() >> 11) * (1.0 / (1UL << 53));
        }

        /*---------------------------------------------------------------------
        | --- RandomFloat: Generate next pseudorandom float in [0.0, 1.0] --- |
        ---------------------------------------------------------------------*/
        public float RandomFloat()
        {
            return (float)RandomDouble();
        }

        /*--------------------------------------------------------
        | --- RandomBool: Generate next pseudorandom boolean --- |
        --------------------------------------------------------*/
        public bool RandomBool()
        {
            return (RandomUInt64() & 1) == 1;
        }

        /*-----------------------------------------------------------------------
        | --- RandomRange: Generate next pseudorandom integer in [min, max] --- |
        -----------------------------------------------------------------------*/
        public int RandomRange(int min, int max)
        {
            if (min >= max)
                return min;

            ulong range = (ulong)(max - min);

            ulong threshold = (~range + 1) % range;
            ulong randomValue;

            do
            {
                randomValue = RandomUInt64();
            } while (randomValue < threshold);

            return min + (int)(randomValue % range);
        }

        /*---------------------------------------------------------------------
        | --- RandomRange: Generate next pseudorandom float in [min, max] --- |
        ---------------------------------------------------------------------*/
        public float RandomRange(float min, float max)
        {
            if (min >= max)
                return min;

            float t = RandomFloat();
            return min + t * (max - min);
        }

        /*----------------------------------------------------------------------
        | --- RandomRange: Generate next pseudorandom double in [min, max] --- |
        ----------------------------------------------------------------------*/
        public double RandomRange(double min, double max)
        {
            if (min >= max)
                return min;

            double d = RandomDouble();
            return min + d * (max - min);
        }

        /*-----------------------------------------------------------
        | --- Seed: Re-seed the generator with a new seed value --- |
        -----------------------------------------------------------*/
        public void Seed(ulong seed)
        {
            if (seed == 0)
            {
                seed = (ulong)DateTime.Now.Ticks;
            }

            ulong seedState = seed;
            m_state[0] = SplitMax64(ref seedState);
            m_state[1] = SplitMax64(ref seedState);

            if (m_state[0] == 0) m_state[0] = 1;
            if (m_state[1] == 0) m_state[1] = 1;
        }

        /*---------------------------------------------------------------
        | --- SplitMax64: Helper function to generate initial state --- |
        ---------------------------------------------------------------*/
        private ulong SplitMax64(ref ulong state)
        {
            ulong z = (state += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}