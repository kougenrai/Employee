using CommandLine;

namespace AssemblySimplify
{
    public class Options
    {
        [Option('d')]
        public string WorkDirectory { get; set; }
    }
}
