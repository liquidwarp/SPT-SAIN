using System.Reflection;
using System.Text.Json.Serialization;
using SAIN.Preset.Shared.Attributes;

namespace SAINServerMod.Utils;

public static class SettingsCopier
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;

    public static bool ShouldCopyDefault(FieldInfo field)
    {
        return field.GetCustomAttribute<CopyValueAttribute>() != null;
    }

    public static void CopyValues(object source, object target, Func<FieldInfo, bool>? shouldCopyField = null)
    {
        foreach (FieldInfo targetCategory in target.GetType().GetFields(Flags))
        {
            if (!TryGetMemberValue(source, targetCategory.Name, out object? sourceCategory) || sourceCategory == null)
            {
                continue;
            }

            object? targetCategoryObj = targetCategory.GetValue(target);
            if (targetCategoryObj == null)
            {
                continue;
            }

            foreach (FieldInfo targetVar in targetCategory.FieldType.GetFields(Flags))
            {
                if (shouldCopyField != null && !shouldCopyField(targetVar))
                {
                    continue;
                }
                if (!TryGetMemberValue(sourceCategory, targetVar.Name, out object? value) || value == null)
                {
                    // Not present, or an unset nullable on the server: keep SAIN's default.
                    continue;
                }
                targetVar.SetValue(targetCategoryObj, value);
            }
        }
    }

    private static bool TryGetMemberValue(object obj, string name, out object? value)
    {
        Type type = obj.GetType();

        FieldInfo? field = type.GetField(name, Flags);
        if (field != null)
        {
            value = field.GetValue(obj);
            return true;
        }

        PropertyInfo? property = FindProperty(type, name);
        if (property != null && property.CanRead)
        {
            value = property.GetValue(obj);
            return true;
        }

        value = null;
        return false;
    }

    // Server models use PascalCase properties and keep the game's name, which SAIN's fields use, in [JsonPropertyName].
    private static PropertyInfo? FindProperty(Type type, string name)
    {
        foreach (PropertyInfo property in type.GetProperties(Flags))
        {
            if (property.Name == name || property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == name)
            {
                return property;
            }
        }
        return null;
    }
}
