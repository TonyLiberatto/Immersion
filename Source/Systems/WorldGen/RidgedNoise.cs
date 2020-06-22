using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

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

    /*
    public class TestModSystem1 : ModSystem
    {
        public override void StartServerSide(ICoreServerAPI api)
        {
            Bitmap bitmap = new Bitmap(512, 512);
            RidgedSimplexNoise ridged = RidgedSimplexNoise.FromDefaultOctaves(1, 0.1, 1.0, api.WorldManager.Seed + 465814);
            for (int x = 0; x < 512; x++)
            {
                for (int y = 0; y < 512; y++)
                {
                    double n = ridged.InvNoise(x, y);
                    Color c = Color.FromArgb((int)(n * 255), (int)(n * 255), (int)(n * 255));
                    bitmap.SetPixel(x, y, c);
                }
            }
            bitmap.Save("test.png", ImageFormat.Png);
        }
    }
    */

    public class RidgedSimplexNoise
    {
        public SimplexNoiseOctave[] octaves;

        public double[] amplitudes;
        public double[] frequencies;
        long seed;

        public RidgedSimplexNoise(double[] amplitudes, double[] frequencies, long seed)
        {
            this.amplitudes = amplitudes;
            this.frequencies = frequencies;
            this.seed = seed;

            octaves = new SimplexNoiseOctave[amplitudes.Length];

            for (int i = 0; i < octaves.Length; i++)
            {
                octaves[i] = new SimplexNoiseOctave(seed * 65599 + i);
            }
        }

        public static RidgedSimplexNoise FromDefaultOctaves(int quantityOctaves, double baseFrequency, double persistence, long seed)
        {
            double[] frequencies = new double[quantityOctaves];
            double[] amplitudes = new double[quantityOctaves];

            for (int i = 0; i < quantityOctaves; i++)
            {
                frequencies[i] = Math.Pow(2, i) * baseFrequency;
                amplitudes[i] = Math.Pow(persistence, i);
            }

            return new RidgedSimplexNoise(amplitudes, frequencies, seed);
        }

        public virtual double Noise(double x, double y)
        {
            double value = 0;

            for (int i = 0; i < amplitudes.Length; i++)
            {
                value += octaves[i].Evaluate(x * frequencies[i], y * frequencies[i]) * amplitudes[i];
            }

            return Math.Abs(value);
        }

        public virtual double InvNoise(double x, double y) => 1.0 - Noise(x, y);

        public double Noise(double x, double y, double[] thresholds)
        {
            double value = 0;

            for (int i = 0; i < amplitudes.Length; i++)
            {
                double val = octaves[i].Evaluate(x * frequencies[i], y * frequencies[i]) * amplitudes[i];
                value += (val > 0 ? Math.Max(0, val - thresholds[i]) : Math.Min(0, val + thresholds[i]));
            }

            return Math.Abs(value);
        }

        public virtual double InvNoise(double x, double y, double[] thresholds) => 1.0 - Noise(x, y, thresholds);

        public virtual double Noise(double x, double y, double z)
        {
            double value = 0;

            for (int i = 0; i < amplitudes.Length; i++)
            {
                value += octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i]) * amplitudes[i];
            }

            return Math.Abs(value);
        }

        public virtual double InvNoise(double x, double y, double z) => 1.0 - Noise(x, y, z);

        public SimplexNoise Clone()
        {
            return new SimplexNoise((double[])amplitudes.Clone(), (double[])frequencies.Clone(), seed);
        }
    }
}
