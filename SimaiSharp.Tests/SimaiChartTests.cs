using SimaiSharp.Structures;

namespace SimaiSharp.Tests;

[TestFixture]
public class SimaiChartTests
{
    [Test]
    public void CanReadEmptyChart()
    {
        const string maidataFilePath = @"./Resources/SimaiChartTests/CanReadEmptyChart.txt";
        const string chartKey        = @"inote_1";

        using var simaiFile = new SimaiFile(maidataFilePath);
        if (simaiFile.TryGetValueSpan(chartKey, out var chartSpan))
        {
            var chart = SimaiConvert.Deserialize(chartSpan);
            Assert.That(chart.noteFrames, Is.Empty);
        }
    }

    [Test]
    public void CanReadSingularLocation()
    {
        const string maidataFilePath = @"./Resources/SimaiChartTests/CanReadSingularLocation.txt";
        const string chartKey        = @"inote_1";

        using var simaiFile = new SimaiFile(maidataFilePath);
        if (simaiFile.TryGetValueSpan(chartKey, out var chartSpan))
        {
            var chart = SimaiConvert.Deserialize(chartSpan);
            Assert.That(chart.noteFrames,          Has.Count.EqualTo(1));
            Assert.That(chart.noteFrames[0].Notes, Has.Count.EqualTo(1));
            Assert.That(chart.noteFrames[0].Notes[0].location is 0x00);
        }
    }

    [Test]
    public void CanReadLocationsWithSeparators()
    {
        const string maidataFilePath = @"./Resources/SimaiChartTests/CanReadLocationsWithSeparators.txt";
        const string chartKey        = @"inote_1";

        using var simaiFile = new SimaiFile(maidataFilePath);
        if (simaiFile.TryGetValueSpan(chartKey, out var chartSpan))
        {
            var chart = SimaiConvert.Deserialize(chartSpan);
            Assert.That(chart.noteFrames,          Has.Count.EqualTo(1));
            Assert.That(chart.noteFrames[0].Notes, Has.Count.EqualTo(8));
            Assert.Multiple(() =>
            {
                Assert.That(chart.noteFrames[0].Notes[0].location, Is.EqualTo(0x00));
                Assert.That(chart.noteFrames[0].Notes[1].location, Is.EqualTo(0x01));
                Assert.That(chart.noteFrames[0].Notes[2].location, Is.EqualTo(0x02));
                Assert.That(chart.noteFrames[0].Notes[3].location, Is.EqualTo(0x03));
                Assert.That(chart.noteFrames[0].Notes[4].location, Is.EqualTo(0x04));
                Assert.That(chart.noteFrames[0].Notes[5].location, Is.EqualTo(0x05));
                Assert.That(chart.noteFrames[0].Notes[6].location, Is.EqualTo(0x06));
                Assert.That(chart.noteFrames[0].Notes[7].location, Is.EqualTo(0x07));
            });
        }
    }

    [Test]
    public void CanReadLocationsWithoutSeparators()
    {
        const string maidataFilePath = @"./Resources/SimaiChartTests/CanReadLocationsWithoutSeparators.txt";
        const string chartKey        = @"inote_1";

        using var simaiFile = new SimaiFile(maidataFilePath);

        if (simaiFile.TryGetValueSpan(chartKey, out var chartSpan))
        {
            var chart = SimaiConvert.Deserialize(chartSpan);

            Assert.That(chart.noteFrames,          Has.Count.EqualTo(1));
            Assert.That(chart.noteFrames[0].Notes, Has.Count.EqualTo(8));
            Assert.Multiple(() =>
            {
                Assert.That(chart.noteFrames[0].Notes[0].location, Is.EqualTo(0x00));
                Assert.That(chart.noteFrames[0].Notes[1].location, Is.EqualTo(0x01));
                Assert.That(chart.noteFrames[0].Notes[2].location, Is.EqualTo(0x02));
                Assert.That(chart.noteFrames[0].Notes[3].location, Is.EqualTo(0x03));
                Assert.That(chart.noteFrames[0].Notes[4].location, Is.EqualTo(0x04));
                Assert.That(chart.noteFrames[0].Notes[5].location, Is.EqualTo(0x05));
                Assert.That(chart.noteFrames[0].Notes[6].location, Is.EqualTo(0x06));
                Assert.That(chart.noteFrames[0].Notes[7].location, Is.EqualTo(0x07));
            });
        }
    }

    [Test]
    public void CanReadTempoWithDefaultSubdivisions()
    {
        const string maidataFilePath = @"./Resources/SimaiChartTests/CanReadTempoWithDefaultSubdivisions.txt";
        const string chartKey        = @"inote_1";

        using var simaiFile = new SimaiFile(maidataFilePath);
        if (simaiFile.TryGetValueSpan(chartKey, out var chartSpan))
        {
            var chart = SimaiConvert.Deserialize(chartSpan);

            Assert.That(chart.noteFrames,         Has.Count.EqualTo(2));
            Assert.That(chart.noteFrames[1].time, Is.EqualTo(1));
        }
    }

    [Test]
    public void CanReadTempoChangesWithDefaultSubdivisions()
    {
        const string maidataFilePath = @"./Resources/SimaiChartTests/CanReadTempoChangesWithDefaultSubdivisions.txt";
        const string chartKey        = @"inote_1";

        using var simaiFile = new SimaiFile(maidataFilePath);
        if (simaiFile.TryGetValueSpan(chartKey, out var chartSpan))
        {
            var chart = SimaiConvert.Deserialize(chartSpan);

            Assert.That(chart.noteFrames, Has.Count.EqualTo(3));
            Assert.Multiple(() =>
            {
                Assert.That(chart.noteFrames[1].time, Is.EqualTo(1));
                Assert.That(chart.noteFrames[2].time, Is.EqualTo(1.5f));
            });
        }
    }

    [Test]
    public void CanSerialize()
    {
        const string maidataFilePath = @"./Resources/SimaiFileTests/0.txt";
        const string chartKey        = @"inote_3";

        using var simaiFile = new SimaiFile(maidataFilePath);
        if (simaiFile.TryGetValueSpan(chartKey, out var chartSpan))
        {
            var chart = SimaiConvert.Deserialize(chartSpan);

            var serialized = SimaiConvert.Serialize(chart);
            Console.WriteLine(serialized);
            Assert.That(serialized, Is.Not.Empty);
        }
    }
}
