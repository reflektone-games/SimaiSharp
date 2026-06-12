using System.Text;
using SimaiSharp.FileReading;

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

        Assert.That(chart.noteFrames, Is.Empty);
    }

    [Test]
    public void CanReadSingularLocation()
    {
        var target = Make("1");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteFrames,                      Has.Count.EqualTo(1));
        Assert.That(chart.noteFrames[0].Notes,             Has.Count.EqualTo(1));
        Assert.That(chart.noteFrames[0].Notes[0].location, Is.Zero);
    }


    [Test]
    public void CanReadLocationsWithSeparators()
    {
        var target = Make("1/2/3/4/5/6/7/8");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteFrames,          Has.Count.EqualTo(1));
        Assert.That(chart.noteFrames[0].Notes, Has.Count.EqualTo(8));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(chart.noteFrames[0].Notes[0].location, Is.Zero);
            Assert.That(chart.noteFrames[0].Notes[1].location, Is.EqualTo(0x01));
            Assert.That(chart.noteFrames[0].Notes[2].location, Is.EqualTo(0x02));
            Assert.That(chart.noteFrames[0].Notes[3].location, Is.EqualTo(0x03));
            Assert.That(chart.noteFrames[0].Notes[4].location, Is.EqualTo(0x04));
            Assert.That(chart.noteFrames[0].Notes[5].location, Is.EqualTo(0x05));
            Assert.That(chart.noteFrames[0].Notes[6].location, Is.EqualTo(0x06));
            Assert.That(chart.noteFrames[0].Notes[7].location, Is.EqualTo(0x07));
        }
    }

    [Test]
    public void CanReadSplitFrame()
    {
        var target = Make("(60)12,34`78");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteFrames, Has.Count.EqualTo(3));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(chart.noteFrames[0].time,  Is.Zero);
            Assert.That(chart.noteFrames[0].Notes, Has.Count.EqualTo(2));

            Assert.That(chart.noteFrames[1].time,  Is.EqualTo(1));
            Assert.That(chart.noteFrames[1].Notes, Has.Count.EqualTo(2));

            Assert.That(chart.noteFrames[2].time,  Is.EqualTo(1));
            Assert.That(chart.noteFrames[2].Notes, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public void CanReadLocationsWithoutSeparators()
    {
        var target = Make("12345678");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteFrames,          Has.Count.EqualTo(1));
        Assert.That(chart.noteFrames[0].Notes, Has.Count.EqualTo(8));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(chart.noteFrames[0].Notes[0].location, Is.Zero);
            Assert.That(chart.noteFrames[0].Notes[1].location, Is.EqualTo(0x01));
            Assert.That(chart.noteFrames[0].Notes[2].location, Is.EqualTo(0x02));
            Assert.That(chart.noteFrames[0].Notes[3].location, Is.EqualTo(0x03));
            Assert.That(chart.noteFrames[0].Notes[4].location, Is.EqualTo(0x04));
            Assert.That(chart.noteFrames[0].Notes[5].location, Is.EqualTo(0x05));
            Assert.That(chart.noteFrames[0].Notes[6].location, Is.EqualTo(0x06));
            Assert.That(chart.noteFrames[0].Notes[7].location, Is.EqualTo(0x07));
        }
    }

    [Test]
    public void CanReadTempoWithDefaultSubdivisions()
    {
        var target = Make("(60)1,1");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteFrames,         Has.Count.EqualTo(2));
        Assert.That(chart.noteFrames[1].time, Is.EqualTo(1));
    }

    [Test]
    public void CanReadTempoChangesWithDefaultSubdivisions()
    {
        var target = Make("(60)1,(120)1,(60)1");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteFrames, Has.Count.EqualTo(3));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(chart.noteFrames[0].time, Is.Zero);
            Assert.That(chart.noteFrames[1].time, Is.EqualTo(1));
            Assert.That(chart.noteFrames[2].time, Is.EqualTo(1.5f));
        }
    }

    [Test]
    public void CanReadSlideDurationWithDefaultDelayAndDecimalDuration()
    {
        var target = Make("(60)1-5[##2]");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteFrames,               Has.Count.EqualTo(1));
        Assert.That(chart.noteFrames[0].SlidePaths, Has.Count.EqualTo(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(chart.noteFrames[0].SlidePaths[0].delay,    Is.EqualTo(1));
            Assert.That(chart.noteFrames[0].SlidePaths[0].duration, Is.EqualTo(2));
        }
    }

    [Test]
    public void CanCreateMineSlide()
    {
        var target = Make("(60)1-5[1:1]m");
        var chart  = SimaiConvert.Deserialize(target);

        Assert.That(chart.noteFrames,               Has.Count.EqualTo(1));
        Assert.That(chart.noteFrames[0].SlidePaths, Has.Count.EqualTo(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(chart.noteFrames[0].SlidePaths[0].delay,    Is.EqualTo(1));
            Assert.That(chart.noteFrames[0].SlidePaths[0].duration, Is.EqualTo(4));
            Assert.That(chart.noteFrames[0].SlidePaths[0].isMine,   Is.True);
        }
    }

    [Test]
    public void CanSerialize()
    {
        const string maidataFilePath = @"./Resources/SimaiFileTests/0.txt";
        const string chartKey        = @"inote_3";

        using var simaiFile = SimaiFileReader.FromPath(maidataFilePath);

        if (!simaiFile.TryGetValueSpan(chartKey, out var chartSpan))
            return;

        var chart = SimaiConvert.Deserialize(chartSpan);

        using var buffer = new MemoryStream();

        using var writer = new StreamWriter(buffer, Encoding.UTF8);
        SimaiConvert.Serialize(chart, writer);

        using var reader = new StreamReader(buffer, Encoding.UTF8, false);
        buffer.Position = 0;
        var result = reader.ReadToEnd();
        Console.WriteLine(result);
        Assert.That(result, Is.Not.Empty);
    }
}
