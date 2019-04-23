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
                    Console.WriteLine("Starting...");
                    new DocPreprocessor().Run(options);
                    Console.WriteLine("Finished!");
                }
                else
                    throw new ApplicationException("Invalid arguments.");
            }
            catch (Exception e)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.ToString());
                Console.WriteLine(e.StackTrace);
                Console.ForegroundColor = color;
                throw;
            }
        }
    }
}
