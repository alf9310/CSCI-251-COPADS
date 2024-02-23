class Program{
    static void Main(string[] args){

        // Write help message when invalid command line arguments are given (wrong # of args, flag or invalid path)
        if (args.Length != 2 || (args[0] != "-s" && args[0] != "-d" && args[0] != "-b") || !Directory.Exists(args[1])){
            Console.WriteLine(@"Usage: du [-s] [-d] [-b] <path>
Summarize disk usage of the set of FILEs, recursively for directories.

You MUST specify one of the parameters, -s, -d, or -b
-s       Run in single threaded mode
-d       Run in parallel mode (uses all available processors)
-b       Run in both single threaded and parallel mode.
         Runs parallel followed by sequential mode");
            return;
        }

        string flag = args[0];
        string path = args[1];

        Console.WriteLine("Directory \'{0}\':\n", path);

        if (flag == "-s"){
            RunSequential(path);
        } else if (flag == "-d"){
            RunParallel(path);
        } else if (flag == "-b"){
            RunParallel(path);
            RunSequential(path);
        }
    }

    static void RunSequential(string path){
        var time = new System.Diagnostics.Stopwatch();
        time.Start();

        int folders = 0;
        int files = 0;
        long file_bytes = 0;
        int images = 0;
        long image_bytes = 0;

        var enumerate = Directory.EnumerateFileSystemEntries(path, "*", new EnumerationOptions { IgnoreInaccessible=true });

        foreach (var file in enumerate){
            if (File.GetAttributes(file).HasFlag(FileAttributes.Directory)){
                folders++;

            } else {
                files++;

                var file_info = new FileInfo(file);
                file_bytes += file_info.Length;

                if(IsImage(file)){
                    images++;
                    image_bytes += file_info.Length;
                }
            }
        }

        time.Stop();

        Console.WriteLine("Sequential Calculated in: {0}s", time.Elapsed.TotalSeconds);
        Console.WriteLine("{0} folders, {1} files, {2} bytes", folders, files, file_bytes);

        if (images != 0){
            Console.WriteLine("{0} image files, {1} bytes\n", images, image_bytes);
        } else {
            Console.WriteLine("No image files found in the directory\n");
        }

        return;
    }

    static void RunParallel(string path){
        long time = 0;
        int folders = 0;
        int files = 0;
        long file_bytes = 0;
        int images = 0;
        long image_bytes = 0;

        Console.WriteLine("Parallel Calculated in: {0}s", time);
        Console.WriteLine("{0} folders, {1} files, {2} bytes", folders, files, file_bytes);
        if (images != 0){
            Console.WriteLine("{0} image files, {1} bytes\n", images, image_bytes);
        } else {
            Console.WriteLine("No image files found in the directory\n");
        }
        return;
    }

    static bool IsImage(string file){
        string[] extensions = { ".jpg", ".gif", ".jpeg", ".svg", ".tiff", ".bmp", ".png" };
        return extensions.Contains(Path.GetExtension(file));
    }
}