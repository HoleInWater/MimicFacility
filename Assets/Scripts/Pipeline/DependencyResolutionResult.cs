using System.Collections.Generic;
using System.Linq;

namespace MimicFacility.Pipeline
{
    public class DependencyResolutionResult
    {
        public List<string> RequiredSystems = new List<string>();
        public List<string> PresentSystems = new List<string>();
        public List<string> MissingSystems = new List<string>();
        public List<string> Issues = new List<string>();

        public bool HasFatalIssues => Issues.Any();
    }
}
