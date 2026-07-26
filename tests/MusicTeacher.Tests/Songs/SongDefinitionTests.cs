using MusicTeacher.Shared.MusicTheory;
using MusicTeacher.Shared.Songs;

namespace MusicTeacher.Tests.Songs;

public sealed class SongDefinitionTests
{
    [Fact]
    public void FirstSongContainsTwoSectionsAndFourCompleteFourFourMeasures()
    {
        var song = BeginnerSongLibrary.LevelComplete;

        Assert.Equal(2, song.Sections.Count);
        Assert.Equal(4, song.Measures.Count);
        Assert.All(song.Measures, measure =>
        {
            Assert.Equal(TimeSignature.FourFour, measure.WrittenMeasure.TimeSignature);
            Assert.Equal(4, measure.WrittenMeasure.Notes.Count);
            Assert.All(measure.WrittenMeasure.Notes, note => Assert.Equal(1, note.DurationBeats));
        });
    }
}
