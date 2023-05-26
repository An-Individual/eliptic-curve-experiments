using ECExperiments.ECC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ECExperiments
{
    public class PointPlayground
    {
        private const string EXIT_CMD = "exit";
        private const string HELP_CMD = "help";
        private const string CURVE_CMD = "curve";
        private const string LIST_CMD = "list";

        private const string HEX_CHARS = "0123456789abcdefABCDEFx";

        public SortedDictionary<string, BigPoint> Points { get; set; } = new SortedDictionary<string, BigPoint>();

        public WeierstrasCurve Curve { get; set; }

        public void Run()
        {
            Initialize();
            Execute();
        }

        #region Initialization

        private void Initialize()
        {
            Console.WriteLine("Starting Point Playground...");
            Console.WriteLine("Initializing Weierstras Curve...");
            Console.WriteLine("Points are on the cuve if (x^3 + a*x + b - y2) % p == 0");

            Console.WriteLine("Please specify a value for 'a':");
            if (!ReadNumber(out BigInteger a))
            {
                return;
            }

            Console.WriteLine("Please specify a value for 'b':");
            if (!ReadNumber(out BigInteger b))
            {
                return;
            }

            Console.WriteLine("Please specify a value for 'p'. Ideally this should be a prime number:");
            if (!ReadNumber(out BigInteger p))
            {
                return;
            }

            Curve = new WeierstrasCurve(a, b, p, BigInteger.Zero, BigInteger.Zero, BigPoint.Zero);

            Console.WriteLine("Initialization Complete");
            PrintHelp();
        }

        private bool ReadNumber(out BigInteger result)
        {
            while (true)
            {
                string input = Console.ReadLine();

                if (string.Equals(input, EXIT_CMD, StringComparison.OrdinalIgnoreCase))
                {
                    result = BigInteger.Zero;
                    return false;
                }
                if (input.StartsWith("0x"))
                {
                    input = input.Substring(2);
                    if (BigInteger.TryParse(input, System.Globalization.NumberStyles.HexNumber, null, out result))
                    {
                        return true;
                    }
                }
                else if (BigInteger.TryParse(input, out result))
                {
                    return true;
                }

                Console.WriteLine($"Input not recognized. Input must be an integer, a hex string starting with '0x', or the command '{EXIT_CMD}' to exit");
            }
        }

        #endregion Initialization

        private void Execute()
        {
            while (true)
            {
                Console.Write("Command: ");

                string command = Console.ReadLine().Trim();

                try
                {
                    if (string.Equals(command, EXIT_CMD, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    if (string.Equals(command, HELP_CMD, StringComparison.OrdinalIgnoreCase))
                    {
                        PrintHelp();
                        continue;
                    }

                    if (string.Equals(command, CURVE_CMD, StringComparison.OrdinalIgnoreCase))
                    {
                        PrintCurve();
                        continue;
                    }

                    if (string.Equals(command, LIST_CMD, StringComparison.OrdinalIgnoreCase))
                    {
                        PrintPoints();
                        continue;
                    }

                    if (command.Contains(':'))
                    {
                        HandleEnumerateCommand(command);
                    }
                    else if (command.Contains('='))
                    {
                        HandleAssignmentCommand(command);
                    }
                    else if (command.Contains('+'))
                    {
                        BigPoint point = HandleAddCommand(command);
                        Console.WriteLine(point);
                    }
                    else if (command.Contains('*'))
                    {
                        BigPoint point = HandleMultiplyCommand(command);
                        Console.WriteLine(point);
                    }
                    else
                    {
                        CommandEnumerator enumerator = new CommandEnumerator(command);

                        if (!enumerator.MoveNext())
                        {
                            throw new Exception("Unexpected end of command.");
                        }

                        BigPoint point = ReadPoint(enumerator);

                        if (!Curve.IsOnCurve(point))
                        {
                            throw new Exception($"Point {point} is not on the curve.");
                        }

                        if (enumerator.MoveNext())
                        {
                            throw new Exception("Unexpected command continuation");
                        }

                        Console.WriteLine(point);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command failed: {ex.Message}");
                }
            }
        }

        private void PrintHelp()
        {
            Console.WriteLine("Formatting:");
            Console.WriteLine("<point_name>: A string of alpha numeric characters. The first must be alphabetic.");
            Console.WriteLine("    <number>: An integer or a hex string that starts with '0x'.");
            Console.WriteLine("     <point>: A new point in the format (<number>,<number>) or a <point_name>.");
            Console.WriteLine("Commands:");
            Console.WriteLine("                             exit: Exit the program.");
            Console.WriteLine("                             help: Repeat these instructions.");
            Console.WriteLine("                            curve: Prints the curve information.");
            Console.WriteLine("                             list: Lists all stored points.");
            Console.WriteLine("                     <point name>: Prints the point stored in the specified name.");
            Console.WriteLine("           <point name> = <point>: Stores the specified point in the given name.");
            Console.WriteLine("                <point> + <point>: Peforms point addition and prints the result.");
            Console.WriteLine(" <point name> = <point> + <point>: Peforms point addition storing the result.");
            Console.WriteLine("               <point> * <number>: Peforms point multiplication and prints the result.");
            Console.WriteLine("<point name> = <point> * <number>: Peforms point multiplication storing the result.");
            Console.WriteLine("    <point> * [<number>:<number>]: Prints the results for multiplying the point by");
            Console.WriteLine("                                   each integer in the given range.");
        }

        private void PrintCurve()
        {
            Console.WriteLine($"(x^3 + {Curve.A}*x + {Curve.B} - y^2) % {Curve.Prime} == 0");
        }

        private void PrintPoints()
        {
            foreach(KeyValuePair<string, BigPoint> point in Points)
            {
                Console.WriteLine($"{point.Key}: {point.Value}");
            }
        }

        private BigPoint HandleAddCommand(string command)
        {
            CommandEnumerator enumerator = new CommandEnumerator(command);

            if (!enumerator.MoveNext())
            {
                throw new Exception("Unexpected end of command.");
            }

            BigPoint point1 = ReadPoint(enumerator);

            if (enumerator.Current != '+')
            {
                throw new Exception($"Unexpected character '{enumerator.Current}'.");
            }

            if (!enumerator.MoveNext())
            {
                throw new Exception("Unexpected end of command.");
            }

            BigPoint point2 = ReadPoint(enumerator);

            if (enumerator.MoveNext())
            {
                throw new Exception("Unexpected command continuation");
            }

            if (!Curve.IsOnCurve(point1))
            {
                throw new Exception($"Point {point1} is not on the curve.");
            }

            if (!Curve.IsOnCurve(point2))
            {
                throw new Exception($"Point {point2} is not on the curve.");
            }

            return Curve.PointAddition(point1, point2);
        }

        private BigPoint HandleMultiplyCommand(string command)
        {
            CommandEnumerator enumerator = new CommandEnumerator(command);

            if (!enumerator.MoveNext())
            {
                throw new Exception("Unexpected end of command.");
            }

            BigPoint point = ReadPoint(enumerator);

            if (enumerator.Current != '*')
            {
                throw new Exception($"Unexpected character '{enumerator.Current}'.");
            }

            if (!enumerator.MoveNext())
            {
                throw new Exception("Unexpected end of command.");
            }

            BigInteger multiplier = ReadNumber(enumerator);

            if (enumerator.MoveNext())
            {
                throw new Exception("Unexpected command continuation");
            }

            if (!Curve.IsOnCurve(point))
            {
                throw new Exception($"Point {point} is not on the curve.");
            }

            return Curve.MultiplyPoint(point, multiplier);
        }

        private void HandleAssignmentCommand(string command)
        {
            string[] commandParts = command.Split('=');
            if(commandParts.Length != 2 )
            {
                throw new Exception("Command not recognized.");
            }

            CommandEnumerator nameEnumerator = new CommandEnumerator(commandParts[0]);
            
            if (!nameEnumerator.MoveNext())
            {
                throw new Exception("Unexpected end of command.");
            }

            string targetPoint = ReadPointName(nameEnumerator);
            if(string.IsNullOrEmpty(targetPoint) || nameEnumerator.MoveNext())
            {
                throw new Exception("Invalid point name");
            }

            BigPoint value;
            if (commandParts[1].Contains('+'))
            {
                value = HandleAddCommand(commandParts[1]);
            }
            else if (commandParts[1].Contains("*"))
            {
                value = HandleMultiplyCommand(commandParts[1]);
            }
            else
            {
                CommandEnumerator valueEnumerator = new CommandEnumerator(commandParts[1]);

                if (!valueEnumerator.MoveNext())
                {
                    throw new Exception("Unexpected end of command.");
                }

                value = ReadPoint(valueEnumerator);

                if (!Curve.IsOnCurve(value))
                {
                    throw new Exception($"Point {value} is not on the curve.");
                }

                if (valueEnumerator.MoveNext())
                {
                    throw new Exception("Unexpected command continuation");
                }
            }

            Points[targetPoint] = value;

            Console.WriteLine($"{targetPoint} = {value}");
        }

        private void HandleEnumerateCommand(string command)
        {
            CommandEnumerator enumerator = new CommandEnumerator(command);

            if (!enumerator.MoveNext())
            {
                throw new Exception("Unexpected end of command.");
            }

            BigPoint point = ReadPoint(enumerator);

            if (enumerator.Current != '*')
            {
                throw new Exception($"Unexpected character '{enumerator.Current}'.");
            }

            if (!enumerator.MoveNext())
            {
                throw new Exception("Unexpected end of command.");
            }

            if (enumerator.Current != '[')
            {
                throw new Exception($"Unexpected character '{enumerator.Current}'.");
            }

            if (!enumerator.MoveNext())
            {
                throw new Exception("Unexpected end of command.");
            }

            BigInteger lowerBound = ReadNumber(enumerator);

            if (enumerator.Current != ':')
            {
                throw new Exception($"Unexpected character '{enumerator.Current}'.");
            }

            if (!enumerator.MoveNext())
            {
                throw new Exception("Unexpected end of command.");
            }

            BigInteger upperBound = ReadNumber(enumerator);
            
            if (enumerator.Current != ']')
            {
                throw new Exception($"Unexpected character '{enumerator.Current}'.");
            }

            if (enumerator.MoveNext())
            {
                throw new Exception("Unexpected command continuation");
            }

            if (!Curve.IsOnCurve(point))
            {
                throw new Exception($"Point {point} is not on the curve.");
            }

            if(lowerBound > upperBound)
            {
                throw new Exception($"{lowerBound} is greater than {upperBound}");
            }

            while (lowerBound <= upperBound)
            {
                BigPoint result = Curve.MultiplyPoint(point, lowerBound);
                Console.WriteLine($"{lowerBound}: {result}");
                lowerBound++;
            }
        }

        #region Command Reading Helpers

        private BigInteger ReadNumber(CommandEnumerator enumerator)
        {
            StringBuilder sb = new StringBuilder();

            while (HEX_CHARS.Contains(enumerator.Current))
            {
                sb.Append(enumerator.Current);
                if (!enumerator.MoveNext())
                {
                    break;
                }
            }

            string number = sb.ToString();

            if (number.StartsWith("0x"))
            {
                number = "0" + number.Substring(2);
                if (BigInteger.TryParse(number, System.Globalization.NumberStyles.HexNumber, null, out BigInteger hexResult))
                {
                    return hexResult;
                }
                else
                {
                    throw new Exception($"Unable to parse expected hex string {number}.");
                }
            }

            if (BigInteger.TryParse(number, out BigInteger result))
            {
                return result;
            }
            else
            {
                throw new Exception($"Unable to parse expected integer string {number}.");
            }
        }

        private string ReadPointName(CommandEnumerator enumerator)
        {
            if (!char.IsLetter(enumerator.Current))
            {
                throw new Exception($"Unexpcted character '{enumerator.Current}'.");
            }

            StringBuilder sb = new StringBuilder();

            while (char.IsLetterOrDigit(enumerator.Current))
            {
                sb.Append(enumerator.Current);
                if (!enumerator.MoveNext())
                {
                    break;
                }
            }

            return sb.ToString();
        }

        private BigPoint ReadPoint(CommandEnumerator enumerator)
        {
            if(enumerator.Current == '(')
            {
                if (!enumerator.MoveNext())
                {
                    throw new Exception("Unexpected end of command.");
                }

                BigInteger x = ReadNumber(enumerator);

                if(enumerator.Current != ',')
                {
                    throw new Exception($"Unexpcted character '{enumerator.Current}'.");
                }

                if (!enumerator.MoveNext())
                {
                    throw new Exception("Unexpected end of command.");
                }

                BigInteger y = ReadNumber(enumerator);


                if (enumerator.Current != ')')
                {
                    throw new Exception($"Unexpcted character '{enumerator.Current}'.");
                }

                enumerator.MoveNext();

                return new BigPoint(x, y);
            }

            string pointName = ReadPointName(enumerator);
            if (!Points.TryGetValue(pointName, out BigPoint point))
            {
                throw new Exception($"Point name '{pointName}' is not defined.");
            }

            return point;
        }

        #endregion Command Reading Helpers
    }

    public class CommandEnumerator : IEnumerator<char>
    {
        private string _command;
        private int _idx;

        public CommandEnumerator(string command)
        {
            _command = command;
            _idx = -1;
        }

        public char Current => _idx < 0 || _idx >= _command.Length ? default(char) : _command[_idx];

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            do
            {
                _idx++;
                if (_idx >= _command.Length)
                {
                    return false;
                }
            }
            while(char.IsWhiteSpace(Current));

            return true;
        }

        public void Reset()
        {
            _idx = -1;
        }

        public void Dispose()
        {
        }
    }
}
