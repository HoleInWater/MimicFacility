using System.Collections.Generic;

namespace MimicFacility.Pipeline
{
    public class ImportResult
    {
        public bool Success;
        public string Message;
        public List<string> FailedSystems = new List<string>();
        public List<string> Log = new List<string>();
        public DependencyResolutionResult Resolution;

        public ImportResult Fail(string message, List<string> issues = null)
        {
            Success = false;
            Message = message;
            if (issues != null)
                Log.AddRange(issues);
            return this;
        }

        public ImportResult Succeed(DependencyResolutionResult resolution)
        {
            Success = true;
            Resolution = resolution;
            Message = "Import completed successfully.";
            return this;
        }

        public void RecordFailure(string systemId, string reason)
        {
            FailedSystems.Add(systemId);
            Log.Add($"Failed to install {systemId}: {reason}");
        }
    }

    public class ImportSettings
    {
        public bool AutoInstallMissingSystems = true;
        public bool AbortOnInstallFailure = false;
        public bool BakeNavMesh = true;
        public bool CreateNewScene = true;
    }
}
