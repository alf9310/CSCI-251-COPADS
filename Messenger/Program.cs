﻿using System.Text.Json;
using System.Numerics;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;

/// CSCI-251 COPADS Project 3
/// Audrey Fuller 
/// alf9310@rit.edu
/// 4/10/2024

/// <summary> Use network protocols to be able to send secure messages to other 
/// people and decode messages sent to you </summary>
public static class Program{
    
    /// <summary> Allows Main to be run Async </summary>
    /// <param name="args"> User input </param>
    static void Main(string[] args){
        AsyncMain(args).GetAwaiter().GetResult();
    }

    /// <summary> Takes a user input (keyGen, sendKey, getKey, sendMsg or getMsg 
    /// and additional input</summary>
    /// <param name="args"> User input </param>
    static async Task AsyncMain(string[] args){
        try {
            // Check user input length
            if (args.Length != 3 && args.Length != 2){
                Console.WriteLine("Invalid Argument Count");
                PrintHelpMessage();
                return;
            }

            // Check user input command
            switch(args[0]){
                case "keyGen":
                    int keysize = 0;
                    // Invalid additional input
                    if (args.Length != 2 || !(int.TryParse(args[1], out keysize)) || keysize < 0){
                        Console.WriteLine("Improper usage of keyGen");
                        PrintHelpMessage();
                        return;
                    }

                    await KeyGen(keysize);
                    break;

                case "sendKey":
                    // Invalid additional input
                    string email = "";
                    if (args.Length != 2){
                        Console.WriteLine("Improper usage of sendKey");
                        PrintHelpMessage();
                        return;
                    }
                    email = args[1];

                    await SendKey(email);
                    break;

                case "getKey":
                    // Invalid additional input
                    if (args.Length != 2 ){
                        Console.WriteLine("Improper usage of getKey");
                        PrintHelpMessage();
                        return;
                    }
                    email = args[1];

                    await GetKey(email);
                    break;

                case "sendMsg":
                    // Invalid additional input
                    if (args.Length != 3){
                        Console.WriteLine("Improper usage of sendMsg");
                        PrintHelpMessage();
                        return;
                    }
                    email = args[1];
                    string plaintext = args[2];

                    await SendMsg(email, plaintext);
                    break;

                case "getMsg":
                    // Invalid additional input
                    if (args.Length != 2 ){
                        Console.WriteLine("Improper usage of getMsg");
                        PrintHelpMessage();
                        return;
                    }
                    email = args[1];

                    await GetMsg(email);
                    break;

                default:
                    Console.WriteLine("Invalid Argument Option");
                    PrintHelpMessage();
                    return;
            }
        } catch {
            Console.WriteLine("Unexpected Error");
            PrintHelpMessage();
            return;
        }
    }

    // ----------------------------- Argument Methods -----------------------------

    /// <summary> Generate a keypair of size keysize bits (public and private
    /// keys) and store them locally on the disk (in files called 
    /// public.key and private.key respectively), in the current directory. </summary>
    /// <param name="keysize"> User inputed size of keypair (bits) </param>
    static async Task KeyGen(int keysize) {
        // Calculate the size of p
        Random random = new Random();
        int pSize = random.Next((int)(keysize * 0.7), (int)(keysize * 1.3));
        // Calculate the size of q
        int qSize = keysize - pSize;

        // Generate two prime numbers
        BigInteger p = GeneratePrime(pSize);
        BigInteger q = GeneratePrime(qSize);

        // Calculate N and T
        BigInteger N = BigInteger.Multiply(p, q);
        BigInteger T = BigInteger.Multiply(p - 1, q - 1);

        // Choose a prime number E
        BigInteger E = GeneratePrime(16);

        // Calculate D using modular inverse
        BigInteger D = modInverse(E, T);

        // Encode public and private keys
        byte[] publicKeyBytes = EncodeKey(E, N);
        byte[] privateKeyBytes = EncodeKey(D, N);

        // Base64 encode the keys
        string publicKeyBase64 = Convert.ToBase64String(publicKeyBytes);
        string privateKeyBase64 = Convert.ToBase64String(privateKeyBytes);

        // Convert to public & private key objects
        PublicKey publicKey = new PublicKey("", publicKeyBase64);
        PrivateKey privateKey = new PrivateKey(new List<string>(), privateKeyBase64);

        // Serialize the Key objects to JSON string
        string publicJsonString = JsonSerializer.Serialize(publicKey, new JsonSerializerOptions { WriteIndented = true });
        string privateJsonString = JsonSerializer.Serialize(privateKey, new JsonSerializerOptions { WriteIndented = true });

        // Write keys to files
        await File.WriteAllTextAsync("public.key", publicJsonString);
        await File.WriteAllTextAsync("private.key", privateJsonString);
    }

    /// <summary> Sends the public key that was generated in the keyGen phase to the 
    /// server, with the email address given. This should be your email address. 
    /// The server will then register this email address as a valid receiver of
    /// messages. The private key will remain locally, though the email address 
    /// that was given should be added to the private key for later validation. 
    /// If the server already has a key for this user, it will be overwritten. </summary>
    /// <param name="email"> User inputed email string </param>
    static async Task SendKey(string email) {
        // TODO
    }

    /// <summary> Retrieve public key for a particular user (usually not yourself).
    /// Stored in the local filesystem as email.key. </summary>
    /// <param name="email"> User inputed email string </param>
    static async Task GetKey(string email) {
        // Set up new client to connect to server
        HttpClient client = new HttpClient();
        try {
            using HttpResponseMessage response = await client.GetAsync("http://voyager.cs.rit.edu:5050/Key/" + email);
            // Error with retrieving key
            if (!response.IsSuccessStatusCode) {
                Console.WriteLine("Key not Retrieved");
                PrintHelpMessage();
                return;
            }
            string responseBody = await response.Content.ReadAsStringAsync();

            // Parse the JSON response using System.Text.Json
            JsonDocument jsonDoc = JsonDocument.Parse(responseBody);

            // Deserialize JSON response into PublicKey object
            PublicKey? publicKey = JsonSerializer.Deserialize<PublicKey>(responseBody);
            // Verify publicKey is not null
            if (publicKey == null){
                Console.WriteLine("Unable to deserialize key");
                PrintHelpMessage();
                return;
            }

            // Verify Email Matches
            if (publicKey.email != email){
                Console.WriteLine("Email does not match");
                PrintHelpMessage();
                return;
            }

            // Verify Key Exists
            if (string.IsNullOrEmpty(publicKey.key)){
                Console.WriteLine("Key not found");
                PrintHelpMessage();
                return;
            }

            // Serialize the PublicKey object back to JSON string
            string jsonString = JsonSerializer.Serialize(publicKey, new JsonSerializerOptions { WriteIndented = true });

            // Write to local file system
            string fileName = $"{email}.key";
            File.WriteAllText(fileName, jsonString);

        } catch {
            // Catch errors
            Console.WriteLine("GetKey Error");
            PrintHelpMessage();

        } finally {
            client.Dispose();
        }
    }

    /// <summary> Takes a plaintext message and encrypts it using the public key of the 
    /// person you are sending it to, based on their email address. It will 
    /// base64 encode the message it before sending it to the server. </summary>
    /// <param name="email"> User inputed email string </param>
    /// <param name="plaintext"> User inputed message to be sent </param>
    static async Task SendMsg(string email, string plaintext) {
        // TODO
    }

    /// <summary> Retrieve a message for a particular user. While it is possible
    /// to download messages for any user, you can only decode messages for
    /// which you have the private key. If you download messages for which 
    /// you don't have the private key, those messages can't be decoded. </summary>
    /// <param name="email"> User inputed email string </param>
    static async Task GetMsg(string email) {
        // TODO
    }

    // ----------------------------- Helper Methods -----------------------------

    /// <summary> Helper Method to generate prime numbers </summary>
    /// <param name="bits"> size of prime number being generated </param>
    /// <returns> A prime number of size bits </returns>
    static BigInteger GeneratePrime(int bits){
        BigInteger prime;
        do{
            prime = GenerateRandomNum(bits);
        } while (prime % 2 == 0 || !prime.IsProbablyPrime()); // Odd & prime check

        return prime;
    }

    /// <summary> Helper Method to check if a number is prime </summary>
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

    /// <summary> Helper Method BigInteger Random Number generator using RandomNumberGenerator </summary>
    /// <param name="bits"> Number of bits in the random number </param>
    /// <returns> random BigInteger </returns>
    private static BigInteger GenerateRandomNum(int bits){
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create()){
            byte[] bytes = new byte[bits / 8];
            rng.GetBytes(bytes);
            bytes[bytes.Length - 1] &= 0x7F;
            BigInteger num = new BigInteger(bytes);
            return num;
        }
    }

    /// <summary> Helper method to calculate modular inverse of two BigIntegers </summary>
    /// <param name="a"> A BigInteger the method is called on </param>
    /// <param name="b"> A BigInteger the method is called on </param>
    /// <returns> The big integer modular inverse of a and b </returns>
    static BigInteger modInverse(BigInteger a, BigInteger b){
        BigInteger i = b, v = 0, d = 1;
        while (a > 0) {
            BigInteger z = i / a, x = a;
            a = i % x;
            i = x;
            x = d;
            d = v - z * x;
            v = x;
        }
        v %= b;
        if (v < 0) v = (v + b) % b;
        return v;
    }

    /// <summary> Helper Function to encode the key </summary>
    /// <param name="a"> A BigInteger the method is called on </param>
    /// <param name="b"> A BigInteger the method is called on </param>
    /// <returns> Byte Array Encoded Key </returns>
    static byte[] EncodeKey(BigInteger eOrD, BigInteger N){
        byte[] eOrDBytes = eOrD.ToByteArray();
        byte[] NBytes = N.ToByteArray();

        // Combine eOrD and N bytes
        byte[] encodedKey = new byte[eOrDBytes.Length + NBytes.Length];
        Buffer.BlockCopy(eOrDBytes, 0, encodedKey, 0, eOrDBytes.Length);
        Buffer.BlockCopy(NBytes, 0, encodedKey, eOrDBytes.Length, NBytes.Length);

        return encodedKey;
    }

    /// <summary> Helper method for the main function that 
    /// prints a generic help message </summary>
    static void PrintHelpMessage(){
        Console.WriteLine(@"Usage: dotnet run <option> <other arguments>
Send secure messages to other people and decode messages sent to you.

Must include one option:
keyGen <keysize>    
                    Generate a keypair of size keysize bits (public and private
                    keys) and store them locally on the disk (in files called 
                    public.key and private.key respectively), in the current directory.
sendKey <email>
                    Sends the public key that was generated in the keyGen phase to the 
                    server, with the email address given. This should be your email address. 
                    The server will then register this email address as a valid receiver of
                    messages. The private key will remain locally, though the email address 
                    that was given should be added to the private key for later validation. 
                    If the server already has a key for this user, it will be overwritten.
getKey <email>      
                    Retrieve public key for a particular user (usually not yourself).
                    Stored in the local filesystem as <email>.key.
sendMsg <email> <plaintext> 
                    Takes a plaintext message and encrypts it using the public key of the 
                    person you are sending it to, based on their email address. It will 
                    base64 encode the message it before sending it to the server.
getMsg <email>      
                    Retrieve a message for a particular user. While it is possible
                    to download messages for any user, you can only decode messages for
                    which you have the private key. If you download messages for which 
                    you don't have the private key, those messages can't be decoded.");
        return;
    }
}

/// <summary> Class to store Public Keys</summary>
public class PublicKey{
    public string? email { get; set; }
    public string? key { get; set; }
    public PublicKey(string? email, string? key){
        this.email = email;
        this.key = key;
    }
}

/// <summary> Class to store Private Keys</summary>
public class PrivateKey{
    public List<string>? emails { get; set; }
    public string? key { get; set; }
    public PrivateKey(List<string>? emails, string? key){
        this.emails = emails;
        this.key = key;
    }
}