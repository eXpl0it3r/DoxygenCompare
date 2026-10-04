using DoxygenCompare.Api;

namespace DoxygenCompare.Comparison;

public static class ApiComparer
{
    public static ComparisonResult Compare(ApiSurface oldApi, ApiSurface newApi, string oldVersion = "old", string newVersion = "new")
    {
        var oldEntities = oldApi.Entities;
        var newEntities = newApi.Entities;

        var removed = oldEntities.Keys.Where(k => !newEntities.ContainsKey(k)).ToHashSet(StringComparer.Ordinal);
        var added = newEntities.Keys.Where(k => !oldEntities.ContainsKey(k)).ToHashSet(StringComparer.Ordinal);
        var changes = new List<ApiChange>();

        // A function whose only overload changed its parameters shows up as one removed and one added key,
        // pair them up, so the output shows the signature change instead
        foreach (var (oldKey, newKey) in PairSignatureChanges(oldEntities, newEntities, removed, added))
        {
            var oldEntity = oldEntities[oldKey];
            var newEntity = newEntities[newKey];
            removed.Remove(oldKey);
            added.Remove(newKey);

            changes.Add(new ApiChange(ApiChangeKind.SignatureChanged,
                                      newEntity.Kind,
                                      newEntity.Scope,
                                      newEntity.Name,
                                      oldEntity.Signature,
                                      newEntity.Signature,
                                      // Default arguments are part of the shown signatures and positional, so their diff is just noise
                                      DiffProperties(oldEntity, newEntity).Where(p => p.Property != ApiProperties.DefaultArguments).ToList(),
                                      newEntity.Location));
        }

        // Members of added or removed scopes are implied, listing them would just repeat the whole class
        changes.AddRange(added.Where(k => !HasAncestorIn(newEntities, k, added))
                              .Select(k => newEntities[k])
                              .Select(e => new ApiChange(ApiChangeKind.Added, e.Kind, e.Scope, e.Name, null, e.Signature, [], e.Location)));

        changes.AddRange(removed.Where(k => !HasAncestorIn(oldEntities, k, removed))
                                .Select(k => oldEntities[k])
                                .Select(e => new ApiChange(ApiChangeKind.Removed, e.Kind, e.Scope, e.Name, e.Signature, null, [], e.Location)));

        foreach (var (key, oldEntity) in oldEntities)
        {
            if (!newEntities.TryGetValue(key, out var newEntity))
            {
                continue;
            }

            var properties = DiffProperties(oldEntity, newEntity);

            if (properties.Count > 0 || oldEntity.Kind != newEntity.Kind)
            {
                changes.Add(new ApiChange(ApiChangeKind.Changed,
                                          newEntity.Kind,
                                          newEntity.Scope,
                                          newEntity.Name,
                                          oldEntity.Signature,
                                          newEntity.Signature,
                                          properties,
                                          newEntity.Location));
            }
        }

        var ordered = changes.OrderBy(c => c.Scope, StringComparer.Ordinal)
                             .ThenBy(c => c.Change)
                             .ThenBy(c => c.Kind)
                             .ThenBy(c => c.Name, StringComparer.Ordinal)
                             .ThenBy(c => c.Signature, StringComparer.Ordinal)
                             .ToList();

        return new ComparisonResult(oldVersion, newVersion, ordered);
    }

    private static IEnumerable<(string OldKey, string NewKey)> PairSignatureChanges(
        IReadOnlyDictionary<string, ApiEntity> oldEntities,
        IReadOnlyDictionary<string, ApiEntity> newEntities,
        IReadOnlySet<string> removed,
        IReadOnlySet<string> added)
    {
        static bool IsCallable(ApiEntity entity) => entity.Kind is ApiKind.Function or ApiKind.Friend;

        var removedByName = removed.Select(k => oldEntities[k])
                                   .Where(IsCallable)
                                   .GroupBy(e => (e.ParentKey, e.Name, e.Kind))
                                   .ToDictionary(g => g.Key, g => g.ToList());
        var addedByName = added.Select(k => newEntities[k])
                               .Where(IsCallable)
                               .GroupBy(e => (e.ParentKey, e.Name, e.Kind))
                               .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (name, removedOverloads) in removedByName)
        {
            // With several overloads changing at once there's no reliable way to tell which became which
            if (removedOverloads.Count == 1 && addedByName.TryGetValue(name, out var addedOverloads) && addedOverloads.Count == 1)
            {
                yield return (removedOverloads[0].Key, addedOverloads[0].Key);
            }
        }
    }

    private static bool HasAncestorIn(IReadOnlyDictionary<string, ApiEntity> entities, string key, IReadOnlySet<string> keys)
    {
        var parent = entities[key].ParentKey;

        while (parent.Length > 0)
        {
            if (keys.Contains(parent))
            {
                return true;
            }

            if (!entities.TryGetValue(parent, out var parentEntity))
            {
                return false;
            }

            parent = parentEntity.ParentKey;
        }

        return false;
    }

    private static List<PropertyChange> DiffProperties(ApiEntity oldEntity, ApiEntity newEntity)
    {
        return oldEntity.Properties.Keys
                        .Union(newEntity.Properties.Keys)
                        .Order(StringComparer.Ordinal)
                        .Select(p => new PropertyChange(p,
                                                        oldEntity.Properties.GetValueOrDefault(p),
                                                        newEntity.Properties.GetValueOrDefault(p)))
                        .Where(p => p.OldValue != p.NewValue)
                        .ToList();
    }
}
