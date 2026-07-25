using MusicTeacher.Shared.MusicTheory;

namespace MusicTeacher.Shared.Practice;

/// <summary>
/// A written phrase whose first learning stage assesses pitch order only.
/// Duration and tempo are deliberately absent until those skills are introduced.
/// </summary>
public sealed record StaffPhrase
{
    public StaffPhrase(IReadOnlyList<Pitch> pitches)
    {
        ArgumentNullException.ThrowIfNull(pitches);
        if (pitches.Count < 2)
        {
            throw new ArgumentException("A staff phrase needs at least two pitches.", nameof(pitches));
        }

        Pitches = pitches.ToArray();
    }

    public IReadOnlyList<Pitch> Pitches { get; }

    public PitchSequenceEvaluator CreatePitchEvaluator()
        => new(Pitches.Select(pitch => pitch.MidiNote).ToArray());
}
