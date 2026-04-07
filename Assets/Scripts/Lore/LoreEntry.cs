using System;
using System.Collections.Generic;

namespace MimicFacility.Lore
{
    public enum ELoreChannel
    {
        Environmental,
        Terminal,
        Director
    }

    public enum ELoreClassification
    {
        MEMO,
        LOG,
        REDACTED,
        SYSTEM
    }

    [Serializable]
    public class LoreEntry
    {
        public string entryId;
        public string terminalId;
        public string title;
        public string content;
        public string author;
        public ELoreClassification classification;
        public ELoreChannel channel;
        public int minCorruptionToReveal;
        public bool isRedacted;
        public string redactedContent;
        public string zoneTag;
    }

    [Serializable]
    public class LoreEntryCollection
    {
        public List<LoreEntry> entries = new List<LoreEntry>();
    }
}
