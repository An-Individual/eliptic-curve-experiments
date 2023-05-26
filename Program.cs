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
    }
}