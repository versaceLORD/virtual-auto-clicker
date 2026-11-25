using VirtualAutoClicker.Interop;

namespace VirtualAutoClicker.Tests.Interop;

[TestClass]
public sealed class ClientPointTests
{
    [TestMethod]
    [DataRow(0, 0, 0L)]
    [DataRow(450, 450, (450L << 16) | 450L)]
    [DataRow(1, 0, 1L)]
    [DataRow(0, 1, 0x0001_0000L)]
    [DataRow(0x7FFF, 0x7FFF, 0x7FFF_7FFFL)]
    public void ToLParam_PacksLikeMakeLParam(int x, int y, long expected)
    {
        var lParam = new ClientPoint(x, y).ToLParam();

        Assert.AreEqual(expected, (long)lParam);
    }

    [TestMethod]
    public void ToLParam_XDoesNotBleedIntoY()
    {
        // The 1.x implementation used (y << 16) + x, which corrupts Y for X > 0xFFFF.
        var lParam = new ClientPoint(0x1_0005, 3).ToLParam();

        Assert.AreEqual(3, (int)((lParam >> 16) & 0xFFFF));
        Assert.AreEqual(5, (int)(lParam & 0xFFFF));
    }

    [TestMethod]
    [DataRow("450,450", 450, 450)]
    [DataRow(" 1 , 2 ", 1, 2)]
    [DataRow("0,0", 0, 0)]
    [DataRow("32767,32767", 32767, 32767)]
    public void TryParse_Valid(string text, int x, int y)
    {
        var ok = ClientPoint.TryParse(text, out var error, out var point);

        Assert.IsTrue(ok, error);
        Assert.AreEqual(new ClientPoint(x, y), point);
    }

    [TestMethod]
    [DataRow("450")]
    [DataRow("1,2,3")]
    [DataRow("a,b")]
    [DataRow("1.5,2")]
    [DataRow("-1,5")]
    [DataRow("5,32768")]
    [DataRow("")]
    [DataRow("   ")]
    public void TryParse_Invalid_ReturnsError(string text)
    {
        var ok = ClientPoint.TryParse(text, out var error, out _);

        Assert.IsFalse(ok);
        Assert.IsFalse(string.IsNullOrWhiteSpace(error));
    }

    [TestMethod]
    [DataRow("notepad", "notepad")]
    [DataRow("notepad.exe", "notepad")]
    [DataRow("Notepad.EXE", "Notepad")]
    [DataRow("\"My Game\"", "My Game")]
    [DataRow("  spaced  ", "spaced")]
    public void NormalizeProcessName(string input, string expected) =>
        Assert.AreEqual(expected, ProcessWindowLocator.NormalizeProcessName(input));
}
