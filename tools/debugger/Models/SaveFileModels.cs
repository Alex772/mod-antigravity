namespace Antigravity.Debugger.Models
{
    /// <summary>
    /// ONI Save file header structure
    /// </summary>
    public class SaveFileHeader
    {
        public uint BuildVersion { get; set; }
        public int HeaderSize { get; set; }
        public uint HeaderVersion { get; set; }
        public int Compression { get; set; }
        public bool IsCompressed => Compression != 0;
    }

    /// <summary>
    /// ONI GameInfo from save file (JSON)
    /// </summary>
    public class SaveGameInfo
    {
        public int numberOfCycles { get; set; }
        public int numberOfDuplicants { get; set; }
        public string? baseName { get; set; }
        public bool isAutoSave { get; set; }
        public string? originalSaveName { get; set; }
        public int saveMajorVersion { get; set; }
        public int saveMinorVersion { get; set; }
        public string? clusterId { get; set; }
        public string[]? worldTraits { get; set; }
        public bool sandboxEnabled { get; set; }
        public string? colonyGuid { get; set; }
        public string? dlcId { get; set; }
        public System.Collections.Generic.List<string>? dlcIds { get; set; }
    }

    /// <summary>
    /// Parsed save data combining header and game info
    /// </summary>
    public class ParsedSaveData
    {
        public SaveFileHeader? Header { get; set; }
        public SaveGameInfo? GameInfo { get; set; }
        public int WorldWidth { get; set; }
        public int WorldHeight { get; set; }
        public long CompressedSize { get; set; }
        public long DecompressedSize { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsValid => Header != null && GameInfo != null && string.IsNullOrEmpty(ErrorMessage);
    }
}
