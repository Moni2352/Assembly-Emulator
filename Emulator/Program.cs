using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

internal partial class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Stopwatch stopwatch = new();
        // Dictionaries of Hex commands and their purpose and command structure (# of bytes after for arguments)

        // Command Encoder Dictionary NAME, (HEX CODE, # OF ARGUMENT BYTES)
        

        // Gets file path from user
        
        // asks user if they want to compile or run a file
        Console.WriteLine("Would you like to (C)ompile or (R)un a file?");
        char choice = Char.ToUpper(Console.ReadKey(intercept:true).KeyChar);

        if (choice != 'C' && choice != 'R')
        {
            throw new ArgumentException("Invalid choice. Please enter 'C' to compile or 'R' to run.");   
        }

        if (choice == 'C')
        {
            compile();
        }
        else if (choice == 'R')
        {
            run();
        }

        void compile() // compiles a .asm file into a .bin file
        {
            
            string filepath = Filecontrol.fileloader(false); // Gets filepath for .asm from user

            if (filepath == null) // Throws error as there was no file selected
            {
                throw new FileNotFoundException("No file was selected for compilation.");
            }

            stopwatch.Start();

            Console.WriteLine($"Compiling file: {filepath}");

            string[] Lines = File.ReadAllLines(filepath); // Grabs lines from given file and returns them as an array of lines in string format

            Console.WriteLine("Tokenizing Instructions...");

            List<string> tokens = Tokenization.Splitter(Lines); // Splits "Lines" into "Words" called tokens

            Console.WriteLine("Tokenization Complete.");

            Console.WriteLine(string.Join(", ", tokens)); // debug print of all tokens

            tokens = Tokenization.DefineAddresser(tokens); // Looks through and scans for "Define: Word"

            tokens = Tokenization.AddEndOfFileCharacter(tokens); // Does what is says

            tokens = Tokenization.TextMover(tokens); // Moves text to be written after end of file and adds a link there

            List<byte> tokenbytes = Tokenization.ConvertBytes(tokens); // Converts Tokens to their Byte versions

            Console.WriteLine(string.Join(" ", tokenbytes.ToArray().Select(b => "0x" + b.ToString("X2"))));   // Debug display byte tokens

            stopwatch.Stop();
            
            Filecontrol.filesaver(tokenbytes.ToArray());
        }

        void run() // runs a compiled .bin file
        {
            string filename = Filecontrol.fileloader(true);

            byte[] programData = File.ReadAllBytes(filename); // Loads Bytes from .bin to array

            Console.WriteLine("File Successfully loaded");

            stopwatch.Start();

            byte[] RAM = new byte[255]; // Ram for CPU

            byte currentByte = 0x00;

            byte regA = 0;
            byte regB = 0;
            byte regC = 0;
            byte programCounter = 0;

            Stack<byte> MainStack = new Stack<byte>(255);

            //const int targetHz = 5;
            //double targetPeriodMs = 1000.0 / targetHz;

            //var stopwatch = Stopwatch.StartNew();
            //long nextTick = stopwatch.ElapsedTicks;
            //long ticksPerMs = Stopwatch.Frequency / 1000;

            while (true)
            {
                //nextTick += (long)(targetPeriodMs * ticksPerMs); // calculates next tick time

                currentByte = programData[programCounter];

                if (Dictionaries.ArithmeticGateInstructions.Contains(currentByte))
                {
                    regC = CPU.ArithmeticLogic(currentByte, regA, regB);
                    programCounter++;
                    
                } else if (Dictionaries.RegisterControlInstructions.Contains(currentByte))
                {
                    if (currentByte == 0x74)
                    {
                        programCounter = RAM[programData[programCounter + 1]];
                    } else
                    {
                        switch (currentByte)
                        {
                            case 0x71:
                                regA = RAM[programData[programCounter + 1]];
                            break;
                            case 0x72:
                                regB = RAM[programData[programCounter + 1]];
                            break;
                            case 0x73:
                                regC = RAM[programData[programCounter + 1]];
                            break;
                            case 0x75:
                                RAM[programData[programCounter + 1]] = regA;
                            break;
                            case 0x76:
                                RAM[programData[programCounter + 1]] = regB;
                            break;
                            case 0x77:
                                RAM[programData[programCounter + 1]] = regC;
                            break;
                            case 0x78:
                                RAM[programData[programCounter + 1]] = programCounter;
                            break;
                            case 0x79:
                                regA = programData[programCounter + 1];
                            break;
                            case 0x7A:
                                RAM[programData[programCounter + 1]] = (byte)MainStack.Count;
                            break;
                        }    
                        programCounter = (byte)(programCounter + 2);
                    }
                    
                } else if (Dictionaries.LogicalJumpInstructions.Contains(currentByte))
                {
                    byte newindex = CPU.LogicalJumpControl(programCounter, programData, regA, regB);
                    if (newindex == 0xFF)
                    {
                        programCounter = (byte)(programCounter + 2);
                    } else
                    {
                        programCounter = newindex;
                    }
                } else if (currentByte == 0x62) // Write to display
                {
                    byte targetAddress = programData[programCounter + 1];
                    byte currentAddress = targetAddress;
                    string text = "";

                    while (true)
                    {
                        byte currentData = programData[currentAddress];
                        if (currentData == 0x00) {break;}
                        text = $"{text}{Convert.ToString(currentData)}";
                        currentAddress++;
                    }


                    Console.WriteLine(text);
                    programCounter++;
                } else if (Dictionaries.JumpInstructions.Contains(currentByte))
                {
                    int targetaddress;
                    switch (currentByte)
                    {  
                        case 0x41:
                        MainStack.Push((byte)(programCounter + 2));
                        targetaddress = programData[programCounter + 1];
                        programCounter = (byte)targetaddress;
                        break;
                        case 0x42:
                        targetaddress = programData[programCounter + 1];
                        programCounter = (byte)targetaddress;
                        break;
                        case 0x43:
                        programCounter = MainStack.Pop();
                        break;
                    }
                }

                if (currentByte == 0x81)
                {
                    Console.WriteLine("End of Program");
                    stopwatch.Stop();
                    break;
                }

            }



        }
        Console.WriteLine($"{stopwatch.Elapsed.Microseconds.ToString()} Microseconds");
        Console.ReadKey();
    }

    public class Filecontrol
    {
        public static string fileloader(bool runnotcompile)
        {
                OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select a file to load",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Filter = runnotcompile
                ? "Binary Files (*.bin)|*.bin"
                : "Assembly Files (*.asm)|*.asm",
                Multiselect = false
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string filepath = dialog.FileName;
                Console.WriteLine($"Selected File: {filepath}");
                return filepath;
            }
            else
            {
                Console.WriteLine("No File Selected");
                return null;
            }   
        }

        public static void filesaver(byte[] Bytes)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save Compiled Data";
            saveFileDialog.Filter = "Binary Files (*.bin)|*.bin";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filepath = saveFileDialog.FileName;
                Console.WriteLine($"Saving Data To {filepath}");

                System.IO.File.WriteAllBytes(filepath, Bytes);

                Console.WriteLine("Save Succesful");

                //byte[] test = System.IO.File.ReadAllBytes(filepath);
                //Console.WriteLine(string.Join(" ", test.ToArray().Select(b => "0x" + b.ToString("X2"))));

            } else
            {
                Console.WriteLine("Save Canceled");
            }
        }

    }

    public class Tokenization
    {
        public static List<string> Splitter (string[] fileLines)
        {
            List<string> tokenHolder = new List<string>();
            Regex regex = new Regex(@"""([^""]*)""|(\S+)");
             foreach (string line in fileLines)
            {
                string cleanline = line.Split("//")[0].Trim(); // removes comments and trims whitespace

                if (string.IsNullOrEmpty(cleanline)) continue; // skip empty lines

                foreach (Match match in regex.Matches(cleanline))
                {
                    if (match.Groups[1].Success)
                    {
                        tokenHolder.Add(match.Groups[1].Value);
                    } else
                    {
                        tokenHolder.Add(match.Groups[2].Value);
                    }
                }
            }
            return tokenHolder;
        }
    
        public static List<string> DefineAddresser(List<string> inputTokens)
        {
            while (inputTokens.Contains("DEFINE:"))
            {

                int definePosition = -1;
                string defineName = "";

                for (int i = 0; i < inputTokens.Count; i++)
                {
                    if (inputTokens[i] == "DEFINE:")
                    {
                        definePosition = i;
                        defineName = inputTokens[i+1];
                        break;
                    }
                }

                if (definePosition >= 0) // Found a definition
                {

                    inputTokens.RemoveRange(definePosition, 2); // remove definition word

                    for (int i = 0; i < inputTokens.Count; i++)
                    {
                        if (inputTokens[i] == defineName)
                        {
                            inputTokens[i] = definePosition.ToString();
                            inputTokens[i] = "0x" + ((byte)definePosition).ToString("X2");
                        }
                    }
                }
            }  

            return inputTokens;
        }

        public static List<string> AddEndOfFileCharacter(List<string> inputTokens)
        {
            inputTokens.Add("END");
            return inputTokens;
        }

        public static List<string> TextMover(List<string> inputTokens)
        {
            List<string> returnList = inputTokens;

            for (int i = 0; i < inputTokens.Count; i++)
            {
                if (inputTokens[i] == "END") {return returnList;}
                
                if (inputTokens[i] == "WRT")
                {
                    string text = inputTokens[i+1];
                    byte[] bytes = Encoding.UTF8.GetBytes(text);
                    Array.Resize(ref bytes, bytes.Length + 1);
                    bytes[bytes.Length - 1] = 0x00;

                    int index = returnList.Count;

                    returnList[i+1] =  ((byte)index).ToString();

                    List<string> bytestring = new List<string>();
                    foreach (byte bytebit in bytes)
                    {
                       bytestring.Add(bytebit.ToString());   
                    }

                    returnList.AddRange(bytestring);
                }
            }
            return returnList;
        }

        public static List<byte> ConvertBytes(List<string> inputTokens)
        {
            List<byte> byteTokens = new List<byte>();

            foreach (string token in inputTokens)
            {

                if (Dictionaries.EncoderDictionary.ContainsKey(token)) {byteTokens.Add(Dictionaries.EncoderDictionary[token]);}

                if (token.Length == 4 && token.StartsWith("0x"))
                {
                    byteTokens.Add(Byte.Parse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                } else if (token.Length == 2 && token != "PC")
                {
                    byteTokens.Add(byte.Parse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }
                if (token == "END") {break;}
            }
            return byteTokens;
        }
    }  

    public class CPU
    {
        public static byte ArithmeticLogic(byte Command, byte A, byte B)
        {
            switch (Command)
            {
                case 0x1:
                    return (byte)(A + B);
                case 0x2:
                    return (byte)(A - B);
                case 0x3:
                    return (byte)(A * B);
                case 0x4:
                    return (byte)(A / B);
                case 0x5:
                    return (byte)(A % B);
                case 0x21:
                    return (byte)(A & B);
                case 0x22:
                    return (byte)(A | B);
                case 0x23:
                    return (byte)~(A & B);
                case 0x24:
                    return (byte)~(A | B);
                case 0x25:
                    return (byte)(A ^ B);
                case 0x26:
                    return (byte)(A << B);
                case 0x27:
                    return (byte)(A >> B);
                case 0x28:
                    return (byte)BitOperations.RotateLeft(A, B);
                case 0x29:
                    return (byte)BitOperations.RotateRight(A, B);
                case 0x2A:
                    return (byte)~A;
            }
            
            return 0;
        }
    
        public static byte LogicalJumpControl(byte index, byte[] Data, byte A, byte B)
        {
            byte Instruction = Data[index];
            byte JumpPoint = Data[index+1];

            switch (Instruction)
            {
                case 0x31:
                    if (A < B) {return JumpPoint;}
                break;
                case 0x32:
                    if (A <= B) {return JumpPoint;}
                break;
                case 0x33:
                    if (A > B) {return JumpPoint;}
                break;
                case 0x34:
                    if (A >= B) {return JumpPoint;}
                break;
                case 0x35:
                    if (A == B) {return JumpPoint;}
                break;
                case 0x36:
                    if (A != B) {return JumpPoint;}
                break;
                case 0x37:
                    if (A > 0) {return JumpPoint;}
                break;
                case 0x38:
                    if (A < 0) {return JumpPoint;}
                break;
                case 0x39:
                    if (A == 0) {return JumpPoint;}
                break;
                case 0x3A:
                    if (A != 0) {return JumpPoint;}
                break;
            }

            return (byte)(index+2);
        }
    }
}