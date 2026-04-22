using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LabelStudio.Models;

namespace LabelStudio.Services;

public static class ProjectService
{
    private static readonly string SaveDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LabelStudio", "projects");

    private static readonly JsonSerializerOptions JsonOpts =
        new() { WriteIndented = true };

    public static void Save(LabelProject project)
    {
        Directory.CreateDirectory(SaveDir);
        var path = Path.Combine(SaveDir, project.Id + ".json");
        File.WriteAllText(path, JsonSerializer.Serialize(project, JsonOpts));
    }

    public static List<LabelProject> LoadAll()
    {
        if (!Directory.Exists(SaveDir)) return [];
        var result = new List<LabelProject>();
        foreach (var file in Directory.GetFiles(SaveDir, "*.json"))
        {
            try
            {
                var text = File.ReadAllText(file);
                var proj = JsonSerializer.Deserialize<LabelProject>(text);
                if (proj is not null) result.Add(proj);
            }
            catch { /* skip corrupted files */ }
        }
        result.Sort((a, b) => b.ModifiedAt.CompareTo(a.ModifiedAt));
        return result;
    }

    public static void Delete(string id)
    {
        var path = Path.Combine(SaveDir, id + ".json");
        if (File.Exists(path)) File.Delete(path);
    }
}
