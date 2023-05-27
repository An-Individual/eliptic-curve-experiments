using ECExperiments.Bitcoin;
using ECExperiments.ECC;
using System.Numerics;
using System.Security.Cryptography;

namespace ECExperiments
{
    internal class Program
    {
        private const string PLAYGROUND = "playground";
        private const string WIF_PARSER = "wifparser";
        private const string MAKE_KEY = "makekey";
        private const string SIGNER = "signer";
        private const string VALIDATOR = "validator";
        private const string ENCRYPTOR = "encryptor";
        private const string DECRYPTOR = "decryptor";

        private static readonly string[] EXPERIMENTS = new string[]
        {
            PLAYGROUND,
            WIF_PARSER,
            MAKE_KEY,
            SIGNER,
            VALIDATOR,
            ENCRYPTOR,
            DECRYPTOR,
        };

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string experiment = args[0]?.ToLowerInvariant()?.Trim();

                if (!EXPERIMENTS.Contains(experiment))
                {
                    Console.WriteLine("Experiment not recognized.");
                }

                args = args.Skip(1).ToArray();

                RunExperiment(experiment, args);

                return;
            }

            while (true)
            {
                try
                {
                    if (!ReadExperiment(out string experiment))
                    {
                        return;
                    }

                    RunExperiment(experiment, new string[] { });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private static bool ReadExperiment(out string experiment)
        {
            Console.WriteLine("Select an experiment or enter 'exit' to leave?");
            Console.WriteLine("    " + PLAYGROUND);
            Console.WriteLine("    " + WIF_PARSER);
            Console.WriteLine("    " + MAKE_KEY);
            Console.WriteLine("    " + SIGNER);
            Console.WriteLine("    " + VALIDATOR);
            Console.WriteLine("    " + ENCRYPTOR);
            Console.WriteLine("    " + DECRYPTOR);

            experiment = null;

            do
            {
                if (experiment != null)
                {
                    Console.WriteLine("Value not recognized. Please enter a valid experiment name.");
                }

                Console.Write("Selection: ");
                experiment = Console.ReadLine()?.ToLowerInvariant()?.Trim() ?? string.Empty;

                if (experiment == "exit")
                {
                    return false;
                }
            }
            while (!EXPERIMENTS.Contains(experiment));

            return true;
        }

        private static void RunExperiment(string experiment, string[] args)
        {
            switch (experiment)
            {
                case PLAYGROUND:
                    PointPlayground playground = new PointPlayground();
                    playground.Run();
                    break;
                case WIF_PARSER:
                    ParseAndPrintWIF(args);
                    break;
                case MAKE_KEY:
                    MakeKey();
                    break;
                case SIGNER:
                    MakeSignature(args);
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

        private static void MakeKey()
        {
            ECEncryptor encryptor = new ECEncryptor(WeierstrasCurve.secp256k1);

            BigInteger privateKey = encryptor.GenerateRandomPrivateKey();
            encryptor.SetPrivateKey(privateKey);

            byte[] privateKeyData = encryptor.ExportPrivateKey();

            Console.WriteLine("Private Key:");
            Console.WriteLine("    " + Convert.ToHexString(privateKeyData));

            byte[] publicKey = encryptor.ExportPublicKeyCompressed();

            Console.WriteLine("Public Key:");
            Console.WriteLine("    " + Convert.ToHexString(publicKey));
        }

        private static void MakeSignature(string[] args)
        {
            ECEncryptor encryptor = new ECEncryptor(WeierstrasCurve.secp256k1);

            byte[] privateKey;
            if(args.Length > 0)
            {
                privateKey = Convert.FromHexString(args[0]);
            }
            else
            {
                Console.WriteLine("Enter the private key to use as a hex string:");
                string hexString = Console.ReadLine().Trim();
                privateKey = Convert.FromHexString(hexString);
            }

            encryptor.ImportPrivateKey(privateKey);

            string filePath;
            if(args.Length > 1)
            {
                filePath = args[1];
            }
            else
            {
                Console.WriteLine("Enter the path to the file to generate a signature for:");
                filePath = Console.ReadLine().Trim();
            }

            if (!File.Exists(filePath))
            {
                Console.WriteLine("File does not exist.");
                return;
            }

            Console.WriteLine("Reading file...");
            byte[] fileData = File.ReadAllBytes(filePath);

            Console.WriteLine("Hashing file with SHA256...");
            byte[] hash = SHA256.HashData(fileData);

            Console.WriteLine("Generating signature...");
            byte[] signature = encryptor.SignHash(hash);

            Console.WriteLine("Signature:");
            Console.WriteLine("    " + Convert.ToHexString(signature));
        }
    }
}