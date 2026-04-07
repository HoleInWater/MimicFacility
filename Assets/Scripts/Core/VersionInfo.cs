using UnityEngine;

namespace MimicFacility.Core
{
    public class VersionInfo : MonoBehaviour
    {
        public static VersionInfo Instance { get; private set; }

        public string FullVersion { get; private set; }
        public string Phase { get; private set; }
        public int[] Segments { get; private set; }
        public int Depth => Segments != null ? Segments.Length : 0;

        private static readonly string[] GreekPhases =
        {
            "Alpha", "Beta", "Gamma", "Delta", "Epsilon",
            "Zeta", "Eta", "Theta", "Iota", "Kappa",
            "Lambda", "Mu", "Nu", "Xi", "Omicron",
            "Pi", "Rho", "Sigma", "Tau", "Upsilon",
            "Phi", "Chi", "Psi", "Omega"
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVersion();
        }

        private void LoadVersion()
        {
            TextAsset versionFile = Resources.Load<TextAsset>("VERSION");

            if (versionFile != null)
            {
                ParseVersion(versionFile.text.Trim());
            }
            else
            {
                // Fallback: try StreamingAssets
                string path = System.IO.Path.Combine(Application.streamingAssetsPath, "VERSION");
                if (System.IO.File.Exists(path))
                {
                    ParseVersion(System.IO.File.ReadAllText(path).Trim());
                }
                else
                {
                    FullVersion = "Unknown-0";
                    Phase = "Unknown";
                    Segments = new int[] { 0 };
                    Debug.LogWarning("VERSION file not found. Using fallback version.");
                }
            }
        }

        private void ParseVersion(string versionString)
        {
            FullVersion = versionString;

            int dashIndex = versionString.IndexOf('-');
            if (dashIndex < 0)
            {
                Phase = "Alpha";
                ParseSegments(versionString);
                return;
            }

            Phase = versionString.Substring(0, dashIndex);
            ParseSegments(versionString.Substring(dashIndex + 1));
        }

        private void ParseSegments(string segmentString)
        {
            string[] parts = segmentString.Split('.');
            Segments = new int[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out int val))
                    Segments[i] = val;
                else
                    Segments[i] = 0;
            }
        }

        public int GetPhaseIndex()
        {
            for (int i = 0; i < GreekPhases.Length; i++)
            {
                if (GreekPhases[i] == Phase)
                    return i;
            }
            return -1;
        }

        public string GetNextPhase()
        {
            int idx = GetPhaseIndex();
            if (idx < 0 || idx >= GreekPhases.Length - 1)
                return null;
            return GreekPhases[idx + 1];
        }

        public int GetSegmentAtDepth(int depth)
        {
            if (depth < 0 || depth >= Segments.Length)
                return 0;
            return Segments[depth];
        }

        public string GetDisplayVersion()
        {
            return $"{Phase} {string.Join(".", Segments)}";
        }

        public override string ToString()
        {
            return FullVersion;
        }
    }
}
