using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ECExperiments.ECC
{
    public class WeierstrasCurve
    {
        public static readonly WeierstrasCurve secp256k1 = new WeierstrasCurve(
            BigInteger.Parse("0", System.Globalization.NumberStyles.HexNumber),
            BigInteger.Parse("07", System.Globalization.NumberStyles.HexNumber),
            BigInteger.Parse("0fffffffffffffffffffffffffffffffffffffffffffffffffffffffefffffc2f", System.Globalization.NumberStyles.HexNumber),
            BigInteger.Parse("0fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141", System.Globalization.NumberStyles.HexNumber),
            BigInteger.Parse("01", System.Globalization.NumberStyles.HexNumber),
            BigPoint.Make(
                BigInteger.Parse("079be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798", System.Globalization.NumberStyles.HexNumber),
                BigInteger.Parse("0483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4b8", System.Globalization.NumberStyles.HexNumber)));

        public WeierstrasCurve(BigInteger a, BigInteger b, BigInteger prime, BigInteger order, BigInteger cofactor, BigPoint generatorPoint)
        {
            A = a;
            B = b;
            Prime = prime;
            Order = order;
            Cofactor = cofactor;
            GeneratorPoint = generatorPoint;
        }

        public BigInteger A { get; init; }

        public BigInteger B { get; init; }

        public BigInteger Prime { get; init; }

        public BigInteger Order { get; init; }

        public BigInteger Cofactor { get; init; }

        public BigPoint GeneratorPoint { get; init; }

        public bool IsOnCurve(BigPoint point)
        {
            return (BigInteger.Pow(point.X, 3) + A * point.X + B - BigInteger.Pow(point.Y, 2)) % Prime == 0;
        }
        public BigPoint MultiplyPoint(BigPoint point, BigInteger multiple)
        {
            BigPoint temp = point;
            BigPoint result = BigPoint.Zero;
            while (multiple > 0)
            {
                if ((multiple & 1) == 1)
                {
                    result = PointAddition(result, temp);
                }

                temp = PointAddition(temp, temp);
                multiple >>= 1;
            }

            return result;
        }

        public BigPoint PointAddition(BigPoint P, BigPoint Q)
        {
            // Adapted from https://stackoverflow.com/questions/31074172/elliptic-curve-point-addition-over-a-finite-field-in-python

            if (P == BigPoint.Zero)
            {
                // the empty point + a point
                return Q;
            }

            if (Q == BigPoint.Zero)
            {
                // a point + the empty point
                return P;
            }

            BigInteger slope;
            if (P.X == Q.X)
            {
                if (P.Y != Q.Y)
                {
                    // a point minus itself
                    return BigPoint.Zero;
                }

                slope = (3 * BigInteger.Pow(P.X, 2) + A) * Utils.InverseMod(2 * P.Y, Prime);
            }
            else
            {
                slope = (Q.Y - P.Y) * Utils.InverseMod(Q.X - P.X, Prime);
            }

            BigInteger x = (BigInteger.Pow(slope, 2) - P.X - Q.X) % Prime;
            BigInteger y = (slope * (P.X - x) - P.Y) % Prime;

            if (y < 0)
            {
                y += Prime;
            }

            return BigPoint.Make(x, y);
        }
    }
}
