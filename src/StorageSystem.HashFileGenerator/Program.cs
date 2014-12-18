using System;

namespace Qoollo.HashFileGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 0)
                HashFileGen.Generate(args[0], args);
            else
            {
                Console.WriteLine("Input file name");
                string name = Console.ReadLine();
                HashFileGen.Generate(name);
            }
        }
    }
}
