using System.Collections.Generic;

namespace WinNvm
{
    public class NodeVersions
    {
        public string Version { get; set; }
        public string Date { get; set; }
        public List<string> Files { get; set; }
    }

    public class RcFileData
    {
        public string NodeMirror { get; set; }
    }
}