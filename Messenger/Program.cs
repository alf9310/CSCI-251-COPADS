/// CSCI-251 COPADS Project 3
/// Audrey Fuller 
/// alf9310@rit.edu
/// 4/10/2024

// <summary> Use network protocols to be able to send secure messages to other 
// people and decode messages sent to you </summary>
public static class Program{
    /// <summary> Takes a user input (keyGen, sendKey, getKey, sendMsg or getMsg 
    /// and additional input</summary>
    /// <param name="args"> User input </param>
    static void Main(string[] args){
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
                break;

            case "getKey":
                // Invalid additional input
                if (args.Length != 2 ){
                    Console.WriteLine("Improper usage of getKey");
                    PrintHelpMessage();
                    return;
                }
                email = args[1];
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
                break;

            case "getMsg":
                // Invalid additional input
                if (args.Length != 2 ){
                    Console.WriteLine("Improper usage of getMsg");
                    PrintHelpMessage();
                    return;
                }
                email = args[1];
                break;

            default:
                Console.WriteLine("Invalid Argument Option");
                PrintHelpMessage();
                return;
        }
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
getKey <email>      
                    Retrieve public key f or a particular user (usually not yourself).
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