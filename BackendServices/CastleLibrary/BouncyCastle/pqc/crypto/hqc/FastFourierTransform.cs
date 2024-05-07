using System;

namespace Org.BouncyCastle.Pqc.Crypto.Hqc
{
    internal class FastFourierTransform
    {
        internal static void FFT(int[] output, int[] elements, int noCoefs, int fft)
        {
            int m = HqcParameters.PARAM_M;
            int mSize = 1 << (HqcParameters.PARAM_M - 1);

            int fftSize = 1 << fft;

            int[] f0 = new int[fftSize];
            int[] f1 = new int[fftSize];
            int[] deltas = new int[m - 1];
            int[] u = new int[mSize];
            int[] v = new int[mSize];

            // Step 1: Compute betas
            int[] betas = new int[m - 1];
            int[] betaSum = new int[mSize];

            ComputeFFTBetas(betas, m);
            ComputeSubsetSum(betaSum, betas, m - 1);

            // Step 2: Compute radix
            ComputeRadix(f0, f1, elements, fft, fft);

            // Step 3: Compute deltas
            for (int i = 0; i < m - 1; i++)
            {
                deltas[i] = GFCalculator.mult(betas[i], betas[i]) ^ betas[i];
            }

            // Step 5:
            ComputeFFTRec(u, f0, (noCoefs + 1) / 2, m - 1, fft - 1, deltas, fft, m);
            ComputeFFTRec(v, f1, noCoefs / 2, m - 1, fft - 1, deltas, fft, m);

            // Step 6.7
            int k = 1;
            k = 1 << (m - 1);

            Array.Copy(v, 0, output, k, k);

            output[0] = u[0];
            output[k] ^= u[0];

            for (int i = 1; i < k; i++)
            {
                output[i] = u[i] ^ GFCalculator.mult(betaSum[i], v[i]);
                output[k + i] ^= output[i];
            }
        }

        internal static void ComputeFFTBetas(int[] betas, int m)
        {
            for (int i = 0; i < m - 1; i++)
            {
                betas[i] = 1 << (m - 1 - i);
            }
        }

        internal static void ComputeSubsetSum(int[] subsetSum, int[] set, int size)
        {
            subsetSum[0] = 0;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < (1 << i); j++)
                {
                    subsetSum[(1 << i) + j] = set[i] ^ subsetSum[j];
                }
            }
        }

        internal static void ComputeRadix(int[] f0, int[] f1, int[] f, int mf, int fft)
        {
            switch (mf)
            {
                case 4:
                    f0[4] = f[8] ^ f[12];
                    f0[6] = f[12] ^ f[14];
                    f0[7] = f[14] ^ f[15];
                    f1[5] = f[11] ^ f[13];
                    f1[6] = f[13] ^ f[14];
                    f1[7] = f[15];
                    f0[5] = f[10] ^ f[12] ^ f1[5];
                    f1[4] = f[9] ^ f[13] ^ f0[5];

                    f0[0] = f[0];
                    f1[3] = f[7] ^ f[11] ^ f[15];
                    f0[3] = f[6] ^ f[10] ^ f[14] ^ f1[3];
                    f0[2] = f[4] ^ f0[4] ^ f0[3] ^ f1[3];
                    f1[1] = f[3] ^ f[5] ^ f[9] ^ f[13] ^ f1[3];
                    f1[2] = f[3] ^ f1[1] ^ f0[3];
                    f0[1] = f[2] ^ f0[2] ^ f1[1];
                    f1[0] = f[1] ^ f0[1];
                    return;

                case 3:
                    f0[0] = f[0];
                    f0[2] = f[4] ^ f[6];
                    f0[3] = f[6] ^ f[7];
                    f1[1] = f[3] ^ f[5] ^ f[7];
                    f1[2] = f[5] ^ f[6];
                    f1[3] = f[7];
                    f0[1] = f[2] ^ f0[2] ^ f1[1];
                    f1[0] = f[1] ^ f0[1];
                    return;

                case 2:
                    f0[0] = f[0];
                    f0[1] = f[2] ^ f[3];
                    f1[0] = f[1] ^ f0[1];
                    f1[1] = f[3];
                    return;

                case 1:
                    f0[0] = f[0];
                    f1[0] = f[1];
                    return;

                default:
                    ComputeRadixBig(f0, f1, f, mf, fft);
                    break;
            }
        }

        internal static void ComputeRadixBig(int[] f0, int[] f1, int[] f, int mf, int fft)
        {
            int n = 1;
            n <<= (mf - 2);
            int fftSize = 1 << (fft - 2);

            int[] Q = new int[2 * fftSize];
            int[] R = new int[2 * fftSize];

            int[] Q0 = new int[fftSize];
            int[] Q1 = new int[fftSize];
            int[] R0 = new int[fftSize];
            int[] R1 = new int[fftSize];


            Utils.CopyBytes(f, 3 * n, Q, 0, 2 * n);
            Utils.CopyBytes(f, 3 * n, Q, n, 2 * n);
            Utils.CopyBytes(f, 0, R, 0, 4 * n);

            for (int i = 0; i < n; ++i)
            {
                Q[i] ^= f[2 * n + i];
                R[n + i] ^= Q[i];
            }

            ComputeRadix(Q0, Q1, Q, mf - 1, fft);
            ComputeRadix(R0, R1, R, mf - 1, fft);

            Utils.CopyBytes(R0, 0, f0, 0, 2 * n);
            Utils.CopyBytes(Q0, 0, f0, n, 2 * n);
            Utils.CopyBytes(R1, 0, f1, 0, 2 * n);
            Utils.CopyBytes(Q1, 0, f1, n, 2 * n);
        }

        internal static void ComputeFFTRec(int[] output, int[] func, int noCoeffs, int noOfBetas, int noCoeffsPlus, int[] betaSet, int fft, int m)
        {
            int fftSize = 1 << (fft - 2);
            int mSize = 1 << (m - 2);

            int[] fx0 = new int[fftSize];
            int[] fx1 = new int[fftSize];
            int[] gammaSet = new int[m - 2];
            int[] deltaSet = new int[m - 2];
            int k = 1;
            int[] gammaSumSet = new int[mSize];
            int[] uSet = new int[mSize];
            int[] vSet = new int[mSize];
            int[] tempSet = new int[m - fft + 1];

            int x = 0;
            if (noCoeffsPlus == 1)
            {
                for (int i = 0; i < noOfBetas; i++)
                {
                    tempSet[i] = GFCalculator.mult(betaSet[i], func[1]);
                }

                output[0] = func[0];
                x = 1;
                for (int j = 0; j < noOfBetas; j++)
                {
                    for (int t = 0; t < x; t++)
                    {
                        output[x + t] = output[t] ^ tempSet[j];
                    }
                    x <<= 1;
                }
                return;
            }

            if (betaSet[noOfBetas - 1] != 1)
            {
                int betaMPow = 1;
                x = 1;
                x <<= noCoeffsPlus;
                for (int i = 1; i < x; i++)
                {
                    betaMPow = GFCalculator.mult(betaMPow, betaSet[noOfBetas - 1]);
                    func[i] = GFCalculator.mult(betaMPow, func[i]);
                }
            }

            ComputeRadix(fx0, fx1, func, noCoeffsPlus, fft);

            for (int i = 0; i < noOfBetas - 1; i++)
            {
                gammaSet[i] = GFCalculator.mult(betaSet[i], GFCalculator.inverse(betaSet[noOfBetas - 1]));
                deltaSet[i] = GFCalculator.mult(gammaSet[i], gammaSet[i]) ^ gammaSet[i];
            }

            ComputeSubsetSum(gammaSumSet, gammaSet, noOfBetas - 1);

            ComputeFFTRec(uSet, fx0, (noCoeffs + 1) / 2, noOfBetas - 1, noCoeffsPlus - 1, deltaSet, fft, m);

            k = 1;
            k <<= ((noOfBetas - 1) & 0xf);
            if (noCoeffs <= 3)
            {
                output[0] = uSet[0];
                output[k] = uSet[0] ^ fx1[0];
                for (int i = 1; i < k; i++)
                {
                    output[i] = uSet[i] ^ GFCalculator.mult(gammaSumSet[i], fx1[0]);
                    output[k + i] = output[i] ^ fx1[0];
                }
            }
            else
            {
                ComputeFFTRec(vSet, fx1, noCoeffs / 2, noOfBetas - 1, noCoeffsPlus - 1, deltaSet, fft, m);

                Array.Copy(vSet, 0, output, k, k);

                output[0] = uSet[0];
                output[k] ^= uSet[0];
                for (int i = 1; i < k; i++)
                {
                    output[i] = uSet[i] ^ GFCalculator.mult(gammaSumSet[i], vSet[i]);
                    output[k + i] ^= output[i];
                }


            }
        }

        internal static void FastFourierTransformGetError(byte[] errorSet, int[] input, int mSize, int[] logArrays)
        {
            int m = HqcParameters.PARAM_M;
            int gfMulOrder = HqcParameters.GF_MUL_ORDER;

            int[] gammaSet = new int[m - 1];
            int[] gammaSumSet = new int[mSize];
            int k = mSize;

            ComputeFFTBetas(gammaSet, m);
            ComputeSubsetSum(gammaSumSet, gammaSet, m - 1);

            errorSet[0] ^= (byte) (1 ^ Utils.ToUnsigned16Bits(-input[0] >> 15));
            errorSet[0] ^= (byte) (1 ^ Utils.ToUnsigned16Bits(-input[k] >> 15));

            for (int i = 1; i < k; i++)
            {
                int tmp = gfMulOrder - logArrays[gammaSumSet[i]];
                errorSet[tmp] ^= (byte) (1 ^ System.Math.Abs(-input[i] >> 15));

                tmp = gfMulOrder - logArrays[gammaSumSet[i] ^ 1];
                errorSet[tmp] ^= (byte) (1 ^ System.Math.Abs(-input[k + i] >> 15));
            }
        }
    }
}
