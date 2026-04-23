using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
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

            List<string> dataTokens;
            List<string> codeTokens;

            (dataTokens, codeTokens) = Tokenization.SectionSplitter(tokens);

            codeTokens = Tokenization.DefineAddresser(codeTokens); // Looks through and scans for "Define: Word"

            codeTokens = Tokenization.AddEndOfFileCharacter(codeTokens); // Does what is says

            List<DataContainer> datasegments = Tokenization.DataSeperator(dataTokens);

            List<string> partialconvert = Tokenization.Step1Converter(codeTokens, datasegments); // Convert Data to bytes except for variable names (Convert to string version of bytes ie "0x03" and not 3)

            List<DataContainer> prepedData = Tokenization.DataPrepper(datasegments); // Prep Data Segment to be added (Convert to proper form Int to hex value, string to string of hex values with end character 0x03)

            List<string> combinedhex = Tokenization.CombinedHex(prepedData, partialconvert); // Attach data segments and replace variable names with pointer to data

            byte[] convertedbytes = Tokenization.ConvertToByteArray(combinedhex); // Fully convert to Binary (convert string hex to actual hex)

            stopwatch.Stop();
            
            Filecontrol.filesaver(convertedbytes);
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
                            case 0x11:
                                regA++;
                                programCounter = (byte)(programCounter - 1);
                            break;
                            case 0x12:
                                regA--;
                                programCounter = (byte)(programCounter - 1);
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
                        if (currentData == 0x03) {break;}
                        text += (char)currentData;
                        currentAddress++;
                    }


                    Console.WriteLine(text);
                    programCounter = (byte)(programCounter + 2);
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

    public struct DataContainer
    {
        public string Name {get; set;}
        public string Type {get; set;}
        public string[] Data {get; set;}
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
    
        public static (List<string>, List<string>) SectionSplitter (List<string> input)
        {
            if (!(input.Contains("_START") || input.Contains("_DATA"))) {Console.WriteLine("ERROR - SECTIONS NOT FOUND");}
            int dataIndex = input.IndexOf("_START");
            List<string> data = input.Take(dataIndex).ToList();
            data.Remove("_DATA");
            List<string> code = input.Skip(dataIndex).ToList();
            code.Remove("_START");

            return (data, code);
        }

        public static List<string> DefineAddresser (List<string> inputTokens)
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
            
            Console.WriteLine(string.Join(" ", inputTokens)); // DEBUG

            return inputTokens;
        }

        public static List<string> AddEndOfFileCharacter (List<string> inputTokens)
        {
            inputTokens.Add("END");
            return inputTokens;
        }

        public static List<string> DataMover (List<string> data, List<string> code)
        {
            //(TYPE) (NAME) (...DATA...)

            return null;
        }

        public static List<DataContainer> DataSeperator (List<string> Tokens)
        {
            List<DataContainer> output = new();

            for (int i = 0; i < Tokens.Count; i++)
            {
                if (Dictionaries.Datatypes.Contains(Tokens[i]))
                {
                    DataContainer container = new DataContainer()
                    {
                        Name = Tokens[i+1],
                        Type = Tokens[i],
                        Data = [Tokens[i+2]]
                    };

                    output.Add(container);
                }
            }
            return output;
        }
    
        public static List<string> Step1Converter (List<string> CodeTokens, List<DataContainer> DataStrings)
        {
            List<string> dataNames = new();
            List<string> output = new();
            
            foreach (DataContainer array in DataStrings)
            {
                dataNames.Add(array.Name);
            }

            for (int i = 0; i < CodeTokens.Count; i++)
            {
                string[] types = ["LDA", "LDB", "LDC", "STA", "STB", "STC", "LDI", "SPC", "LPC", "SSC"];
                if (types.Contains(CodeTokens[i]))
                {
                    if (CodeTokens[i+1].StartsWith("0x") || dataNames.Contains(CodeTokens[i+1])) {continue;}
                    CodeTokens[i+1] = $"0x{int.Parse(CodeTokens[i+1]):X2}";
                }
            }


            for (int i = 0; i < CodeTokens.Count; i++)
            {
                if (dataNames.Contains(CodeTokens[i]))
                {
                    output.Add(CodeTokens[i]); // Adds variable names
                }
                else if (CodeTokens[i].StartsWith("0x"))
                {
                    output.Add(CodeTokens[i]); // Adds converted bytes
                } 
                else if (Dictionaries.EncoderDictionary.ContainsKey(CodeTokens[i]))
                {
                    output.Add($"0x{Dictionaries.EncoderDictionary[CodeTokens[i]]:X2}"); // Converts commands and adds them
                }
                else
                {
                    Console.WriteLine($"Error | {CodeTokens[i]} | Error in Step1Converter");
                }
            }

            Console.WriteLine(string.Join(" ", output));
            return output;
        }

        public static List<DataContainer> DataPrepper (List<DataContainer> DataStrings) // Processes Data strings into strings starting with name, type, and then the data in a hex format (0x01)
        {
            List<DataContainer> Output = new();

            foreach (DataContainer array in DataStrings)
            {
                string Data = array.Data[0];


                DataContainer data = new DataContainer()
                {
                    Name = array.Name,
                    Type = array.Type
                };

                List<string> datalist = new();

                if (array.Type == "INT")
                {
                    datalist.Add($"0x{int.Parse(Data):X2}");
                    datalist.Add("0x03");
                } else if (array.Type == "STRING")
                {
                    foreach (char character in Data)
                    {
                        datalist.Add($"0x{Convert.ToInt32(character):X2}");
                    }
                    datalist.Add("0x03");
                }
                data.Data = datalist.ToArray();
                Output.Add(data);
            }
            return Output;
        }
    
        public static List<string> CombinedHex (List<DataContainer> PreparedData, List<string> PreparedCode)
        {
            List<string> OutputData = PreparedCode;

            PreparedData.RemoveAll(container => !PreparedCode.Contains(container.Name)); // Removes data if the code does not include a reference

            foreach (DataContainer container in PreparedData)
            {
                string indexstring = $"0x{OutputData.Count():X2}"; // Takes the length of the current output which is the index of the data and converts it to a hex string

                OutputData.AddRange(container.Data); // Adds data to output

                for (int i = 0; i < PreparedCode.Count(); i++) // Replaces all instances of the pointer name to the data index
                {
                    if (OutputData[i] == container.Name)
                    {
                        OutputData[i] = indexstring;
                    }
                }
            }
            return OutputData;
        }

        public static byte[] ConvertToByteArray (List<string> InputData)
        {
            List<byte> output = new();
            foreach (string token in InputData)
            {
                byte bytedata = Convert.ToByte(token.Substring(2), 16);
                output.Add(bytedata);
            }
            byte[] trueoutput = output.ToArray();
            return trueoutput;
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