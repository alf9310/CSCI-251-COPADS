/// CSCI-251 COPADS Project 2
/// Audrey Fuller 
/// alf9310@rit.edu
/// 3/19/2024

using System;
using System.Numerics;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Security.Cryptography;

/// <summary> Generates large prime numbers and the factors of odd numbers </summary>
public static class Program{
    /// <summary> Takes 3 user inputs, bits, option & count </summary>
    /// <param name="args"> <bits> <option> <count> </param>
    static void Main(string[] args){
        // Check User Input
        if (args.Length != 3 && args.Length != 2){
            PrintHelpMessage();
            return;
        }
        // Check for valid bits
        int bits = 32;
        if (!(int.TryParse(args[0], out bits) && bits >= 32 && bits % 8 == 0)){
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
        // Main Loops
        if (option == "odd"){ // Generate odds
            for (int index = 1; index < count + 1; index++){
                BigInteger bi = GenerateRandomNum(bits);
                while( bi % 2 == 0){ // Odd check
                    bi = GenerateRandomNum(bits);
                }
                int factors = bi.FactorsNum();
                Console.WriteLine("{0}: {1}", index, bi);
                Console.WriteLine("Number of factors: {0}", factors);
            }
        } else { // Generate primes
            for (int index = 1; index < count + 1; index++){
                BigInteger bi = GenerateRandomNum(bits);
                // TODO all multithreading right below here
                while(bi % 2 == 0 || !bi.IsProbablyPrime()){ // Odd & prime check
                    bi = GenerateRandomNum(bits);
                }
                Console.WriteLine("{0}: {1}", index, bi);
            }
        }
        // Stop Stopwatch
        time.Stop();
        Console.WriteLine("Time to Generate: {0}", time.Elapsed);
    }

    /// <summary> Implementation of the Miller-Rabin primality test </summary>
    /// <param name="value"> An odd BigInteger the method is called on </param>
    /// <param name="k"> Witness Number (# of testing rounds) </param>
    /// <returns> Boolean true if value is probably prime, false otherwise </returns>
    static Boolean IsProbablyPrime(this BigInteger value, int k = 10){
        // Write value as 2^(r) * d + 1 where s > 0 and d is odd and > 0
        BigInteger d = value - 1;
        int s = 0;
        while (d % 2 == 0){
            d /= 2;
            s++;
        }

        byte[] bytes = value.ToByteArray();
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create()){
        // Witness loop
            for (int i = 0; i < k; i++){
                BigInteger a;
                do{
                    // Pick random integer a in range [2, value - 2]
                    rng.GetBytes(bytes);
                    a = new BigInteger(bytes) % (value - 3) + 2;
                } while (a < 2 || a >= value - 1);

                BigInteger x = BigInteger.ModPow(a, d, value);
                if (x == 1 || x == value - 1){
                    continue;
                }

                for (int j = 1; j < s; j++){
                    x = BigInteger.ModPow(x, 2, value);
                    if (x == 1){
                        return false;
                    }
                    if (x == value - 1){
                        break;
                    }
                }

                if (x != value - 1){
                    return false;
                }
            }
        }
        return true;
    }

    // TODO add multithreading in this function
    static int FactorsNum(this BigInteger value){ 
        int factors = 0;
        BigInteger sqrt = Sqrt(value);
        for (BigInteger i = 1; i <= sqrt; i++) { 
            if (value % i == 0) { 
                factors++;
                if (value / i != i){ // If divisors are not equal, add two 
                    factors ++;
                }
            } 
        } 
        return factors;
    } 

    private static BigInteger Sqrt(BigInteger n){
        if (n == 0){
            return 0;
        }

        BigInteger left = 1;
        BigInteger right = n;
        BigInteger result = 0;

        while (left <= right){
            BigInteger mid = (left + right) / 2;
            if (mid * mid <= n){
                result = mid;
                left = mid + 1;
            } else{
                right = mid - 1;
            }
        }

        return result;
    }


    private static BigInteger GenerateRandomNum(int bits){
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create()){
            byte[] bytes = new byte[bits / 8];
            rng.GetBytes(bytes);
            bytes[bytes.Length - 1] &= 0x7F;
            BigInteger num = new BigInteger(bytes);
            return num;
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