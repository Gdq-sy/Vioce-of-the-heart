using System;
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
    public string Bgm;
    public string Audio;
}

public class DialogueData
{
    private readonly List<DialogueRow> rows = new List<DialogueRow>();

    public int Count => rows.Count;
    public bool HasIndex(int index) => index >= 0 && index < Count;

    public DialogueRow GetRow(int index) => rows[index];

    /// <summary>把编号列转成 int，供存档用。</summary>
    public int GetNumberInt(int index) => int.Parse(rows[index].Number);

    /// <summary>
    /// 从 fromIndex（含）开始，向后找第一个“背景图片”列非空的原始行；找不到返回 -1。
    /// 供跳过功能使用（用原始行而非继承合并后的行，否则每行都有背景）。
    /// </summary>
    public int FindNextBackgroundIndex(int fromIndex)
    {
        for (int i = Mathf.Max(0, fromIndex); i < rows.Count; i++)
        {
            if (!string.IsNullOrEmpty(rows[i].Background)) return i;
        }
        return -1;
    }

    /// <summary>
    /// 返回第 index 行，但 Background / Character1 / Character2 三个字段
    /// 如果为空，会向上回溯到最近一个非空值（"#" 算非空，会命中）。
    /// 其余字段（Name / Dialog / Effect）保持原行不变。
    /// </summary>
    public DialogueRow GetRowWithInheritedVisuals(int index)
    {
        if (!HasIndex(index)) return default;

        DialogueRow row = rows[index];
        row.Background = FindLastNonEmpty(index, r => r.Background);
        row.Character1 = FindLastNonEmpty(index, r => r.Character1);
        row.Character2 = FindLastNonEmpty(index, r => r.Character2);
        return row;
    }

    // 从 index 向上找第一个非空值；"#" 视为有效值，命中即停。找不到返回 ""。
    private string FindLastNonEmpty(int index, Func<DialogueRow, string> selector)
    {
        for (int i = index; i >= 0; i--)
        {
            string v = selector(rows[i]);
            if (!string.IsNullOrEmpty(v)) return v;
        }
        return "";
    }

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
            row.Bgm = GetCol(columns, 7);
            row.Audio = GetCol(columns, 8);
            rows.Add(row);
        }
    }

    static string GetCol(string[] columns, int index)
        => index < columns.Length ? columns[index].Trim() : "";

}
