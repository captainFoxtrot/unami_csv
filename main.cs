using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

public static class UnamiCSVCompiler
{
    /// <author>
    /// Captain Foxtrot
    /// </author>

    /// <summary>
    /// Given a list of routes line-by-line, outputs the routes in CSV format.
    /// </summary>

    /// </remarks>

    /// - Each line of the file at inputFileName should match the following format (regex):
    ///   ^([A-Z]{3})(\d{3,4})\/(\d{3,4}) ([A-Z]{4}) .*to ([A-Z]{4})(.*)$
    ///   A line in InputFile as follows:
    ///   UAN069/070 ABCD Not a real airport to WXYZ Also not a real airport with Air Seattle codeshare
    ///   Will be compiled to:
    ///   UAN,069,070,ABCD,WXYZ,Air Seattle codeshare

    /// - Both CRLF and LF line endings are accepted.

    /// - Lines that cannot be parsed are skipped and the user will be notified in the terminal.

    /// - This program must be called from a terminal, as follows (brackets: required, curly brackets: optional switches)
    ///   prompt> unamiCSV.exe [inputFileName] [outputFileName] {-noHeader} {-noEndComma}
    ///   input and outputFileName must be in the positions listed. All other arguments can be in any order.
    ///   See the corresponding <param></param>'s for explanations.

    /// </remarks>

    /// <param pos=0 name="InputFileName">
    /// The file containing routes to compile. See <remarks></remarks> for more details.
    /// </param>

    /// <param pos=1 name="OutputFileName">
    /// The file to output the compiled CSV to.
    /// The program will include a header, unless the -noHeader option is checked:
    /// output> csgn,csgn_out,csgn_ret,dep,arr,comment
    /// Thus the CSV will be self-documenting.
    /// </param>

    /// <switch name="noHeader">
    /// When checked: Removes the header.
    /// </switch>

    /// <switch name="noEndComma">
    /// When checked: Removes the "extra" comma that will be at the end of lines (to be clear, before the newline) when the comment is not present. For example:
    /// input> UAN000/001 KDEN Denver to KBOI Boise
    /// will normally be compiled to:
    /// output> UAN,000,001,KDEN,KBOI,
    /// and with the noEndComma switch will be compiled to:
    /// output> UAN,000,001,KDEN,KBOI
    /// It is important to note that the latter is not standard CSV, and it is recommended to not use this switch for most purposes.
    /// The comma of focus in this switch will not be removed if there is a comment. This does not mean that there will be a comma at the end of the line in this case; there will not, as there is no field defined in the header after the position at "comment".
    /// <switch>

    ///////////////////////////////////////////////////////////////////////////////////////////////
    // Properties

    // This is the name of the comment field. This makes it easier to change later (in case we need to do that)
    static string Comment = "comment";

    // Similar purpose as above...
    static string With = " with ";

    // Header of CSV file
    static string[] headerItems = {
        "csgn",
        "csgn_out",
        "csgn_ret",
        "dep",
        "arr",
        Comment
    };

    static string header = string.Join(',', headerItems);

    // Regex to parse lines
    static Regex regex = new Regex(@"^(?<csgn>[A-Z]{3})(?<csgn_out>\d{3,4})\/(?<csgn_ret>\d{3,4}) (?<dep>[A-Z]{4}) .*to (?<arr>[A-Z]{4})(?<comment>.*)$");

    // Names of switches
    public static class SwitchNames
    {
        public static string NoHeader   = "-noHeader";
        public static string NoEndComma = "-noEndComma";
    }

    // Arguments/switches
    static string InputFileName, OutputFileName;
    static bool noHeader, noEndComma;

    // Input data
    static string[] inputData;

    // CSV output data
    static string data = String.Empty;

    // Was there an error with any of the lines?
    static bool compileError = false;

    ///////////////////////////////////////////////////////////////////////////////////////////////
    // Methods

    public static void MissingArg (string argName)
    {
        Console.WriteLine("Missing argument: " + argName);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
    // Entry point

    public static void Main (string[] Args)
    {

        // Argument and 

        // Input file
        try
        {
            InputFileName = Args[0];
        } 
        catch (Exception e)
        {
            MissingArg("Input filename");
            goto notSuccessful;
        }
        
        // Output file
        try
        {
            OutputFileName = Args[1];
        }
        catch (Exception e)
        {
            MissingArg("Output filename");
            goto notSuccessful;
        }

        // Switches
        noHeader = Args.Contains(SwitchNames.NoHeader);
        noEndComma = Args.Contains(SwitchNames.NoEndComma);

        // Change some stuff based on switches
        if(!noHeader)
        {
            data += header + Environment.NewLine;
        }

        // -noEndComma is handled later.

        //////////////////////////////////////////////////
        // Read the file

        // Try reading the file
        try
        {
            inputData = File.ReadAllLines(InputFileName);
        }
        // File doesn't exist?
        catch (FileNotFoundException exception)
        {
            Console.WriteLine($"Error: {InputFileName} does not exist.");
            goto notSuccessful;
        }
        // Something else went wrong?
        catch (Exception exception)
        {
            Console.WriteLine("Something went wrong while trying to read " + InputFileName);
            goto notSuccessful;
        }

        //////////////////////////////////////////////////
        // Compile the file (hey, that rhymes!)

        // For each line in the input file
        foreach (string line in inputData)
        {
            // Get all matches of the regex to the current line
            MatchCollection matches = regex.Matches(line);

            // Number of matches
            switch(matches.Count)
            {
                // Since the regex matches the string from beginning to end,
                // there cannot be multiple matches,
                // and so there are only two possible cases: No match and one match.

                // It's a match!
                case 1:

                    // There's only one match, so a loop isn't technically required
                    // but I don't know how else to access the item
                    // (it's not a normal array, it's a collection, which is unordered)
                    foreach(Match match in matches)
                    {
                        // Named groups captured by the regex
                        GroupCollection groups = match.Groups;

                        // For each item in the header (really, this line)
                        foreach(string item in headerItems)
                        {
                            // The value of the current item
                            string val = groups[item].Value;

                            // If the current item is not in the Comment field
                            if(item != Comment)
                            {
                                // Add the corresponding item and a comma
                                data += val;
                                
                                // If -noEndComma switch is checked and there isn't a comment on this line, skip the ending comma
                                if(noEndComma && item == "arr" && !groups[Comment].Value.Contains(With))
                                {
                                    continue;
                                }
                                
                                // Add a comma for the next item
                                data += ',';
                            }
                            // Otherwise if the item is actually a comment
                            else if(val.Contains(With))
                            {
                                // Add just the comment and disregard the rest.
                                // No comma is added because it's the last item.
                                data += val.Split(With).Last();
                            }
                        }

                        // End the line
                        data += Environment.NewLine;
                    }

                    // End of switch
                    break;

                // "That was a nice date but I think you are not really my type."
                case 0:

                    // If this is the first error (and the compileError flag has not been set yet)
                    if(!compileError)
                    {
                        compileError = true;
                        Console.WriteLine("The following lines could not be compiled:");
                    }

                    // Proceed with outputting the line, as usual
                    Console.WriteLine("- " + line);

                    // Exit the switch
                    break;
            }
        }

        //////////////////////////////////////////////////
        // Write the file

        // Try writing
        try {
            // Write the results to the output file
            File.WriteAllText(OutputFileName, data);

            // We're done!
            return;

        // Something went wrong?
        } catch (Exception e)
        {
            Console.WriteLine($"Something went wrong while writing to {OutputFileName}.");
            goto notSuccessful;
        }

        // A goto statement will bring the program here if something went wrong.
        notSuccessful:
            Console.WriteLine("Compilation unsuccessful. No files were modified.");
            return;
    }
}
