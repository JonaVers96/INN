using System.Security.Cryptography;
using LYL.Domain.Model.Interfaces;
public class SecureRandomIntService : IRandomIntProvider
{
    public int NextInt() => RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);

    public int NextInt(int minInclusive, int maxExclusive)
        => RandomNumberGenerator.GetInt32(minInclusive, maxExclusive);
        public List<T> Shuffle<T>(IEnumerable<T> source)
{
    var list = source.ToList();
    for (int i = list.Count - 1; i > 0; i--)
    {
        int j = NextInt(0, i + 1);
        (list[i], list[j]) = (list[j], list[i]);
    }
    return list;
}
}


