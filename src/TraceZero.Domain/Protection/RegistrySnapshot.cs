namespace TraceZero.Domain.Protection;

/// <summary>
/// Instantané d'une valeur de registre, encodé de façon portable (§17). Le <see cref="Kind"/> reprend
/// la valeur numérique de <c>Microsoft.Win32.RegistryValueKind</c> ; l'encodage garde la couche Domain
/// sans dépendance Windows.
/// </summary>
public sealed record RegistryValueSnapshot
{
    /// <summary>Nom de la valeur (jamais la valeur par défaut vide).</summary>
    public required string Name { get; init; }

    /// <summary>Type Windows (RegistryValueKind) : 1=String, 2=ExpandString, 3=Binary, 4=DWord, 7=MultiString, 11=QWord.</summary>
    public required int Kind { get; init; }

    /// <summary>
    /// Donnée encodée en texte : chaîne telle quelle (String/ExpandString), nombre décimal (DWord/QWord),
    /// base64 (Binary), ou éléments joints par « \0 » puis base64 (MultiString).
    /// </summary>
    public required string EncodedValue { get; init; }
}

/// <summary>
/// Instantané récursif d'une clé de registre : ses valeurs et ses sous-clés (§17). Sert de sauvegarde
/// avant un nettoyage réversible de trace de confidentialité, afin de pouvoir restaurer.
/// </summary>
public sealed record RegistryKeySnapshot
{
    /// <summary>Nom de la sous-clé (vide pour la clé racine capturée).</summary>
    public required string Name { get; init; }

    public required IReadOnlyList<RegistryValueSnapshot> Values { get; init; }

    public required IReadOnlyList<RegistryKeySnapshot> SubKeys { get; init; }

    /// <summary>Nombre total de valeurs et sous-clés capturées (récursif).</summary>
    public int EntryCount => Values.Count + SubKeys.Count + SubKeys.Sum(s => s.EntryCount);

    /// <summary>Vrai si l'instantané ne contient rien à restaurer.</summary>
    public bool IsEmpty => Values.Count == 0 && SubKeys.Count == 0;
}
