using System.Text;
using System.Text.Json;
using System.Numerics;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Buffers.Binary;
using System.Net.Http.Headers;

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
        try {
            // Calculate the size of p
            Random random = new Random();
            int pSize = random.Next((int)(keysize * 0.2), (int)(keysize * 0.8));
            // Calculate the size of q
            int qSize = (keysize - pSize);

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
        } catch {
            // Catch errors
            Console.WriteLine("KeyGen Error");
            PrintHelpMessage();
        }
    }

    /// <summary> Sends the public key that was generated in the keyGen phase to the 
    /// server, with the email address given. This should be your email address. 
    /// The server will then register this email address as a valid receiver of
    /// messages. The private key will remain locally, though the email address 
    /// that was given should be added to the private key for later validation. 
    /// If the server already has a key for this user, it will be overwritten. </summary>
    /// <param name="email"> User inputed email string </param>
    static async Task SendKey(string email) {
        // Set up new client to connect to server
        HttpClient client = new HttpClient();
        try {
            // Get public keys saved locally
            string publicKeyStr = File.ReadAllText("public.key");
            PublicKey? publicKey = JsonSerializer.Deserialize<PublicKey>(publicKeyStr);
            // Set public key email
            if (publicKey != null){
                publicKey.email = email;
            } else {
                Console.WriteLine("Email not Found");
                PrintHelpMessage();
                return;
            }
            
            // Generate content to send
            string jsonString = JsonSerializer.Serialize(publicKey, new JsonSerializerOptions { WriteIndented = true });
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await client.PutAsync("http://voyager.cs.rit.edu:5050/Key/" + email, content);
            
            // Error with putting key
            if (!response.IsSuccessStatusCode) {
                Console.WriteLine("Key not Put");
                PrintHelpMessage();
                return;
            }
            // Key sucessfully put
            Console.WriteLine("Key saved");

        } catch {
            // Catch errors
            Console.WriteLine("SendKey Error");
            PrintHelpMessage();

        } finally {
            client.Dispose();
        }
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
        // Ensure you have the public key for the user you are sending a message to, if not, abort and show error message
        // Take the plaintext message and convert it to a byte array
        // Take the resulting byte array and load it into a big integer
        // Perform the encryption algorithm
        // Convert the resulting big integer to a byte array
        // Base64 encode the byte array
        // Load the base64 encoded byte array and the email into message object and send it to the server
    }

    /// <summary> Retrieve a message for a particular user. While it is possible
    /// to download messages for any user, you can only decode messages for
    /// which you have the private key. If you download messages for which 
    /// you don't have the private key, those messages can't be decoded. </summary>
    /// <param name="email"> User inputed email string </param>
    static async Task GetMsg(string email) {
        // Set up new client to connect to server
        HttpClient client = new HttpClient();
        try{
            // Validate that you have a private key for the email being requested, if not, abort and show error message.
            if (!File.Exists(email + ".key")){
                Console.WriteLine("Email key not found. Cannot decrypt messages.");
                PrintHelpMessage();
                return;
            }

            // Load the JSON object from the server into a local object 
            HttpResponseMessage response = await client.GetAsync($"http://voyager.cs.rit.edu:5050/Message/" + email);
            if (!response.IsSuccessStatusCode){
                Console.WriteLine("Failed to retrieve the message.");
                return;
            }
            string responseBody = await response.Content.ReadAsStringAsync();

            // Decode the base64 encoded message content into a byte array
            byte[] encryptedBytes = Convert.FromBase64String(responseBody);

            // Read the private key
            string privateKeyStr = File.ReadAllText(email + ".key");

            // Extract the size of E (e) and N (n) from the private key
            byte[] privateKeyBytes = Convert.FromBase64String(privateKeyStr);
            int eSize = BitConverter.ToInt32(privateKeyBytes, 0);
            int nSize = BitConverter.ToInt32(privateKeyBytes, sizeof(int));

            // Extract E (little endian) from the private key
            BigInteger E = new BigInteger(privateKeyBytes.Skip(sizeof(int) * 2).Take(eSize).Reverse().ToArray());
            // Extract N (little endian) from the private key
            BigInteger N = new BigInteger(privateKeyBytes.Skip(sizeof(int) * 2 + eSize).Take(nSize).Reverse().ToArray());

            // Convert the byte array to a BigInteger representing the encrypted message
            BigInteger encryptedMessage = new BigInteger(encryptedBytes);

            // Perform the decryption algorithm: PlainText ^ e MOD n = cipher Text
            BigInteger decryptedMessage = BigInteger.ModPow(encryptedMessage, E, N);

            // Convert the resulting big integer to a byte array
            byte[] decryptedBytes = decryptedMessage.ToByteArray();
            // Convert the byte array to a string
            string decryptedText = Encoding.UTF8.GetString(decryptedBytes);

            // Display the message
            Console.WriteLine(decryptedText);
        } catch {
            // Catch errors
            Console.WriteLine("GetMsg Error");
            PrintHelpMessage();

        } finally {
            client.Dispose();
        }
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
    /// <param name="ED"> BigInteger E or D</param>
    /// <param name="ED"> BigInteger N</param>
    /// <returns> Byte Array Encoded Key </returns>
    static byte[] EncodeKey(BigInteger ED, BigInteger N){
        byte[] EDBytes = ED.ToByteArray();
        byte[] ed = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(EDBytes.Length));

        byte[] NBytes = N.ToByteArray();
        byte[] n = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(NBytes.Length));

        // Combine ED and N bytes
        byte[] encodedKey = new byte[ed.Length + EDBytes.Length +n.Length +NBytes.Length];
        Buffer.BlockCopy(ed, 0, encodedKey, 0, ed.Length);
        Buffer.BlockCopy(EDBytes, 0, encodedKey, ed.Length, EDBytes.Length);
        Buffer.BlockCopy(n, 0, encodedKey, ed.Length + EDBytes.Length, n.Length);
        Buffer.BlockCopy(NBytes, 0, encodedKey, ed.Length + EDBytes.Length + n.Length, NBytes.Length);

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