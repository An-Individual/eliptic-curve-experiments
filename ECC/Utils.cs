using System.Numerics;

namespace ECExperiments.ECC
{
    public static class Utils
    {
        public static byte[] MakeUnsignedBigEndianArray(BigInteger n, int? arraySize = null)
        {
            byte[] littleEndian = n.ToByteArray();

            if (arraySize.HasValue)
            {
                littleEndian = littleEndian.Take(arraySize.Value).ToArray();
                if (littleEndian.Length < arraySize.Value)
                {
                    byte[] temp = new byte[arraySize.Value];
                    littleEndian.CopyTo(temp, 0);
                    littleEndian = temp;
                }
            }
            // If the most significant digit is 0 then it was just
            // tacked on for signing reasons
            else if (littleEndian[littleEndian.Length - 1] == 0)
            {
                littleEndian = littleEndian.Take(littleEndian.Length - 1).ToArray();
            }

            return littleEndian.Reverse().ToArray();
        }

        public static BigInteger ReadUnsignedBigEndianInt(byte[] bytes, int idx, int length)
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(bytes, idx, length);

            return new BigInteger(span, true, true);
        }

        public static BigInteger InverseMod(BigInteger value, BigInteger mod)
        {
            // Adapted from https://stackoverflow.com/questions/31074172/elliptic-curve-point-addition-over-a-finite-field-in-python
            if (value % mod == 0)
            {
                throw new DivideByZeroException();
            }

            return BigInteger.ModPow(value, mod - 2, mod);
        }

        public static BigInteger ModSquareRoot(BigInteger value, BigInteger modPrime)
        {
            // Implementation of https://en.wikipedia.org/wiki/Tonelli%E2%80%93Shanks_algorithm
            // https://eli.thegreenplace.net/2009/03/07/computing-modular-square-roots-in-python
            // helped clarify some points and identify the modPrime % 4 == 3 case.

            // Make sure the provided value is a quadratic residue.
            if (!IsQuadraticResidue(value, modPrime))
            {
                throw new Exception($"{value} is not a quadratic residue of {modPrime}");
            }

            // This method doesn't work for value 0
            if (value == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            // This method only works for odd primes.
            if (modPrime == 2)
            {
                throw new ArgumentOutOfRangeException(nameof(modPrime));
            }

            // A special case that can be handled very quickly. This will be the case for secp256k1.
            if (modPrime % 4 == 3)
            {
                return BigInteger.ModPow(value, (modPrime + 1) / 4, modPrime);
            }

            // This part is factoring p-1 into Q * 2 ^ S.
            BigInteger S = 1;
            BigInteger Q = (modPrime - 1) / 2;
            while (Q % 2 == 0)
            {
                S++;
                Q /= 2;
            }

            // This is the part where we search for a quadratic non-residue. Which shouldn't be
            // hard to find as about half the values we check should fall into that category.
            BigInteger z = 2;
            while (IsQuadraticResidue(z, modPrime))
            {
                z += 1;
            }

            BigInteger M = S;
            BigInteger c = BigInteger.ModPow(z, Q, modPrime);
            BigInteger t = BigInteger.ModPow(value, Q, modPrime);
            BigInteger R = BigInteger.ModPow(value, (Q + 1) / 2, modPrime);

            while (true)
            {
                if(t == 0)
                {
                    return 0;
                }

                if(t == 1)
                {
                    return R;
                }

                BigInteger i;
                BigInteger temp = t;
                for(i = 0; i <= M; i++)
                {
                    if (temp == 1)
                    {
                        break;
                    }

                    temp = BigInteger.ModPow(temp, 2, modPrime);
                }

                // The inner ModPow should probably just be a Pow. But BigInteger.Pow() takes an int as it's
                // exponent, which concerns me, and I'm relatively confident that this calculation shouldn't
                // overrun the prime anyway so I decided to do it this way.
                BigInteger b = BigInteger.ModPow(c, BigInteger.ModPow(2, (M - i - 1), modPrime), modPrime);
                M = i;
                c = BigInteger.ModPow(b, 2, modPrime);
                t = t * c % modPrime;
                R = R * b % modPrime;
            }
        }

        private static bool IsQuadraticResidue(BigInteger value, BigInteger modPrime)
        {
            BigInteger symbol = BigInteger.ModPow(value, (modPrime - 1) / 2, modPrime);
            return symbol == 1;
        }
    }
}
