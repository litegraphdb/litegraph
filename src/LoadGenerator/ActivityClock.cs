namespace LoadGenerator
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Generates organic-looking timestamps across a time window using a Poisson-style process
    /// modulated by a diurnal curve (busy working hours, quiet nights) plus random bursts.
    /// This class is not thread-safe; use one instance per thread.
    /// </summary>
    public class ActivityClock
    {
        #region Public-Members

        /// <summary>
        /// Inclusive start of the activity window, in UTC.
        /// </summary>
        public DateTime WindowStartUtc
        {
            get
            {
                return _WindowStartUtc;
            }
        }

        /// <summary>
        /// Exclusive end of the activity window, in UTC.
        /// </summary>
        public DateTime WindowEndUtc
        {
            get
            {
                return _WindowEndUtc;
            }
        }

        #endregion

        #region Private-Members

        private DateTime _WindowStartUtc;
        private DateTime _WindowEndUtc;
        private Random _Random;
        private List<DateTime> _BurstCenters = new List<DateTime>();
        private List<double> _BurstSigmasSeconds = new List<double>();
        private double _BurstProbability = 0.18;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="random">Random number generator.</param>
        /// <param name="windowStartUtc">Inclusive start of the activity window, in UTC.</param>
        /// <param name="windowEndUtc">Exclusive end of the activity window, in UTC.  Must be later than the start.</param>
        /// <exception cref="ArgumentNullException">Thrown when the random number generator is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the window end is not later than the window start.</exception>
        public ActivityClock(Random random, DateTime windowStartUtc, DateTime windowEndUtc)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (windowEndUtc <= windowStartUtc) throw new ArgumentException("Window end must be later than window start.");

            _Random = random;
            _WindowStartUtc = windowStartUtc;
            _WindowEndUtc = windowEndUtc;

            int burstCount = Math.Max(1, (int)Math.Round((windowEndUtc - windowStartUtc).TotalDays));
            double windowSeconds = (windowEndUtc - windowStartUtc).TotalSeconds;

            for (int i = 0; i < burstCount; i++)
            {
                _BurstCenters.Add(windowStartUtc.AddSeconds(_Random.NextDouble() * windowSeconds));
                _BurstSigmasSeconds.Add(300.0 + (_Random.NextDouble() * 900.0));
            }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Generate a sorted list of organic timestamps within the window.
        /// Most samples follow the diurnal curve; a fraction cluster around random burst centers.
        /// </summary>
        /// <param name="count">Number of timestamps to generate.  Minimum is 0.</param>
        /// <returns>Sorted list of UTC timestamps.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative.</exception>
        public List<DateTime> GenerateTimestamps(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            List<DateTime> result = new List<DateTime>(count);

            for (int i = 0; i < count; i++)
            {
                result.Add(NextTimestamp());
            }

            result.Sort();
            return result;
        }

        /// <summary>
        /// Generate a single organic timestamp within the window.
        /// </summary>
        /// <returns>UTC timestamp.</returns>
        public DateTime NextTimestamp()
        {
            if (_Random.NextDouble() < _BurstProbability) return SampleBurst();
            return SampleDiurnal();
        }

        #endregion

        #region Private-Methods

        private DateTime SampleDiurnal()
        {
            double windowSeconds = (_WindowEndUtc - _WindowStartUtc).TotalSeconds;

            while (true)
            {
                DateTime candidate = _WindowStartUtc.AddSeconds(_Random.NextDouble() * windowSeconds);
                double intensity = DiurnalIntensity(candidate);
                if (_Random.NextDouble() < intensity) return candidate;
            }
        }

        private DateTime SampleBurst()
        {
            int index = _Random.Next(_BurstCenters.Count);
            DateTime center = _BurstCenters[index];
            double sigma = _BurstSigmasSeconds[index];

            double offset = NextGaussian() * sigma;
            DateTime candidate = center.AddSeconds(offset);

            if (candidate < _WindowStartUtc || candidate >= _WindowEndUtc) return SampleDiurnal();
            return candidate;
        }

        private double DiurnalIntensity(DateTime utc)
        {
            double hour = utc.TimeOfDay.TotalHours;

            double morningPeak = Math.Exp(-Math.Pow(hour - 10.5, 2.0) / (2.0 * Math.Pow(2.2, 2.0)));
            double afternoonPeak = 0.85 * Math.Exp(-Math.Pow(hour - 15.5, 2.0) / (2.0 * Math.Pow(2.5, 2.0)));

            double intensity = 0.12 + (0.88 * Math.Min(1.0, morningPeak + afternoonPeak));

            if (utc.DayOfWeek == DayOfWeek.Saturday || utc.DayOfWeek == DayOfWeek.Sunday) intensity *= 0.35;

            return Math.Min(1.0, intensity);
        }

        private double NextGaussian()
        {
            double u1 = 1.0 - _Random.NextDouble();
            double u2 = _Random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        #endregion
    }
}
