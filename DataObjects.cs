using System.Collections.Generic;

namespace WinNvm
{
    public class NodeVersions
    {
        public string Version { get; set; }
        public string Date { get; set; }
        public List<string> Files { get; set; }
    }

    public class RCFileData
    {
        public string NodeMirror { get; set; }
    }
}
