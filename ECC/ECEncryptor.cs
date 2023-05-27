using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ECExperiments.ECC
{
    public class ECEncryptor
    {
        private BigPoint? _publicKey;
        private int? _primeLength;
        private int? _orderLength;

        public ECEncryptor(WeierstrasCurve curve)
        {
            Curve = curve;
        }

        public WeierstrasCurve Curve { get; init; }

        public BigInteger PrivateKey { get; private set; }

        #region Key Methods

        public BigPoint GetPublicKey()
        {
            if (_publicKey.HasValue)
            {
                return _publicKey.Value;
            }

            if(PrivateKey == 0)
            {
                throw new Exception("Cannot generate public key. Private key has not been set.");
            }

            _publicKey = Curve.MultiplyPoint(Curve.GeneratorPoint, PrivateKey);

            return _publicKey.Value;
        }

        public void SetPrivateKey(BigInteger privateKey)
        {
            if(privateKey >= Curve.Prime || privateKey == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(privateKey));
            }

            PrivateKey = privateKey;
        }

        public void SetPublicKey(BigPoint publicKey)
        {
            if (!Curve.IsOnCurve(publicKey))
            {
                throw new ArgumentException("The specified point is not part of the curve", nameof(publicKey));
            }

            if(publicKey == BigPoint.Zero)
            {
                throw new ArgumentException("The specified point is the infinity point", nameof(publicKey));
            }

            _publicKey = publicKey;
        }

        public void ImportPublicKey(byte[] data)
        {
            if(data == null)
            {
                throw new ArgumentNullException();
            }

            if(data.Length == 0)
            {
                throw new ArgumentException("Array cannot be empty.", nameof(data));
            }

            BigInteger x;
            BigInteger y;
            if (data[0] == 0x04)
            {
                int expectedLength = 1 + 2 * GetPrimeLength();
                if (data.Length != expectedLength)
                {
                    throw new ArgumentException($"Expected uncompressed array of length {expectedLength}. An array of length {data.Length}.", nameof(data));
                }

                x = Utils.ReadUnsignedBigEndianInt(data, 1, GetPrimeLength());
                y = Utils.ReadUnsignedBigEndianInt(data, 1 + GetPrimeLength(), GetPrimeLength());
            }
            else
            {
                if (data[0] != 0x02 && data[0] != 0x03)
                {
                    throw new ArgumentException("Key prefix not recognized.", nameof(data));
                }

                bool shouldBeOdd = data[0] == 0x03;

                int expectedLength = 1 + GetPrimeLength();
                if (data.Length != expectedLength)
                {
                    throw new ArgumentException($"Expected compressed array of length {expectedLength}. An array of length {data.Length}.", nameof(data));
                }

                x = Utils.ReadUnsignedBigEndianInt(data, 1, GetPrimeLength());
                y = Utils.ModSquareRoot(BigInteger.ModPow(x, 3, Curve.Prime) + Curve.A * x + Curve.B, Curve.Prime);
                bool isOdd = (y & 1) == 1;

                if (shouldBeOdd != isOdd)
                {
                    y = Curve.Prime - y;
                }
            }

            SetPublicKey(BigPoint.Make(x, y));
        }

        public byte[] ExportPublicKey()
        {
            BigPoint key = GetPublicKey();

            byte[] x = Utils.MakeUnsignedBigEndianArray(key.X, GetPrimeLength());
            byte[] y = Utils.MakeUnsignedBigEndianArray(key.Y, GetPrimeLength());

            byte[] result = new byte[x.Length + y.Length + 1];
            result[0] = 0x04;
            x.CopyTo(result, 1);
            y.CopyTo(result, 1 + x.Length);

            return result;
        }

        public byte[] ExportPublicKeyCompressed()
        {
            BigPoint key = GetPublicKey();

            byte[] x = Utils.MakeUnsignedBigEndianArray(key.X, GetPrimeLength());
            byte[] result = new byte[x.Length + 1];

            x.CopyTo(result, 1);

            if(key.Y % 2 == 0)
            {
                // Y is even
                result[0] = 0x02;
            }
            else
            {
                // Y is odd
                result[0] = 0x03;
            }

            return result;
        }

        public void ImportPrivateKey(byte[] data)
        {
            BigInteger privateKey = Utils.ReadUnsignedBigEndianInt(data, 0, data.Length);
            
            if(privateKey <= 0 || privateKey >= Curve.Order)
            {
                throw new Exception("The provided key is outside the valid range of values for the curve.");
            }

            SetPrivateKey(privateKey);
        }

        public byte[] ExportPrivateKey()
        {
            if(PrivateKey == 0)
            {
                throw new Exception("Private key has not been set.");
            }

            return Utils.MakeUnsignedBigEndianArray(PrivateKey, GetOrderLength());
        }

        #endregion Key Methods

        #region Signature Methods

        public byte[] SignHash(byte[] hash)
        {
            if(PrivateKey == 0)
            {
                throw new Exception("Private key not specified");
            }

            // Implements https://en.wikipedia.org/wiki/Elliptic_Curve_Digital_Signature_Algorithm

            BigInteger z = Utils.ReadUnsignedBigEndianInt(hash, 0, GetOrderLength());

            BigInteger r;
            BigInteger s = 0;

            RandomNumberGenerator secureRandom = RandomNumberGenerator.Create();

            do
            {
                // Ideally, we'd use a deterministic k. To keep things simple, I'm sticking
                // with the original definition of this step and using a random one.
                BigInteger k = GenerateRandomPrivateKey(secureRandom);
                BigPoint curvePoint = Curve.MultiplyPoint(Curve.GeneratorPoint, k);

                r = curvePoint.X % Curve.Order;
                if(r == 0)
                {
                    continue;
                }

                s = (Utils.InverseMod(k, Curve.Order) * ((z + (r * PrivateKey)) % Curve.Order)) % Curve.Order;
            }
            while (r == 0 || s == 0);

            byte[] rData = Utils.MakeUnsignedBigEndianArray(r, GetOrderLength());
            byte[] sData = Utils.MakeUnsignedBigEndianArray(s, GetOrderLength());
            
            byte[] result = new byte[rData.Length + sData.Length];
            rData.CopyTo(result, 0);
            sData.CopyTo(result, rData.Length);

            return result;
        }

        public bool VerifySignature(byte[] hash, byte[] signature)
        {
            // Implements https://en.wikipedia.org/wiki/Elliptic_Curve_Digital_Signature_Algorithm

            if(signature.Length != GetOrderLength() * 2)
            {
                throw new Exception("Signature is incorrect length");
            }

            // If this point is not on the curve, or it's the infinity point, failures will have curred
            // in setting up the encryptor.
            BigPoint pubicKey = GetPublicKey();

            BigInteger r = Utils.ReadUnsignedBigEndianInt(signature, 0, GetOrderLength());
            BigInteger s = Utils.ReadUnsignedBigEndianInt(signature, GetOrderLength(), GetOrderLength());

            BigInteger z = Utils.ReadUnsignedBigEndianInt(hash, 0, GetOrderLength());

            BigInteger u1 = (z * Utils.InverseMod(s, Curve.Order)) % Curve.Order;
            BigInteger u2 = (r * Utils.InverseMod(s, Curve.Order)) % Curve.Order;

            BigPoint x1y1 = Curve.PointAddition(Curve.MultiplyPoint(Curve.GeneratorPoint, u1), Curve.MultiplyPoint(pubicKey, u2));
            if(x1y1 == BigPoint.Zero)
            {
                return false;
            }

            return r == x1y1.X % Curve.Order;
        }

        #endregion Signature Methods

        #region Helper Methods

        public BigInteger GenerateRandomPrivateKey(RandomNumberGenerator secureRandom = null)
        {
            secureRandom ??= RandomNumberGenerator.Create();
            byte[] data = new byte[GetPrimeLength()];
            BigInteger result;
            do
            {
                secureRandom.GetBytes(data);
                result = Utils.ReadUnsignedBigEndianInt(data, 0, data.Length);
            }
            while (result <= 0 && result >= Curve.Order);

            return result;
        }

        private int GetOrderLength()
        {
            if (_orderLength.HasValue)
            {
                return _orderLength.Value;
            }

            _orderLength = Utils.MakeUnsignedBigEndianArray(Curve.Order).Length;
            return _orderLength.Value;
        }

        private int GetPrimeLength()
        {
            if (_primeLength.HasValue)
            {
                return _primeLength.Value;
            }

            _primeLength = Utils.MakeUnsignedBigEndianArray(Curve.Prime).Length;
            return _primeLength.Value;
        }

        #endregion Helper Methods

    }
}
