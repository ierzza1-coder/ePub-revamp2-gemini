using ePub.Models;
using System.Collections.Generic;
using System.IO;

public class ContentFileReader
{
    public List<ContentItem> LoadFromFile()
    {


        var filePath = "C:\\source\\ePub-revamp2\\ePub-revamp2\\App_Data";

        var items = new List<ContentItem>();

        foreach (var line in File.ReadLines(filePath))
        {
            var parts = line.Split('|');
            if (parts.Length < 3) continue;

            items.Add(new ContentItem
            {
                Id = parts[0],
                Title = parts[1],
                Body = parts[2],
                Embedding = new float[0] // Placeholder
            });
        }

        return items;
    }
}
