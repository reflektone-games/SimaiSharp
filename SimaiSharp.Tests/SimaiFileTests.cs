namespace SimaiSharp.Tests
{
    [TestFixture]
    public class SimaiFileTests
    {
        private const string MaidataFilePath = "./Resources/SimaiFileTests/0.txt";

        [Test]
        public void CanReadEntry_ASCII()
        {
            var simaiFile = SimaiFile.FromMemoryMappedFile(MaidataFilePath);
            Assert.That(simaiFile["title"].TrimEnd(), Is.EqualTo("SimaiFileRead test case"));
        }

        [Test]
        public void CanReadEntry_Unicode()
        {
            var simaiFile = SimaiFile.FromMemoryMappedFile(MaidataFilePath);
            Assert.That(simaiFile["artist"].TrimEnd(), Is.EqualTo("非英語文本"));
        }

        [Test]
        public void CanReadEntry_WithSlash()
        {
            var simaiFile = SimaiFile.FromMemoryMappedFile(MaidataFilePath);
            Assert.That(simaiFile["slash"].TrimEnd(), Is.EqualTo("Test/Entry"));
        }

        [Test]
        public void CanReadEntry_WithAmpersand()
        {
            var simaiFile = SimaiFile.FromMemoryMappedFile(MaidataFilePath);
            Assert.That(simaiFile["and"].TrimEnd(), Is.EqualTo("Test&Entry"));
        }

        [Test]
        public void CanReadEntry_WithEquals()
        {
            var simaiFile = SimaiFile.FromMemoryMappedFile(MaidataFilePath);
            Assert.That(simaiFile["equals"].TrimEnd(), Is.EqualTo("Test=Entry"));
        }

        [Test]
        public void CanReadDictionary()
        {
            var kvp       = new Dictionary<int, SimaiFile.MemorySlice>();
            var simaiFile = SimaiFile.FromMemoryMappedFile(MaidataFilePath);

            Assert.DoesNotThrow(() => { kvp = simaiFile.ParseFile(); });

            Assert.Multiple(() =>
            {
                var span = simaiFile.GetSpan();

                Assert.That(simaiFile.GetString(span, kvp[SimaiFile.ComputeHash("title")]).TrimEnd(),
                            Is.EqualTo("SimaiFileRead test case"));
                Assert.That(simaiFile.GetString(span, kvp[SimaiFile.ComputeHash("artist")]).TrimEnd(), Is.EqualTo("非英語文本"));
                Assert.That(simaiFile.GetString(span, kvp[SimaiFile.ComputeHash("first")]).TrimEnd(),  Is.EqualTo("0"));
                Assert.That(simaiFile.GetString(span, kvp[SimaiFile.ComputeHash("lv_1")]).TrimEnd(),   Is.EqualTo("12+"));
                Assert.That(simaiFile.GetString(span, kvp[SimaiFile.ComputeHash("inote_1")]).TrimEnd(),
                            Is.EqualTo("(170){4}A1/2,3h[4:1],E"));
            });
        }

        [Test]
        public void CanReadIndividualKeyValuePair()
        {
            var simaiFile = SimaiFile.FromMemoryMappedFile(MaidataFilePath);
            Assert.That(simaiFile["inote_2"].TrimEnd(),
                        Is.EqualTo("(170){1},{8}6h[8:1]/2,7,,3h[8:1]/7,2,,6h[8:1]/2,5,,E"));
        }

        [Test]
        public void CanReadMultilineValues()
        {
            var simaiFile = SimaiFile.FromMemoryMappedFile(MaidataFilePath);
            Assert.That(simaiFile["inote_3"],
                        Is.EqualTo(@"(170){16}7/2-6[8:1],,1-5[8:1],8,2,,1,,
              {8}2,18,7,16,2/7h[4:1],1,
              3,{16}3,4,{8}2h[8:1]/5h[8:1],,18,,
              {16}2/7-3[8:1],,8-4[8:1],1,7,,8,,
              {8}7,81,2,83,7/2h[4:1],8,
              6,{16}6,5,{8}7h[8:1]/4h[8:1],,7h[16:3]/1,,
"));
        }
    }
}
