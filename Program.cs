using ECExperiments.Bitcoin;
using ECExperiments.ECC;
using System.Numerics;

namespace ECExperiments
{
    internal class Program
    {
        private const string PLAYGROUND = "playground";
        private const string WIF_PARSER = "wifparser";
        private const string SIGNER = "signer";
        private const string VALIDATOR = "validator";
        private const string ENCRYPTOR = "encryptor";
        private const string DECRYPTOR = "decryptor";

        private static readonly string[] EXPERIMENTS = new string[]
        {
            PLAYGROUND,
            WIF_PARSER,
            SIGNER,
            VALIDATOR,
            ENCRYPTOR,
            DECRYPTOR,
        };

        static void Main(string[] args)
        {
            string experiment = null;
            if(args.Length > 0)
            {
                experiment = args[0]?.ToLowerInvariant()?.Trim();

                if(!EXPERIMENTS.Contains(experiment))
                {
                    Console.WriteLine("Experiment not recognized.");
                }

                args = args.Skip(1).ToArray();
            }
            else
            {
                Console.WriteLine("Which experiment would you like to run?");
                Console.WriteLine(PLAYGROUND);
                Console.WriteLine(WIF_PARSER);
                Console.WriteLine(SIGNER);
                Console.WriteLine(VALIDATOR);
                Console.WriteLine(ENCRYPTOR);
                Console.WriteLine(DECRYPTOR);

                do
                {
                    if(experiment != null)
                    {
                        Console.WriteLine("Value not recognized. Please enter a valid experiment name.");
                    }

                    experiment = Console.ReadLine()?.ToLowerInvariant()?.Trim() ?? string.Empty;
                }
                while (!EXPERIMENTS.Contains(experiment));
            }

            switch (experiment)
            {
                case PLAYGROUND:
                    PointPlayground playground = new PointPlayground();
                    playground.Run();
                    break;
                case WIF_PARSER:
                    ParseAndPrintWIF(args);
                    break;
                case SIGNER:
                    break;
                case VALIDATOR:
                    break;
                case ENCRYPTOR:
                    break;
                case DECRYPTOR:
                    break;
            }
        }

        private static void ParseAndPrintWIF(string[] args)
        {
            string wif;
            if(args.Length > 0)
            {
                wif = args[0];
            }
            else
            {
                Console.WriteLine("Enter a Bitcoin private key in Wallet Import Format (WIF):");
                wif = Console.ReadLine().Trim();
            }

            bool publicKeyCompressed = wif.StartsWith("K") || wif.StartsWith("L") || wif.StartsWith("M");
            Console.WriteLine($"Public key is {(publicKeyCompressed ? "compressed" : "uncompressed")}");

            BigInteger privateKey = WIFUtils.WIFToPrivateKey(wif);
            Console.WriteLine("Private Key:");
            Console.WriteLine("    " + Convert.ToHexString(Utils.MakeUnsignedBigEndianArray(privateKey)));
            Console.WriteLine();

            ECEncryptor encryptor = new ECEncryptor(WeierstrasCurve.secp256k1);
            encryptor.SetPrivateKey(privateKey);
            BigPoint publicKey = encryptor.GetPublicKey();

            Console.WriteLine("Public Key as Point:");
            Console.WriteLine($"    X: {Convert.ToHexString(Utils.MakeUnsignedBigEndianArray(publicKey.X))}");
            Console.WriteLine($"    Y: {Convert.ToHexString(Utils.MakeUnsignedBigEndianArray(publicKey.Y))}");
            Console.WriteLine();

            byte[] publicKeyData;
            if (publicKeyCompressed)
            {
                publicKeyData = encryptor.ExportPublicKeyCompressed();
            }
            else
            {
                publicKeyData = encryptor.ExportPublicKey();
            }

            Console.WriteLine("Public Key as Byte Array:");
            Console.WriteLine("    " + Convert.ToHexString(publicKeyData));
            Console.WriteLine();

            string address = WIFUtils.CreateBitcoinAddressString(publicKeyData);

            Console.WriteLine("Wallet Address:");
            Console.WriteLine("    " + address);
        }
    }
}