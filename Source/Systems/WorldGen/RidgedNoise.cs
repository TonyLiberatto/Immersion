using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.MathTools;

namespace Immersion
{
    class RidgedNoise : NormalizedSimplexNoise
    {
        public RidgedNoise(double[] inputAmplitudes, double[] frequencies, long seed) : base(inputAmplitudes, frequencies, seed)
        {
        }

        public static new RidgedNoise FromDefaultOctaves(int quantityOctaves, double baseFrequency, double persistence, long seed)
        {
            double[] frequencies = new double[quantityOctaves];
            double[] amplitudes = new double[quantityOctaves];

            for (int i = 0; i < quantityOctaves; i++)
            {
                frequencies[i] = Math.Pow(2, i) * baseFrequency;
                amplitudes[i] = Math.Pow(persistence, i);
            }

            return new RidgedNoise(amplitudes, frequencies, seed);
        }



        public override double Noise(double x, double y)
        {
            double value = 0;

            for (int i = 0; i < scaledAmplitudes2D.Length; i++)
            {
                value -= Math.Abs(1.2 * octaves[i].Evaluate(x * frequencies[i], y * frequencies[i]) * scaledAmplitudes2D[i]);
            }

            return Math.Tanh(value) + 1.0;

        }

        public override double Noise(double x, double y, double z)
        {
            double value = 0;

            for (int i = 0; i < scaledAmplitudes3D.Length; i++)
            {
                value -= Math.Abs(1.2 * octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i]) * scaledAmplitudes3D[i]);
            }

            return Math.Tanh(value) + 1.0;
        }

        public override double Noise(double x, double y, double z, double[] amplitudes)
        {
            double value = 0;

            for (int i = 0; i < scaledAmplitudes3D.Length; i++)
            {
                value -= Math.Abs(1.2 * octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i])) * amplitudes[i];
            }
            return Math.Tanh(-Math.Abs(value)) + 1.0;
        }

        public new double Noise(double x, double y, double z, double[] amplitudes, double[] thresholds)
        {
            double value = 0;

            for (int i = 0; i < scaledAmplitudes3D.Length; i++)
            {
                double val = 2.0 * (Math.Abs(octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i])) * - amplitudes[i]) - 1.0;        

                value +=  1.2 * (val > 0 ? Math.Max(0.0, val - thresholds[i]) : Math.Min(0.0, val + thresholds[i]));
            }
            return Math.Tanh(value) / 2 + 0.5;
        }
    }
}
