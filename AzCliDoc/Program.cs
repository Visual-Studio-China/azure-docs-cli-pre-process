using CommandLine;
using System;
using System.Linq;

namespace AzCliDocPreprocessor
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Options options = new Options();
                if (Parser.Default.ParseArguments(args, options))
                {
                    new DocPreprocessor().Run(options);
                }
                else
                    throw new ApplicationException("Invalid arguments.");
            }
            catch (Exception e)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.ToString());
                Console.ForegroundColor = color;
                throw;
            }
        }
    }
}
