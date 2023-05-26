using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ECExperiments.ECC
{
    public struct BigPoint
    {
        public static readonly BigPoint Zero = Make(0, 0);

        public static BigPoint Make(int x, int y)
        {
            return new BigPoint(x, y);
        }

        public static BigPoint Make(BigInteger x, BigInteger y)
        {
            return new BigPoint(x, y);
        }

        public BigPoint(BigInteger x, BigInteger y)
        {
            X = x;
            Y = y;
        }

        public BigInteger X { get; set; }

        public BigInteger Y { get; set; }

        #region Equality Override

        public override bool Equals(object obj)
        {
            return obj is BigPoint other && Equals(other);
        }

        public bool Equals(BigPoint point)
        {
            return X == point.X && Y == point.Y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(BigPoint lhs, BigPoint rhs) => lhs.Equals(rhs);

        public static bool operator !=(BigPoint lhs, BigPoint rhs) => !(lhs == rhs);

        #endregion Equality Override

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
