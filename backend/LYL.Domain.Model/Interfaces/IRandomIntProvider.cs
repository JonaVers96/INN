namespace LYL.Domain.Model.Interfaces;

public interface IRandomIntProvider
{
    int NextInt();
    int NextInt(int minInclusive, int maxExclusive);

    List<T> Shuffle<T>(IEnumerable<T> source);


}