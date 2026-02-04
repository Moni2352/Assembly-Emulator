using System.Globalization;
using System.Security.Principal;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Dictionaries of Hex commands and their purpose and command structure (# of bytes after for arguments)

        // Command Encoder Dictionary NAME, (HEX CODE, # OF ARGUMENT BYTES)
        Dictionary<string, byte> EncoderDictionary = new Dictionary<string, byte>()
        {
            {"NOP", 0X00},
            {"ADD", 0X01},
            {"SUB", 0X02},
            {"MUL", 0X03},
            {"DIV", 0X04},
            {"MOD", 0X05},
            {"INC", 0X11},
            {"DEC", 0X12},
            {"AND", 0X21},
            {"OR", 0X22},
            {"NAND", 0X23},
            {"NOR", 0X24},
            {"XOR", 0X25},
            {"LTH", 0X31},
            {"LEQ", 0X32},
            {"GTH", 0X33},
            {"GEQ", 0X34},
            {"EQL", 0X35},
            {"NEQ", 0X36},
            {"GTZ", 0X37},
            {"LTZ", 0X38},
            {"EQZ", 0X39},
            {"NQZ", 0X3A},
            {"JSR", 0X41},
            {"JMP", 0X42},
            {"RET", 0X43},
            {"PUSH", 0X51},
            {"POP", 0X52},
            {"CLR", 0X53},
            {"KEY", 0X61},
            {"WRT", 0X62},
            {"HALT", 0X63},
            {"ERR", 0X64},
            {"LDA", 0X71},
            {"LDB", 0X72},
            {"LDC", 0X73},
            {"LPC", 0X74},
            {"STA", 0X75},
            {"STB", 0X76},
            {"STC", 0X77},
            {"SPC", 0X78},
            {"LDI", 0X79}
        };

        // Gets file path from user
        string fileloader(bool runnotcompile)
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

        void filesaver(byte[] Bytes)
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
            List<byte> tokenbytes = new List<byte>();
            string filepath = fileloader(false);

            if (filepath == null)
            {
                throw new FileNotFoundException("No file was selected for compilation.");
            }

            Console.WriteLine($"Compiling file: {filepath}");

            string[] Lines = File.ReadAllLines(filepath);

            List<string> tokens = new List<string>();

            Console.WriteLine("Tokenizing Instructions...");

            foreach (string line in Lines)
            {
                string cleanline = line.Split("//")[0].Trim(); // removes comments and trims whitespace

                if (string.IsNullOrEmpty(cleanline)) continue; // skip empty lines

                string[] parts = cleanline.Split(new char[] { ' ', '\t'}, StringSplitOptions.RemoveEmptyEntries); // splits each line into tokens based on spaces and tabs

                tokens.AddRange(parts); // adds new tokens to the list
            }

            Console.WriteLine("Tokenization Complete.");

            Console.WriteLine(string.Join(", ", tokens)); // debug print of all tokens

            // Looks through and scans for "Define: Word"

            while (tokens.Contains("DEFINE:"))
            {

                int definePosition = -1;
                string defineName = "";

                for (int i = 0; i < tokens.Count; i++)
                {
                    if (tokens[i] == "DEFINE:")
                    {
                        definePosition = i;
                        defineName = tokens[i+1];
                        break;
                    }
                }

                if (definePosition >= 0) // Found a definition
                {

                    tokens.RemoveRange(definePosition, 2); // remove definition word

                    definePosition--;

                    for (int i = 0; i < tokens.Count; i++)
                    {
                        if (tokens[i] == defineName)
                        {
                            tokens[i] = definePosition.ToString();
                            tokens[i] = "0x" + ((byte)definePosition).ToString("X2");
                        }
                    }
                }
            }
        
            foreach (string token in tokens)
            {
                if (EncoderDictionary.ContainsKey(token)) {tokenbytes.Add(EncoderDictionary[token]);}

                if (token.Length == 4 && token.StartsWith("0x"))
                {
                    tokenbytes.Add(Byte.Parse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                } else if (token.Length == 2 && token != "PC")
                {
                    tokenbytes.Add(byte.Parse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }


            }

            //Console.WriteLine(string.Join(" ", tokenbytes.ToArray().Select(b => "0x" + b.ToString("X2"))));   // Debug display byte tokens
            filesaver(tokenbytes.ToArray());
        }

        void run() // runs a compiled .bin file
        {
            
        }

        Console.ReadKey();
    }

   
}