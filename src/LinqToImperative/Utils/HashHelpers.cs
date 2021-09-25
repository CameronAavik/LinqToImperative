namespace LinqToImperative.Utils
{
    internal static class HashHelpers
    {
        public static int CombineHash(int h1, int h2, int h3)
            => CombineHash(CombineHash(h1, h2), h3);

        public static int CombineHash(int h1, int h2, int h3, int h4)
            => CombineHash(CombineHash(CombineHash(h1, h2), h3), h4);

        /// <remarks>
        /// Source from https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Numerics/Hashing/HashHelpers.cs
        /// </remarks>
        public static int CombineHash(int h1, int h2)
        {
            // RyuJIT optimizes this to use the ROL instruction
            // Related GitHub pull request: dotnet/coreclr#1830
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }
}
