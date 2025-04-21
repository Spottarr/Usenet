using System.Diagnostics.CodeAnalysis;

namespace Usenet.Util
{
    /// <summary>
    /// Utility class for guarding input.
    /// Based on Kristian Hellang's yEnc project https://github.com/khellang/yEnc.
    /// </summary>
    internal static class Guard
    {
        /// <summary>
        /// Throws an <exception cref="ArgumentNullException">ArgumentNullException</exception> if obj is null.
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <param name="name">The name of the object</param>
        /// <exception cref="ArgumentNullException">ArgumentNullException</exception>
        public static void ThrowIfNull([NotNull] object obj, string name)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(obj, name);
#else
            if (obj == null) 
                throw new ArgumentNullException(name, Resources.Util.NullValueNotAllowed);
#endif
        }

        /// <summary>
        /// Throws an <exception cref="ArgumentNullException">ArgumentNullException</exception> if the specified string is null.
        /// Throws an <exception cref="ArgumentException">ArgumentException</exception> if the specified string is empty.
        /// </summary>
        /// <param name="str">The string to check</param>
        /// <param name="name">The name of the string</param>
        /// <exception cref="ArgumentNullException">ArgumentNullException</exception>
        /// <exception cref="ArgumentException">ArgumentException</exception>
        public static void ThrowIfNullOrEmpty([NotNull] string str, string name)
        {
#if NET8_0_OR_GREATER
            ArgumentException.ThrowIfNullOrEmpty(str, name);
#else
            ThrowIfNull(str, name);
            if (str.Length == 0)
                throw new ArgumentException(Resources.Util.EmptyStringNotAllowed, name);
#endif
        }

        /// <summary>
        /// Throws an <exception cref="ArgumentNullException">ArgumentNullException</exception> if the specified string is null.
        /// Throws an <exception cref="ArgumentException">ArgumentException</exception> if the specified string is empty or if it consists only of white-space characters.
        /// </summary>
        /// <param name="str">The string to check</param>
        /// <param name="name">The name of the string</param>
        /// <exception cref="ArgumentNullException">ArgumentNullException</exception>
        /// <exception cref="ArgumentException">ArgumentException</exception>
        public static void ThrowIfNullOrWhiteSpace([NotNull] string str, string name)
        {
#if NET8_0_OR_GREATER
            ArgumentException.ThrowIfNullOrWhiteSpace(str, name);
#else
            ThrowIfNullOrEmpty(str, name);
            if (string.IsNullOrWhiteSpace(str))
                throw new ArgumentException(Resources.Util.OnlyWhiteSpaceCharactersNotAllowed, name);
#endif
        }

        /// <summary>
        /// Throws an <exception cref="ArgumentOutOfRangeException">ArgumentNullException</exception> if the value is negative or 0
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <param name="paramName">The name of the value</param>
        /// <exception cref="ArgumentOutOfRangeException">ArgumentOutOfRangeException</exception>
        public static void ThrowIfNegativeOrZero(long value, string paramName)
        {
#if NET8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, paramName);
#else
            if (value <= 0)
                throw new ArgumentOutOfRangeException(paramName);
#endif
        }
    }
}
