using System;
using System.Numerics;
using System.Diagnostics;
using System.Threading;
using System.Security.Cryptography;

/// <summary> Generates large prime numbers and the factors of odd numbers </summary>
class Program{
    /// <summary> Takes 3 user inputs, bits, option & count </summary>
    /// <typeparam name="args"> <bits> <option> <count> </typeparam>
    static void Main(string[] args){
        // Check User Input
        if (args.Length != 3 && args.Length != 2){
            PrintHelpMessage();
            return;
        }
        // Check for valid bits
        BigInteger bits = new BigInteger(0);
        if (!(BigInteger.TryParse(args[0], out bits) && bits >= 32 && bits % 8 == 0)){
            Console.WriteLine("Invalid Argument Bits");
            PrintHelpMessage();
            return;
        } 
        // Check for valid option
        string option = args[1];
        if (option != "odd" && option != "prime"){
            Console.WriteLine("Invalid Argument Option");
            PrintHelpMessage();
            return;
        } 
        // Check for valid count (if no count entered, default to 1)
        int count = 1;
        if (args.Length == 3 && !int.TryParse(args[2], out count) || count < 0){
            Console.WriteLine("Invalid Argument Count");
            PrintHelpMessage();
            return;
        } 

        Console.WriteLine("BitLength: {0} bits", bits);
        // Initialize Stopwatch
        Stopwatch time = new Stopwatch();
        time.Start();
        // Main Loop
        for (int index = 1; index < count + 1; index++){
            //var bi = new BigInteger(32);
            //Console.WriteLine(bi);
            if (option == "odd"){ // Generate odds

            } else if (option == "prime"){ // Generate primes

            }

            Console.WriteLine("{0}: ", index);
        }
        // Stop Stopwatch
        time.Stop();
        Console.WriteLine("Time to Generate: {0}", time.Elapsed);
    }


    private static byte[] GenerateRandomNum(int size){
        using (var generator = RandomNumberGenerator.Create()){
            var salt = new byte[size];
            generator.GetBytes(salt);
            return salt;
        }
    }

    static void PrintHelpMessage(){
        /// <summary> Helper method to print help message </summary>
        Console.WriteLine(@"Usage: dotnet run <bits> <option> <count>
Generates large prime numbers and the factors of odd numbers. 

Must include these 3 parameters:
bits    The number of bits of the number to be generated, this must
        be a multiple of 8, and at least 32 bits.
option  'odd' or 'prime' (the type of numbers to be generated).
count   The count of numbers to generate, defaults to 1.");
        return;
    }

}