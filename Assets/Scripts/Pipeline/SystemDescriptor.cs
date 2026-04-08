using System.Collections.Generic;

namespace MimicFacility.Pipeline
{
    public class SystemDescriptor
    {
        public string Id;
        public List<string> Dependencies = new List<string>();
    }
}
