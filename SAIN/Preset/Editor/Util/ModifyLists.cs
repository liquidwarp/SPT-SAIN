using System.Collections.Generic;
using EFT;
using EFT.UI;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.Preset.Shared.Enums;
using UnityEngine;
using static SAIN.Editor.SAINLayout;

namespace SAIN.Editor.Util;

public static class ModifyLists
{
    public static void AddOrRemove(List<WildSpawnType> list, out bool wasEdited, int optionsPerLine = 4)
    {
        wasEdited = false;
        if (list != null)
        {
            int i = StartListEdit(optionsPerLine, out var options);
            foreach (var botType in BotTypeDefinitions.BotTypes.Values)
            {
                AddOrRemove(botType.WildSpawnType, list, out bool newEdit, botType.Name, botType.Description, options);
                if (newEdit)
                {
                    wasEdited = true;
                }

                i = ListSpacing(i, optionsPerLine);
            }
            EndListEdit();
        }
    }

    private static void AddOrRemove<T>(T item, List<T> list, bool value, out bool wasEdited)
    {
        wasEdited = false;
        if (value)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
            }
        }
        else
        {
            if (list.Contains(item))
            {
                list.Remove(item);
            }
        }
    }

    public static void AddOrRemove(
        List<BotDifficulty> list,
        out bool wasEdited,
        int optionsPerLine = 4,
        float width = 1200f,
        float height = 20f
    )
    {
        wasEdited = false;
        float optionWidth = (width / optionsPerLine).Round10();
        var dimensions = new GUILayoutOption[] { Height(height), Width(optionWidth) };
        foreach (var dificulty in EnumValues.Difficulties)
        {
            AddOrRemove(dificulty, list, out bool newEdit, null, null, dimensions);
            if (newEdit)
            {
                wasEdited = true;
            }
        }
    }

    public static void AddOrRemove(List<BotType> list, out bool wasEdited, int optionsPerLine = 5)
    {
        wasEdited = false;
        int i = StartListEdit(optionsPerLine, out var options);
        List<BotType> botList = BotTypeDefinitions.BotTypesList;
        for (int b = 0; b < botList.Count; b++)
        {
            BotType bot = botList[b];
            AddOrRemove(bot, list, out bool newEdit, bot.Name, bot.Description, options);
            if (newEdit)
            {
                wasEdited = true;
            }

            i = ListSpacing(i, optionsPerLine);
        }
        EndListEdit();
    }

    public static void AddOrRemove(List<EBrain> list, out bool wasEdited, int optionsPerLine = 5)
    {
        wasEdited = false;
        int i = StartListEdit(optionsPerLine, out var options);
        EBrain[] allBots = AIBrains.AllBrains;
        for (int b = 0; b < allBots.Length; b++)
        {
            EBrain brain = allBots[b];
            AddOrRemove(brain, list, out bool newEdit, null, null, options);
            if (newEdit)
            {
                wasEdited = true;
            }

            i = ListSpacing(i, optionsPerLine);
        }
        EndListEdit();
    }

    public static void AddOrRemove(List<BotType> list, string section, float height, float width)
    {
        foreach (var botType in BotTypeDefinitions.BotTypes.Values)
        {
            if (botType.Section == section)
            {
                AddOrRemove(botType, list, out bool newEdit, botType.Name, botType.Description, Height(height), Width(width));
            }
        }
    }

    private static void AddOrRemove<T>(
        T value,
        List<T> list,
        out bool wasEdited,
        string name = null,
        string description = null,
        params GUILayoutOption[] options
    )
    {
        wasEdited = false;
        if (list != null)
        {
            bool toggleValue = Toggle(
                list.Contains(value),
                new GUIContent(name ?? value.ToString(), description),
                GetStyle(Style.selectionList),
                EUISoundType.MenuCheckBox,
                options
            );

            AddOrRemove(value, list, toggleValue, out bool newEdit);
            if (newEdit)
            {
                wasEdited = true;
            }
        }
    }

    private static int StartListEdit(int optionsPerLine, out GUILayoutOption[] dimensions, float gridWidth = 1875f)
    {
        BeginVertical();
        BeginHorizontal();
        Space(5);
        float width = (gridWidth / optionsPerLine).Round10();
        dimensions = [Height(PresetHandler.EditorDefaults.ConfigEntryHeight), Width(width)];
        return 0;
    }

    private static int ListSpacing(int i, int max)
    {
        i++;
        if (i >= max)
        {
            i = 0;
            Space(5);
            EndHorizontal();
            BeginHorizontal();
        }
        return i;
    }

    private static void EndListEdit()
    {
        EndHorizontal();
        EndVertical();
    }
}
