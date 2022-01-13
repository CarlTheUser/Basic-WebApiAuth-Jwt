using System.Text;

namespace Misc.Utilities
{
    public interface IRandomStringGenerator
    {
        string Generate(int length);
    }

    //https://stackoverflow.com/questions/730268/unique-random-string-generation
    public class RandomStringGenerator : IRandomStringGenerator
    {
        private char[] _pool;

        public string Pool
        {
            get => new(_pool);
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"Value of parameter {nameof(value)} is null or whitespace.", nameof(value));
                _pool = new HashSet<char>(value).ToArray();
            }
        }

        public RandomStringGenerator(string pool)
        {
            if (string.IsNullOrWhiteSpace(pool)) throw new ArgumentException($"Value of parameter {nameof(pool)} is null or whitespace.", nameof(pool));
            _pool = new HashSet<char>(pool).ToArray();
        }

        public string Generate(int length)
        {
            const int byteSize = 0x100;

            // Guid.NewGuid and System.Random are not particularly random. By using a
            // cryptographically-secure random number generator, the caller is always
            // protected, regardless of use.
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            
            StringBuilder bobTheBuilder = new();

            var buf = new byte[128];

            char[] pool = _pool;

            while (bobTheBuilder.Length < length)
            {
                rng.GetBytes(buf);
                for (int i = 0; i < buf.Length && bobTheBuilder.Length < length; ++i)
                {
                    // Divide the byte into allowedCharSet-sized groups. If the
                    // random value falls into the last group and the last group is
                    // too small to choose from the entire allowedCharSet, ignore
                    // the value in order to avoid biasing the result.
                    int outOfRangeStart = byteSize - (byteSize % pool.Length);
                    if (outOfRangeStart <= buf[i]) continue;
                    bobTheBuilder.Append(pool[buf[i] % pool.Length]);
                }
            }
            return bobTheBuilder.ToString();
        }
    }

    public class AbcRandomStringGenerator : RandomStringGenerator
    {

        public AbcRandomStringGenerator() : base ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
        {
        }

    }
}