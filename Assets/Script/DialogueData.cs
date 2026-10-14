using System.Collections.Generic;
using UnityEngine;

public struct DialogueRow
{
    public string Number;
    public string Name;
    public string Dialog;
    public string Background;
    public string Character1;
    public string Character2;
    public string Effect;
}

public class DialogueData
{
    private readonly List<DialogueRow> rows = new List<DialogueRow>();

    public int Count => rows.Count;
    public bool HasIndex(int index) => index >= 0 && index < Count;

    public DialogueRow GetRow(int index) => rows[index];

    /// <summary>把编号列转成 int，供存档用。</summary>
    public int GetNumberInt(int index) => int.Parse(rows[index].Number);

    public void Load(TextAsset csvFile)
    {
        rows.Clear();

        if (csvFile == null)
        {
            Debug.LogError("CSV file not assigned!");
            return;
        }

        string[] lines = csvFile.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] columns = line.Split(',');
            if (columns.Length < 7) continue;

            DialogueRow row;
            row.Number = columns[0].Trim();
            row.Name = columns[1].Trim();
            row.Dialog = columns[2].Trim();
            row.Background = columns[3].Trim();
            row.Character1 = columns[4].Trim();
            row.Character2 = columns[5].Trim();
            row.Effect = columns[6].Trim();

            rows.Add(row);
        }
    }
}