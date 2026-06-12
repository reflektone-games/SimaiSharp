using SimaiSharp.FileReading;

namespace SimaiSharp.Tests
{
    [TestFixture]
    public class SimaiFileReaderTests
    {
        private const string MaidataFilePath = "./Resources/SimaiFileTests/0.txt";

        [Test]
        public void CanReadEntry_ASCII()
        {
            var simaiFile = SimaiFileReader.FromPath(MaidataFilePath);
            Assert.That(simaiFile.GetValue("title").TrimEnd(), Is.EqualTo("SimaiFileRead test case"));
        }

        [Test]
        public void CanReadEntry_Unicode()
        {
            var simaiFile = SimaiFileReader.FromPath(MaidataFilePath);
            Assert.That(simaiFile.GetValue("artist").TrimEnd(), Is.EqualTo("非英語文本"));
        }

        [Test]
        public void CanReadEntry_WithSlash()
        {
            var simaiFile = SimaiFileReader.FromPath(MaidataFilePath);
            Assert.That(simaiFile.GetValue("slash").TrimEnd(), Is.EqualTo("Test/Entry"));
        }

        [Test]
        public void CanReadEntry_WithAmpersand()
        {
            var simaiFile = SimaiFileReader.FromPath(MaidataFilePath);
            Assert.That(simaiFile.GetValue("and").TrimEnd(), Is.EqualTo("Test&Entry"));
        }

        [Test]
        public void CanReadEntry_WithEquals()
        {
            var simaiFile = SimaiFileReader.FromPath(MaidataFilePath);
            Assert.That(simaiFile.GetValue("equals").TrimEnd(), Is.EqualTo("Test=Entry"));
        }

        [Test]
        public void CanReadDictionary()
        {
            var simaiFile = SimaiFileReader.FromPath(MaidataFilePath);

            Assert.DoesNotThrow(() => { simaiFile.ScanFile(); });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(simaiFile.GetValue("title").TrimEnd(),  Is.EqualTo("SimaiFileRead test case"));
                Assert.That(simaiFile.GetValue("artist").TrimEnd(), Is.EqualTo("非英語文本"));
                Assert.That(simaiFile.GetValue("first").TrimEnd(),  Is.EqualTo("0"));
                Assert.That(simaiFile.GetValue("lv_1").TrimEnd(),   Is.EqualTo("12+"));
                Assert.That(simaiFile.GetValue("inote_1").TrimEnd(),  Is.EqualTo("(170){4}A1/2,3h[4:1],E"));
            }
        }

        [Test]
        public void CanReadIndividualKeyValuePair()
        {
            var simaiFile = SimaiFileReader.FromPath(MaidataFilePath);
            Assert.That(simaiFile.GetValue("inote_2").TrimEnd(),
                        Is.EqualTo("(170){1},{8}6h[8:1]/2,7,,3h[8:1]/7,2,,6h[8:1]/2,5,,E"));
        }

        [Test]
        public void CanReadMultilineValues()
        {
            var simaiFile = SimaiFileReader.FromPath(MaidataFilePath);
            Assert.That(simaiFile.GetValue("inote_3"),
                        Is.EqualTo("""
                                   (170){16}7/2-6[8:1],,1-5[8:1],8,2,,1,,
                                   {8}2,18,7,16,2/7h[4:1],1,
                                   3,{16}3,4,{8}2h[8:1]/5h[8:1],,18,,
                                   {16}2/7-3[8:1],,8-4[8:1],1,7,,8,,
                                   {8}7,81,2,83,7/2h[4:1],8,
                                   6,{16}6,5,{8}7h[8:1]/4h[8:1],,7h[16:3]/1,,

                                   """));
        }
    }
}
