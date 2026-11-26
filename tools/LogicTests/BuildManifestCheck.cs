// Static check of the build entry scene: the local build manifest (ProjectSettings/EditorBuildSettings.asset)
// versus the scene hard-coded in Listonosz.EditorTools.MobileBuild (Editor code, not compilable without Unity,
// so its ScenePath constant is read from source). Pure text parsing — no Unity, no build.
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

internal static class BuildManifestCheck
{
    internal sealed class Entry
    {
        public bool Enabled;
        public string Path;
        public string Guid;
    }

    /// <summary>Scenes listed in EditorBuildSettings.asset text, in order.</summary>
    public static List<Entry> ParseManifest(string yaml)
    {
        var list = new List<Entry>();
        foreach (Match m in Regex.Matches(yaml,
                     @"-\s*enabled:\s*(\d)\s*\r?\n\s*path:\s*(\S+)\s*\r?\n\s*guid:\s*([0-9a-f]+)"))
        {
            list.Add(new Entry { Enabled = m.Groups[1].Value == "1", Path = m.Groups[2].Value, Guid = m.Groups[3].Value });
        }

        return list;
    }

    /// <summary>Entry scene of a player built from the manifest = first enabled scene (null if none).</summary>
    public static Entry EntryScene(List<Entry> manifest)
    {
        foreach (Entry e in manifest) if (e.Enabled) return e;
        return null;
    }

    public static string MobileBuildScenePath(string source)
    {
        Match m = Regex.Match(source, @"const\s+string\s+ScenePath\s*=\s*""([^""]+)""");
        return m.Success ? m.Groups[1].Value : null;
    }

    public static string MetaGuid(string metaText)
    {
        Match m = Regex.Match(metaText ?? string.Empty, @"^guid:\s*([0-9a-f]+)", RegexOptions.Multiline);
        return m.Success ? m.Groups[1].Value : null;
    }

    /// <summary>
    /// "ok" when MobileBuild builds the same entry scene the manifest would start with, the scene exists
    /// and the manifest guid matches the scene's .meta; otherwise a reason.
    /// </summary>
    public static string Verdict(string manifestYaml, string mobileBuildSource, System.Func<string, string> readFileOrNull)
    {
        string buildScene = MobileBuildScenePath(mobileBuildSource);
        if (buildScene == null) return "MobileBuild.ScenePath not found";
        Entry entry = EntryScene(ParseManifest(manifestYaml));
        if (entry == null) return "manifest has no enabled scene";
        if (entry.Path != buildScene) return $"entry mismatch: manifest={entry.Path} MobileBuild={buildScene}";
        if (readFileOrNull(buildScene) == null) return $"scene file missing: {buildScene}";
        string guid = MetaGuid(readFileOrNull(buildScene + ".meta"));
        if (guid != entry.Guid) return $"guid mismatch: manifest={entry.Guid} meta={guid}";
        return "ok";
    }

    public static string RepoRoot([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here), "..", ".."));

    public static string ReadRepo(string relative)
    {
        string p = Path.Combine(RepoRoot(), relative);
        return File.Exists(p) ? File.ReadAllText(p) : null;
    }
}
