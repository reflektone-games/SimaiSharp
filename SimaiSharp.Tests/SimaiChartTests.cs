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

		var simaiFile = new SimaiFile(maidataFilePath);
		var chart     = SimaiConvert.Deserialize(simaiFile.GetValue(chartKey));

		Assert.That(chart.NoteCollections, Is.Empty);
	}

	[Test]
	public void CanReadSingularLocation()
	{
		const string maidataFilePath = @"./Resources/SimaiChartTests/CanReadSingularLocation.txt";
		const string chartKey        = @"inote_1";

		var simaiFile = new SimaiFile(maidataFilePath);
		var chart     = SimaiConvert.Deserialize(simaiFile.GetValue(chartKey));

		Assert.That(chart.NoteCollections, Has.Length.EqualTo(1));
		Assert.That(chart.NoteCollections[0], Has.Count.EqualTo(1));
		Assert.That(chart.NoteCollections[0][0].location is { group: NoteGroup.Tap, index: 0 });
	}
	
	[Test]
	public void CanReadLocationsWithSeparators()
    {
        const string maidataFilePath = @"./Resources/SimaiChartTests/CanReadLocationsWithSeparators.txt";
		const string chartKey        = @"inote_1";

		var simaiFile = new SimaiFile(maidataFilePath);
		var chart     = SimaiConvert.Deserialize(simaiFile.GetValue(chartKey));

		Assert.That(chart.NoteCollections, Has.Length.EqualTo(1));
		Assert.That(chart.NoteCollections[0], Has.Count.EqualTo(8));
        Assert.Multiple(() =>
        {
            Assert.That(chart.NoteCollections[0][0].location is { group: NoteGroup.Tap, index: 0 });
            Assert.That(chart.NoteCollections[0][1].location is { group: NoteGroup.Tap, index: 1 });
            Assert.That(chart.NoteCollections[0][2].location is { group: NoteGroup.Tap, index: 2 });
            Assert.That(chart.NoteCollections[0][3].location is { group: NoteGroup.Tap, index: 3 });
            Assert.That(chart.NoteCollections[0][4].location is { group: NoteGroup.Tap, index: 4 });
            Assert.That(chart.NoteCollections[0][5].location is { group: NoteGroup.Tap, index: 5 });
            Assert.That(chart.NoteCollections[0][6].location is { group: NoteGroup.Tap, index: 6 });
            Assert.That(chart.NoteCollections[0][7].location is { group: NoteGroup.Tap, index: 7 });
        });
    }
	
	[Test]
	public void CanReadLocationsWithoutSeparators()
	{
		const string maidataFilePath = @"./Resources/SimaiChartTests/CanReadLocationsWithoutSeparators.txt";
		const string chartKey        = @"inote_1";

		var simaiFile = new SimaiFile(maidataFilePath);
		var chart     = SimaiConvert.Deserialize(simaiFile.GetValue(chartKey));

		Assert.That(chart.NoteCollections, Has.Length.EqualTo(1));
		Assert.That(chart.NoteCollections[0], Has.Count.EqualTo(8));
		Assert.Multiple(() =>
		                {
			                Assert.That(chart.NoteCollections[0][0].location is { group: NoteGroup.Tap, index: 0 });
			                Assert.That(chart.NoteCollections[0][1].location is { group: NoteGroup.Tap, index: 1 });
			                Assert.That(chart.NoteCollections[0][2].location is { group: NoteGroup.Tap, index: 2 });
			                Assert.That(chart.NoteCollections[0][3].location is { group: NoteGroup.Tap, index: 3 });
			                Assert.That(chart.NoteCollections[0][4].location is { group: NoteGroup.Tap, index: 4 });
			                Assert.That(chart.NoteCollections[0][5].location is { group: NoteGroup.Tap, index: 5 });
			                Assert.That(chart.NoteCollections[0][6].location is { group: NoteGroup.Tap, index: 6 });
			                Assert.That(chart.NoteCollections[0][7].location is { group: NoteGroup.Tap, index: 7 });
		                });
	}

    [Test]
	public void CanReadTempoWithDefaultSubdivisions()
	{
		const string maidataFilePath = @"./Resources/SimaiChartTests/CanReadTempoWithDefaultSubdivisions.txt";
		const string chartKey        = @"inote_1";

		var simaiFile = new SimaiFile(maidataFilePath);
		var chart     = SimaiConvert.Deserialize(simaiFile.GetValue(chartKey));

		Assert.That(chart.NoteCollections, Has.Length.EqualTo(2));
		Assert.That(chart.NoteCollections[1].time, Is.EqualTo(1));
	}
	
	[Test]
	public void CanReadTempoChangesWithDefaultSubdivisions()
    {
        const string maidataFilePath = @"./Resources/SimaiChartTests/CanReadTempoChangesWithDefaultSubdivisions.txt";
		const string chartKey        = @"inote_1";

		var simaiFile = new SimaiFile(maidataFilePath);
		var chart     = SimaiConvert.Deserialize(simaiFile.GetValue(chartKey));

		Assert.That(chart.NoteCollections, Has.Length.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(chart.NoteCollections[1].time, Is.EqualTo(1));
            Assert.That(chart.NoteCollections[2].time, Is.EqualTo(1.5f));
        });
    }
}