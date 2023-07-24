namespace SimaiSharp.Tests
{
	[TestFixture]
	public class SimaiFileTests
	{
		private const string MaidataFilePath =
			@"./Resources/SimaiFileTests/0.txt";

		private SimaiFile _simaiFile = null!;

		[SetUp]
		public void Setup()
		{
			_simaiFile = new SimaiFile(new FileInfo(MaidataFilePath));
		}

		[Test]
		public void CanReadFromString()
		{
			var kvp = new Dictionary<string, string>();
			Assert.DoesNotThrow(() => kvp = new SimaiFile(File.ReadAllText(MaidataFilePath))
			                                .ToKeyValuePairs()
			                                .ToDictionary(p => p.Key, p => p.Value));
			Assert.Multiple(() =>
			{
				Assert.That(kvp["title"],   Is.EqualTo("SimaiFileRead test case"));
				Assert.That(kvp["artist"],  Is.EqualTo("非英語文本"));
				Assert.That(kvp["first"],   Is.EqualTo("0"));
				Assert.That(kvp["lv_1"],    Is.EqualTo("12+"));
				Assert.That(kvp["inote_1"], Is.EqualTo("(170){4}A1/2,3h[4:1],E"));
			});
		}

		[Test]
		public void CanReadFromFileInfo()
		{
			var kvp = new Dictionary<string, string>();
			Assert.DoesNotThrow(() => kvp = new SimaiFile(new FileInfo(MaidataFilePath))
			                                .ToKeyValuePairs()
			                                .ToDictionary(p => p.Key, p => p.Value));
			Assert.Multiple(() =>
			{
				Assert.That(kvp["title"],   Is.EqualTo("SimaiFileRead test case"));
				Assert.That(kvp["artist"],  Is.EqualTo("非英語文本"));
				Assert.That(kvp["first"],   Is.EqualTo("0"));
				Assert.That(kvp["lv_1"],    Is.EqualTo("12+"));
				Assert.That(kvp["inote_1"], Is.EqualTo("(170){4}A1/2,3h[4:1],E"));
			});
		}

		[Test]
		public void CanReadFromStream()
		{
			using var stream = File.OpenRead(MaidataFilePath);

			var kvp = new Dictionary<string, string>();
			Assert.DoesNotThrow(() => kvp = new SimaiFile(stream)
			                                .ToKeyValuePairs()
			                                .ToDictionary(p => p.Key, p => p.Value));
			Assert.Multiple(() =>
			{
				Assert.That(kvp["title"],   Is.EqualTo("SimaiFileRead test case"));
				Assert.That(kvp["artist"],  Is.EqualTo("非英語文本"));
				Assert.That(kvp["first"],   Is.EqualTo("0"));
				Assert.That(kvp["lv_1"],    Is.EqualTo("12+"));
				Assert.That(kvp["inote_1"], Is.EqualTo("(170){4}A1/2,3h[4:1],E"));
			});
		}

		[Test]
		public void CanReadFromStreamReader()
		{
			using var stream = File.OpenRead(MaidataFilePath);
			using var reader = new StreamReader(stream);

			var kvp = new Dictionary<string, string>();
			Assert.DoesNotThrow(() => kvp = new SimaiFile(reader)
			                                .ToKeyValuePairs()
			                                .ToDictionary(p => p.Key, p => p.Value));
			Assert.Multiple(() =>
			{
				Assert.That(kvp["title"],   Is.EqualTo("SimaiFileRead test case"));
				Assert.That(kvp["artist"],  Is.EqualTo("非英語文本"));
				Assert.That(kvp["first"],   Is.EqualTo("0"));
				Assert.That(kvp["lv_1"],    Is.EqualTo("12+"));
				Assert.That(kvp["inote_1"], Is.EqualTo("(170){4}A1/2,3h[4:1],E"));
			});
		}

		[Test]
		public void CanConvertToKeyValuePairs()
		{
			var kvp = _simaiFile.ToKeyValuePairs()
			                    .ToDictionary(p => p.Key, p => p.Value);
			Assert.Multiple(() =>
			{
				Assert.That(kvp["title"],   Is.EqualTo("SimaiFileRead test case"));
				Assert.That(kvp["artist"],  Is.EqualTo("非英語文本"));
				Assert.That(kvp["first"],   Is.EqualTo("0"));
				Assert.That(kvp["lv_1"],    Is.EqualTo("12+"));
				Assert.That(kvp["inote_1"], Is.EqualTo("(170){4}A1/2,3h[4:1],E"));
			});
		}

		[Test]
		public void CanReadIndividualKeyValuePair()
		{
			var kvp = _simaiFile.GetValue("inote_2");
			Assert.That(kvp, Is.EqualTo("(170){1},{8}6h[8:1]/2,7,,3h[8:1]/7,2,,6h[8:1]/2,5,,E"));
		}

		[Test]
		public void CanReadMultilineValues()
		{
			var kvp = _simaiFile.GetValue("inote_3");
			Assert.That(kvp, Is.EqualTo(@"(170){16}7/2-6[8:1],,1-5[8:1],8,2,,1,,
              {8}2,18,7,16,2/7h[4:1],1,
              3,{16}3,4,{8}2h[8:1]/5h[8:1],,18,,
              {16}2/7-3[8:1],,8-4[8:1],1,7,,8,,
              {8}7,81,2,83,7/2h[4:1],8,
              6,{16}6,5,{8}7h[8:1]/4h[8:1],,7h[16:3]/1,,"));
		}
	}
}
