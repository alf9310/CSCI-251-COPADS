/// <summary> Class Program defines the modified du command </summary>
class Program{
    /// <summary> Method Main takes 2 user inputs, a flag and a path, 
    /// running a custom single and/or multi-threaded du command </summary>
    /// <typeparam name="args">User input [-s] [-d] [-b] <path>.</typeparam>
    static void Main(string[] args){

        // Write help message when invalid command line arguments are given (wrong # of args, flag or invalid path)
        if (args.Length != 2 || (args[0] != "-s" && args[0] != "-d" && args[0] != "-b") || !Directory.Exists(args[1])){
            Console.WriteLine(@"Usage: du [-s] [-d] [-b] <path>
Summarize disk usage of the set of FILES, recursively for directories.

You MUST specify one of the parameters, -s, -d, or -b
-s       Run in single threaded mode
-d       Run in parallel mode (uses all available processors)
-b       Run in both single threaded and parallel mode.
         Runs parallel followed by sequential mode");
            return;
        }

        // Stores user input
        string flag = args[0];
        string path = args[1];

        Console.WriteLine("Directory \'{0}\':\n", path);

        // Runs parallel or sequential function depending on flag
        if (flag == "-s"){
            RunSequential(path);
        } else if (flag == "-d"){
            RunParallel(path);
        } else if (flag == "-b"){
            RunParallel(path);
            RunSequential(path);
        }
    }

    /// <summary> Method RunSequential prints folders, files, total file sizes, 
    /// images, total image sizes and time taken to run. Single threaded. </summary>
    /// <typeparam name="path"> path to file system </typeparam>
    static void RunSequential(string path){
        // Initialize stopwatch and counters
        var time = new System.Diagnostics.Stopwatch();
        time.Start();

        int folders = 0;
        int files = 0;
        long file_bytes = 0;
        int images = 0;
        long image_bytes = 0;
        
        // Store contents of path, ignoring ones that are restricted
        var enumerate = Directory.EnumerateFileSystemEntries(path, "*", new EnumerationOptions { IgnoreInaccessible=true });

        // Loop through path contents, checking if it's a directory, file or image file and increasing counters
        foreach (var file in enumerate){
            if (File.GetAttributes(file).HasFlag(FileAttributes.Directory)){
                folders++;

            } else {
                files++;

                file_bytes += (new FileInfo(file)).Length;

                if(IsImage(file)){
                    images++;
                    image_bytes += (new FileInfo(file)).Length;
                }
            }
        }
        // Stop stopwatch and print results
        time.Stop();
        Console.WriteLine("Sequential Calculated in: {0}s", time.Elapsed.TotalSeconds);
        // Print counter results
        Console.WriteLine("{0} folders, {1} files, {2} bytes", folders, files, file_bytes);
        if (images != 0){
            Console.WriteLine("{0} image files, {1} bytes\n", images, image_bytes);
        } else {
            Console.WriteLine("No image files found in the directory\n");
        }

        return;
    }

    /// <summary> Method RunParallel prints folders, files, total file sizes, 
    /// images, total image sizes and time taken to run. Multi-threaded. </summary>
    /// <typeparam name="path"> path to file system </typeparam>
    static void RunParallel(string path){
        // Initialize stopwatch and counters
        var time2 = new System.Diagnostics.Stopwatch();
        time2.Start();

        int folders = 0;
        int files = 0;
        long file_bytes = 0;
        int images = 0;
        long image_bytes = 0;
        
        // Store contents of path, ignoring ones that are restricted
        var enumerate = Directory.EnumerateFileSystemEntries(path, "*", new EnumerationOptions { IgnoreInaccessible=true });

        // Loop through path contents, checking if it's a directory, file or image file and increasing counters
        Parallel.ForEach(enumerate, () => (folders: 0, files: 0, file_bytes: 0L, images: 0, image_bytes: 0L),
        (file, loopState, localCounts) => {
            // localCounts accumulates counts within each thread
            if (File.GetAttributes(file).HasFlag(FileAttributes.Directory)){
                localCounts.folders++;

            } else {
                localCounts.files++;

                var fileInfo = new FileInfo(file);
                localCounts.file_bytes += fileInfo.Length;

                if (IsImage(file)){
                    localCounts.images++;
                    localCounts.image_bytes += fileInfo.Length;
                }
            }
            return localCounts;
        },
        // Sum up local counts for each thread
        localCounts => {
            Interlocked.Add(ref folders, localCounts.folders);
            Interlocked.Add(ref files, localCounts.files);
            Interlocked.Add(ref file_bytes, localCounts.file_bytes);
            Interlocked.Add(ref images, localCounts.images);
            Interlocked.Add(ref image_bytes, localCounts.image_bytes);
        });

        // Stop stopwatch and print results
        time2.Stop();
        Console.WriteLine("Parallel Calculated in: {0}s", time2.Elapsed.TotalSeconds);
        // Print counter results
        Console.WriteLine("{0} folders, {1} files, {2} bytes", folders, files, file_bytes);
        if (images != 0){
            Console.WriteLine("{0} image files, {1} bytes\n", images, image_bytes);
        } else {
            Console.WriteLine("No image files found in the directory\n");
        }

        return;
    }

    /// <summary> Method IsImage determines if file is image by checking extension. </summary>
    /// <typeparam name="file"> file being checked </typeparam>
    /// <returns> True if file is an image, false otherwise </returns>
    static bool IsImage(string file){
        string[] extensions = { ".jpg", ".gif", ".jpeg", ".svg", ".tiff", ".bmp", ".png" };
        return extensions.Contains(Path.GetExtension(file.ToLower()));
    }
}