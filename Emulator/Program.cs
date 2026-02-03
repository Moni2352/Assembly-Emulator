
internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Dictionaries of Hex commands and their purpose and command structure (# of bytes after for arguments)
        
        OpenFileDialog dialog = new OpenFileDialog
        {
            Title = "Select a file to load",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Filter = "All Files (*.*)|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            string filepath = dialog.FileName;
            Console.WriteLine($"Selected File: {filepath}");
        }
        else
        {
            Console.WriteLine("No File Selected");
        }

        Console.ReadKey();
    }
}