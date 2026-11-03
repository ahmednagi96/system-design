namespace SystemDesign.API.Services;

public static class Base62Encoder
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int Base = 62;

    public static string Encode(long number)
    {
        if (number == 0) return Alphabet[0].ToString();

        var result = new Stack<char>();
        while (number > 0)
        {
            result.Push(Alphabet[(int)(number % Base)]);
            number /= Base;
        }

        return new string(result.ToArray());
    }
}