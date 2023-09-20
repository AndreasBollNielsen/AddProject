using System.Collections.Generic;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;

Dictionary<string, int> SymbolTable = new Dictionary<string, int>();
Dictionary<string, string> CompTable = new Dictionary<string, string>();
Dictionary<string, string> DestTable = new Dictionary<string, string>();
Dictionary<string, string> JumpTable = new Dictionary<string, string>();
Dictionary<string, int> VariableTable = new Dictionary<string, int>();
Dictionary<string, int> LabelTable = new Dictionary<string, int>();
List<string> binaries = new List<string>();

string[] asmFile;
int lineCounter = 0;
int variableCounter = 15;
int secondLineCounter = 0;

//read file
asmFile = GetfileText("loop", "asm");
Console.ForegroundColor = ConsoleColor.White;

//initialize table
InitializeTable();


//loop through labels
foreach (string line in asmFile)
{
    //add labels if exists
    if (line.Contains("("))
    {
        string label = line.Split('(', ')')[1];
        int incrementor = 0;
        
        //adjust for bad line counting 
        if(LabelTable.Count > 0)
        {
            incrementor = -1;
        }

        
        AddToLabelTable(label, lineCounter + incrementor);

        

    }
    //increment linecounter if line is not blank or contains comments
    if (!line.Contains("//") && !string.IsNullOrEmpty(line) && !line.Contains("C"))
    {
        lineCounter++;

    }
}



//loop through variables
foreach (string line in asmFile)
{
    //add variables
    if (line.Contains("@"))
    {
        string variable = line.Split('@')[1];
        variable = variable.Split(" ")[0];
        string address = $"{variableCounter + 1}";
        int num;

        //check if variable is a number
        if (int.TryParse(variable, out num))
        {
            var addr = SymbolTable[variable];
            address = addr.ToString();
            variable = address;
            AddToVariableTable(address, num);
        }
        //if variable is not a number
        else if (!variable.StartsWith("R"))
        {
            //skip if variable still exists
            if (VariableTable.ContainsKey(variable))
            {
                continue;
            }
          
            //use variable counter if not a pointer
            var value = variableCounter + 1;

            //check if label exists
            if (LabelTable.ContainsKey(variable))
            {
                value = LabelTable[variable];
               
            }

            AddToVariableTable(variable, value);
            variableCounter++;
        }
        //if variable is predetermined
        else
        {
            //skip if variable still exists
            if (VariableTable.ContainsKey(variable))
            {
                continue;
            }
            int value = SymbolTable[variable];
            AddToVariableTable(variable, value);
            Console.WriteLine($"{variable}:{VariableTable[variable]}");
        }


    }

}

// convert instruction to binary
foreach (string line in asmFile)
{
    //skip if line is blank or a comment
    if (line.StartsWith("//") || string.IsNullOrEmpty(line))
    {
        continue;
    }

    


    //check variable
    if (line.Contains("@"))
    {
        string variable = line.Split('@')[1];
        variable = variable.Split(" ")[0];
        if (VariableTable.ContainsKey(variable))
        {
            var value = VariableTable[variable];
            AddInstruction(value);
        }
        secondLineCounter++;
    }

    //check C instruction
    if (line.Contains(";") || line.Contains("="))
    {
        AddCInstruction(line);
        secondLineCounter++;
    }
}



//write out data
if (binaries.Count > 0)
{
    StringBuilder sb = new StringBuilder();
    foreach (string line in binaries)
    {
        sb.Append($"{line}\n");

    }
    SaveToDisk(sb.ToString());

    //compare compiled with source
    var source = GetfileText("compare", "txt");
    var compiled = GetfileText("compiled", "txt");

    Compare(compiled, source);
}





//initialize symbol table
void InitializeTable()
{
    //Initialize A instruction tables---------
    //add R0-15 symbols
    for (int i = 0; i < 16; i++)
    {
        string symbol = $"R{i}";
        SymbolTable.Add(symbol, i);

    }

    //add screen
    SymbolTable.Add("SCREEN", 16384);

    //add keyboard
    SymbolTable.Add("KBD", 24576);

    //add SP
    SymbolTable.Add("SP", 0);
    //add LCL
    SymbolTable.Add("LCL", 1);
    //add ARG
    SymbolTable.Add("ARG", 2);
    //add THIS
    SymbolTable.Add("THIS", 3);
    //add THAT
    SymbolTable.Add("THAT", 4);

    //initialize C instruction tables-----------
    //setup Compute table
    CompTable = new Dictionary<string, string>()
    { {"0", "0101010" },
    {"1", "0111111" },
    {"-1", "0111010" },
    {"D", "0001100" },
    {"A", "0110000" },
    {"!D", "0001101" },
    {"!A", "0110001" },
    {"-D", "0001111" },
    {"-A", "0110011" },
    {"D+1", "0011111" },
    {"A+1", "0110111" },
    {"D-1", "0001110" },
    {"A-1", "0110010" },
    {"D+A", "0000010" },
    {"D-A", "0010011" },
    {"A-D", "0000111" },
    {"D&A", "0000000" },
    {"D|A", "0010101" },
    {"M", "1110000" },
    {"!M", "1110001" },
    {"-M", "1110011" },
    {"M+1", "1110111" },
    {"M-1", "1110010" },
    {"D+M", "1000010" },
    {"D-M", "1010011" },
    {"M-D", "1000111" },
    {"D&M", "1000000" },
    {"D|M", "1010101" }
    };

    //setup dest table
    DestTable = new Dictionary<string, string>() {
        {"null","000" },
        {"M","001" },
        {"D","010" },
        {"MD","011" },
        {"A","100" },
        {"AM","101" },
        {"AD","110" },
        {"AMD","111" },

    };

    //setup jump table
    JumpTable = new Dictionary<string, string>() {
        {"null","000" },
        {"JGT","001" },
        {"JEQ","010" },
        {"JGE","011" },
        {"JLT","100" },
        {"JNE","101" },
        {"JLE","110" },
        {"JMP","111" },
    };
}

//reading file from data folder
string[] GetfileText(string fileName, string extension)
{
    string filepath = "Data/";
    return File.ReadAllLines($"{filepath}{fileName}.{extension}");
}

void SaveToDisk(string text)
{
   
    string filepath = "Data/compiled.txt";
    try
    {
        using (StreamWriter sw = new StreamWriter(filepath))
        {
            sw.Write(text);
        }

    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}

//adding symbol to table
void AddToVariableTable(string address, int value)
{
    VariableTable.Add(address, value);

}
//add to label table
void AddToLabelTable(string address, int value)
{
    LabelTable.Add(address, value);

}

//method to add A instructions
void AddInstruction(int value)
{
    int lineNum = secondLineCounter;
    //int test = 228;
    var binary = Convert.ToString(value, 2).PadLeft(16, '0');
    binaries.Add(binary);
    Console.WriteLine($"{lineNum}: {binary}");
}

//method to add C instructions
void AddCInstruction(string value)
{
    //remove blank spaces if present
    string trimmedVal = value.Replace(" ", "");
    value = trimmedVal;
    int lineNum = secondLineCounter;
    
    //use C instruction without jump statement
    if (value.Contains("="))
    {

        var subparts = value.Split('=');
        string dest = subparts[0];
        string jmp = "null";
        string compute = subparts[1];

        //convert instruction to binary code
        if (CompTable.ContainsKey(compute))
        {
            var comp = CompTable[compute];
            var destination = DestTable[dest];
            var jump = JumpTable[jmp];
            string binary = $"111{comp}{destination}{jump}";

            Console.WriteLine($"{lineNum}: 111{comp}{destination}{jump}");

            binaries.Add(binary);
        }
    }
    //use C instruction with jump statement
    else
    {
        var subparts = value.Split(';');
        string dest = "null";
        string compute = subparts[0];
        string jump = subparts[1];

        //convert instruction to binary code
        if (JumpTable.ContainsKey(jump) && CompTable.ContainsKey(compute))
        {
            var jmp = JumpTable[jump];
            var comp = CompTable[compute];
            var dst = DestTable[dest];
            string binary = $"111{comp}{dst}{jmp}";
            Console.WriteLine($"{lineNum}: 111{comp}{dst}{jmp}");
            binaries.Add(binary);
        }
    }

}

//Method for comparing compiled binary to source binary code
void Compare(string[] compiler, string[] source)
{
    List<bool> result = new List<bool>();
    for (int i = 0; i < source.Length; i++)
    {
        try
        {
            string compiled = "null            ";
            if (i < compiler.Length)
            {
                compiled = compiler[i];

            }
            string src = source[i];
            bool currentCompare = (compiled == src);
            result.Add(currentCompare);

            //set color of text
            if (currentCompare)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            //write out the comparison
            Console.WriteLine($"{i}: [{compiled}] [{src}]");
        }
        catch (Exception)
        {

            throw;
        }


    }




}