namespace SystemDesign.Tests;

public class SanityTests
{
    [Fact]
    public void Pipeline_Runs_Tests()
    {
        // اختبار بسيط عشان نتأكد إن الـ Pipeline بيشغّل الاختبارات
        Assert.Equal(4, 2 + 2);
    }
}