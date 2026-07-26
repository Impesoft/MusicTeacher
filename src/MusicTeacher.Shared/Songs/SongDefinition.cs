using MusicTeacher.Shared.Progress;

namespace MusicTeacher.Shared.Songs;

public sealed record SongDefinition
{
    public SongDefinition(
        string id,
        string titleKey,
        int tempoBpm,
        IReadOnlyList<LearningSkill> prerequisites,
        IReadOnlyList<SongSection> sections)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A song needs an id.", nameof(id));
        if (string.IsNullOrWhiteSpace(titleKey)) throw new ArgumentException("A song needs a title key.", nameof(titleKey));
        if (tempoBpm is < 40 or > 200) throw new ArgumentOutOfRangeException(nameof(tempoBpm));
        ArgumentNullException.ThrowIfNull(prerequisites);
        ArgumentNullException.ThrowIfNull(sections);
        if (sections.Count == 0 || sections.Any(section => section.Measures.Count == 0))
        {
            throw new ArgumentException("A song needs at least one non-empty section.", nameof(sections));
        }

        Id = id;
        TitleKey = titleKey;
        TempoBpm = tempoBpm;
        Prerequisites = prerequisites.ToArray();
        Sections = sections.ToArray();
    }

    public string Id { get; }
    public string TitleKey { get; }
    public int TempoBpm { get; }
    public IReadOnlyList<LearningSkill> Prerequisites { get; }
    public IReadOnlyList<SongSection> Sections { get; }
    public IReadOnlyList<SongMeasure> Measures => Sections.SelectMany(section => section.Measures).ToArray();
}

public sealed record SongSection(string Id, string TitleKey, IReadOnlyList<SongMeasure> Measures);

public sealed record SongMeasure(string Id, MusicTeacher.Shared.Practice.WrittenMeasure WrittenMeasure);
