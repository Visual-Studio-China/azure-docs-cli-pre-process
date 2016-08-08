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
                var result = Parser.Default.ParseArguments<Options>(args);
                result.MapResult(
                    options =>
                    {
                        new DocPreprocessor().Run(options);
                        return 0;
                    },
                    errors =>
                    {
                        throw new ApplicationException(string.Join("|", errors.Select(e => e.ToString())));
                    });
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
