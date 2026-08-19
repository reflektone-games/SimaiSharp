using System.Text;
using SimaiSharp.Structures;

namespace SimaiSharp.Tests;

[TestFixture]
public class SimaiChartTests
{
    private static Span<byte> Make(string target) => Encoding.UTF8.GetBytes(target);

    [Test]
    public void CanReadEmptyChart()
    {
        var target = Make("");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteGroups, Is.Empty);
    }

    [Test]
    public void CanReadSingularLocation()
    {
        var target = Make("1");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteGroups,                      Has.Count.EqualTo(1));
        Assert.That(chart.noteGroups[0].notes,             Has.Count.EqualTo(1));
        Assert.That(chart.noteGroups[0].notes[0].location, Is.Zero);
    }


    [Test]
    public void CanReadLocationsWithSeparators()
    {
        var target = Make("1/2/3/4/5/6/7/8");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteGroups[0].notes, Has.Count.EqualTo(8));
        using (Assert.EnterMultipleScope())
        {
            for (var i = 0; i < 8; i++)
            {
                Assert.That(chart.noteGroups[0].notes[i].location, Is.EqualTo(i));
                Assert.That(chart.noteGroups[0].notes[i].time,     Is.Zero);
            }
        }
    }

    [Test]
    public void CanReadLocationsWithoutSeparators()
    {
        var target = Make("12345678");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteGroups[0].notes, Has.Count.EqualTo(8));
        using (Assert.EnterMultipleScope())
        {
            for (var i = 0; i < 8; i++)
            {
                Assert.That(chart.noteGroups[0].notes[i].location, Is.EqualTo(i));
                Assert.That(chart.noteGroups[0].notes[i].time,     Is.Zero);
            }
        }
    }

    [Test]
    public void CanReadTempoWithDefaultSubdivisions()
    {
        var target = Make("(60)1,1");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteGroups[0].notes,         Has.Count.EqualTo(2));
        Assert.That(chart.noteGroups[0].notes[1].time, Is.EqualTo(1));
    }

    [Test]
    public void CanReadSplitFrame()
    {
        var target = Make("(60)12,34`78");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteGroups[0].notes, Has.Count.EqualTo(6));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(chart.noteGroups[0].notes[0].time,     Is.Zero);
            Assert.That(chart.noteGroups[0].notes[0].location, Is.EqualTo(0));

            Assert.That(chart.noteGroups[0].notes[1].time,     Is.Zero);
            Assert.That(chart.noteGroups[0].notes[1].location, Is.EqualTo(1));

            Assert.That(chart.noteGroups[0].notes[2].styles &= NoteStyles.NewGroup, Is.Not.Zero);
            Assert.That(chart.noteGroups[0].notes[2].time,                          Is.EqualTo(1));
            Assert.That(chart.noteGroups[0].notes[2].location,                      Is.EqualTo(2));

            Assert.That(chart.noteGroups[0].notes[3].time,     Is.EqualTo(1));
            Assert.That(chart.noteGroups[0].notes[3].location, Is.EqualTo(3));

            Assert.That(chart.noteGroups[0].notes[4].styles &= NoteStyles.NewGroup,    Is.Not.Zero);
            Assert.That(chart.noteGroups[0].notes[4].styles &= NoteStyles.ForceSingle, Is.Not.Zero);
            Assert.That(chart.noteGroups[0].notes[4].time,                             Is.EqualTo(1));
            Assert.That(chart.noteGroups[0].notes[4].location,                         Is.EqualTo(6));

            Assert.That(chart.noteGroups[0].notes[5].styles &= NoteStyles.ForceSingle, Is.Not.Zero);
            Assert.That(chart.noteGroups[0].notes[5].time,                             Is.EqualTo(1));
            Assert.That(chart.noteGroups[0].notes[5].location,                         Is.EqualTo(7));
        }
    }


    [Test]
    public void CanReadTempoChangesWithDefaultSubdivisions()
    {
        var target = Make("(60)1,(120)1,(60)1");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteGroups[0].notes, Has.Count.EqualTo(3));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(chart.noteGroups[0].notes[0].time, Is.Zero);
            Assert.That(chart.noteGroups[0].notes[1].time, Is.EqualTo(1));
            Assert.That(chart.noteGroups[0].notes[2].time, Is.EqualTo(1.5f));
        }
    }

    [Test]
    public void CanReadSlideDurationWithDefaultDelayAndDecimalDuration()
    {
        var target = Make("(60)1-5[##2]");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteGroups[0].notes,      Has.Count.EqualTo(1));
        Assert.That(chart.noteGroups[0].slidePaths, Has.Count.EqualTo(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(chart.noteGroups[0].slidePaths[0].delay,    Is.EqualTo(1));
            Assert.That(chart.noteGroups[0].slidePaths[0].duration, Is.EqualTo(2));
        }
    }

    [Test]
    public void CanCreateMineSlide()
    {
        var target = Make("(60)1-5[1:1]m");
        var chart  = SimaiConvert.Deserialize(target);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(chart.noteGroups[0].slidePaths[0].delay,    Is.EqualTo(1));
            Assert.That(chart.noteGroups[0].slidePaths[0].duration, Is.EqualTo(4));
            Assert.That(chart.noteGroups[0].slidePaths[0].isMine,   Is.True);
        }
    }
}
