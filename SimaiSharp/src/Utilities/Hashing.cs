namespace SimaiSharp.Utilities;

// https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
public struct ChartHasher()
{
    private const ulong FnvOffsetBasis = 14695981039346656037;
    private const ulong FnvPrime       = 1099511628211;

    private ulong _hash = FnvOffsetBasis;

    public void Append(byte b)
    {
        unchecked
        {
            _hash ^= b;
            _hash *= FnvPrime;
        }
    }

    public ulong GetHash() => _hash;
}
