using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ECExperiments.ECC;

namespace ECExperiments.Bitcoin
{
    public class WIFUtils
    {
        public static BigInteger WIFToPrivateKey(string privateKey)
        {
            byte[] data = Base58Encoding.DecodeWithCheckSum(privateKey);

            // Per https://en.bitcoin.it/wiki/Wallet_import_format, we don't
            // care about the first byte. Since the key should be a 32 bit
            // integer, we also don't care about any additional bytes if the
            // remaining array is longer than 32. These remaining bytes are
            // related to the public key format.
            return Utils.ReadUnsignedBigEndianInt(data, 1, 32);
        }

        public static string CreateBitcoinAddressString(byte[] publicKey)
        {
            // See https://en.bitcoin.it/wiki/Technical_background_of_version_1_Bitcoin_addresses
            // for steps.

            byte[] firstHash = SHA256.HashData(publicKey);
            // The built in method for RIPEMD160 was dropped in the jump to .NET 5
            // so we have to use a nuget package.
            byte[] ripemdHash = RIPEMD160.Create().ComputeHash(firstHash);

            byte[] ripemdExtended = new byte[ripemdHash.Length + 1];
            ripemdHash.CopyTo(ripemdExtended, 1);

            return Base58Encoding.EncodeWithCheckSum(ripemdExtended);
        }
    }
}
