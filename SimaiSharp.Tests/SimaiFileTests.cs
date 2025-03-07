namespace SimaiSharp.Tests
{
    [TestFixture]
    public class SimaiFileTests
    {
        private const string MaidataFilePath = "./Resources/SimaiFileTests/0.txt";

        [Test]
        public void CanReadFromString()
        {
            var kvp       = new Dictionary<int, SimaiFile.MemorySlice>();
            var simaiFile = new SimaiFile(MaidataFilePath);

            Assert.DoesNotThrow(() => { kvp = simaiFile.ParseFile(); });

            Assert.Multiple(() =>
            {
                Assert.That(simaiFile.GetString(kvp[SimaiFile.ComputeHash("title")]).TrimEnd(),   Is.EqualTo("SimaiFileRead test case"));
                Assert.That(simaiFile.GetString(kvp[SimaiFile.ComputeHash("artist")]).TrimEnd(),  Is.EqualTo("非英語文本"));
                Assert.That(simaiFile.GetString(kvp[SimaiFile.ComputeHash("first")]).TrimEnd(),   Is.EqualTo("0"));
                Assert.That(simaiFile.GetString(kvp[SimaiFile.ComputeHash("lv_1")]).TrimEnd(),    Is.EqualTo("12+"));
                Assert.That(simaiFile.GetString(kvp[SimaiFile.ComputeHash("inote_1")]).TrimEnd(), Is.EqualTo("(170){4}A1/2,3h[4:1],E"));
            });
        }

        [Test]
        public void CanReadIndividualKeyValuePair()
        {
            var simaiFile = new SimaiFile(MaidataFilePath);
            Assert.That(simaiFile.GetString(simaiFile["inote_2"]).TrimEnd(),
                        Is.EqualTo("(170){1},{8}6h[8:1]/2,7,,3h[8:1]/7,2,,6h[8:1]/2,5,,E"));
        }

        [Test]
        public void CanReadMultilineValues()
        {
            var simaiFile = new SimaiFile(MaidataFilePath);
            Assert.That(simaiFile.GetString(simaiFile["inote_3"]),
                        Is.EqualTo(@"(170){16}7/2-6[8:1],,1-5[8:1],8,2,,1,,
              {8}2,18,7,16,2/7h[4:1],1,
              3,{16}3,4,{8}2h[8:1]/5h[8:1],,18,,
              {16}2/7-3[8:1],,8-4[8:1],1,7,,8,,
              {8}7,81,2,83,7/2h[4:1],8,
              6,{16}6,5,{8}7h[8:1]/4h[8:1],,7h[16:3]/1,,"));
        }
    }
}
