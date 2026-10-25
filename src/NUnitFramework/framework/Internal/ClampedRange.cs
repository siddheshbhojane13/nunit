// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
#if !NETFRAMEWORK
using System.Numerics;
#else
using System.Runtime.CompilerServices;
#endif

namespace NUnit.Framework.Internal
{
    /// <summary>
    /// ClampedRange represents the typed limits of values that match
    /// a specific tolerance, when applied to a specific value of a specific type.
    /// If limits would be outside representable values of the type,
    /// the limits are clamped to the min/max values of the type respectively,
    /// and the boolean flags indicate that the requested limits were outside the representable range.
    /// </summary>
    internal sealed class ClampedRange<T>
        where T : IComparable<T>
    {
        /// <summary>
        /// The lower bound of the range
        /// </summary>
        public readonly T LowerBound;

        /// <summary>
        /// Indicates whether the requested lower bound is outside the limits of the target type.
        /// </summary>
        public readonly bool LowerBoundExceedsTypeLimits;

        /// <summary>
        /// The upper bound of the range
        /// </summary>
        public readonly T UpperBound;

        /// <summary>
        /// Indicates whether the requested upper bound is outside the limits of the target type.
        /// </summary>
        public readonly bool UpperBoundExceedsTypeLimits;

        /// <summary>
        /// Constructs a range
        /// </summary>
        public ClampedRange(T lowerBound, bool lowerBoundExceedsTypeLimits, T upperBound, bool upperBoundExceedsTypeLimits)
        {
            LowerBound = lowerBound;
            LowerBoundExceedsTypeLimits = lowerBoundExceedsTypeLimits;
            UpperBound = upperBound;
            UpperBoundExceedsTypeLimits = upperBoundExceedsTypeLimits;
        }

        public bool ValueIsWithinRange(T value)
            => ValueIsGreaterThanOrEqualToLowerBound(value) && ValueIsLessThanOrEqualToUpperBound(value);

        public bool ValueIsGreaterThanLowerBound(T value)
            => LowerBoundExceedsTypeLimits || value.CompareTo(LowerBound) > 0;

        public bool ValueIsLessThanUpperBound(T value)
            => UpperBoundExceedsTypeLimits || value.CompareTo(UpperBound) < 0;

        public bool ValueIsGreaterThanOrEqualToLowerBound(T value)
            => LowerBoundExceedsTypeLimits || value.CompareTo(LowerBound) >= 0;

        public bool ValueIsLessThanOrEqualToUpperBound(T value)
            => UpperBoundExceedsTypeLimits || value.CompareTo(UpperBound) <= 0;
    }

    /// <summary>
    /// Provides factory functionality for creating numeric ranges with bounds clamped to the representable limits of
    /// the numeric type.
    /// </summary>
    /// <remarks>Use this type to create ranges where underflow and overflow at the bounds are prevented by
    /// clamping to the type's minimum and maximum values.</remarks>
    internal static class ClampedRange
    {
#if !NETFRAMEWORK
        /// <summary>
        /// Creates a range centered on a value using the specified tolerance, clamping bounds to the representable
        /// limits of the numeric type.
        /// </summary>
        /// <remarks>When a bound would exceed the type limits, the bound is clamped to the corresponding
        /// limit.</remarks>
        /// <typeparam name="T">A numeric value type that supports arithmetic and exposes minimum and maximum representable values.</typeparam>
        /// <param name="value">The center value of the range.</param>
        /// <param name="tolerance">The non-negative distance from the center value to each bound.</param>
        /// <returns>A <see cref="ClampedRange{T}"/> whose lower and upper bounds are computed from the value and tolerance, with
        /// bounds clamped to <c>T.MinValue</c> and <c>T.MaxValue</c> when necessary.</returns>
        public static ClampedRange<T> ForAbsolute<T>(T value, T tolerance)
            where T : INumber<T>, IMinMaxValue<T>
        {
            // Lower bound is 'value - tolerance', but clamped to T.MinValue
            // 'value - tolerance' < T.MinValue is equivalent to 'value < T.MinValue + tolerance'
            bool lowerBoundExceedsTypeLimits = value < T.MinValue + tolerance;
            T lowerBound = lowerBoundExceedsTypeLimits ? T.MinValue : value - tolerance;
            // Upper bound is 'value + tolerance', but clamped to T.MaxValue
            // 'value + tolerance' > T.MaxValue is equivalent to 'value > T.MaxValue - tolerance'
            bool upperBoundExceedsTypeLimits = value > T.MaxValue - tolerance;
            T upperBound = upperBoundExceedsTypeLimits ? T.MaxValue : value + tolerance;
            return new ClampedRange<T>(lowerBound, lowerBoundExceedsTypeLimits, upperBound, upperBoundExceedsTypeLimits);
        }
#else
        private static ClampedRange<T> ForAbsolute<T>(T value, T tolerance, T minValue, T maxValue, Func<T, T, T> add, Func<T, T, T> subtract)
            where T : struct, IComparable<T>
        {
            // Lower bound is 'value - tolerance', but clamped to T.MinValue
            // 'value - tolerance' < T.MinValue is equivalent to 'value < T.MinValue + tolerance'
            bool lowerBoundExceedsTypeLimits = value.CompareTo(add(minValue, tolerance)) < 0;
            T lowerBound = lowerBoundExceedsTypeLimits ? minValue : subtract(value, tolerance);
            // Upper bound is 'value + tolerance', but clamped to T.MaxValue
            bool upperBoundExceedsTypeLimits = value.CompareTo(subtract(maxValue, tolerance)) > 0;
            T upperBound = upperBoundExceedsTypeLimits ? maxValue : add(value, tolerance);
            return new ClampedRange<T>(lowerBound, lowerBoundExceedsTypeLimits, upperBound, upperBoundExceedsTypeLimits);
        }

        public static ClampedRange<int> ForAbsolute(int value, int tolerance)
            => ForAbsolute(value, tolerance, int.MinValue, int.MaxValue, (x, y) => x + y, (x, y) => x - y);
        public static ClampedRange<uint> ForAbsolute(uint value, uint tolerance)
            => ForAbsolute(value, tolerance, uint.MinValue, uint.MaxValue, (x, y) => x + y, (x, y) => x - y);
        public static ClampedRange<long> ForAbsolute(long value, long tolerance)
            => ForAbsolute(value, tolerance, long.MinValue, long.MaxValue, (x, y) => x + y, (x, y) => x - y);
        public static ClampedRange<ulong> ForAbsolute(ulong value, ulong tolerance)
            => ForAbsolute(value, tolerance, ulong.MinValue, ulong.MaxValue, (x, y) => x + y, (x, y) => x - y);
        public static ClampedRange<decimal> ForAbsolute(decimal value, decimal tolerance)
            => ForAbsolute(value, tolerance, decimal.MinValue, decimal.MaxValue, (x, y) => x + y, (x, y) => x - y);

        public static ClampedRange<T> ForAbsolute<T>(T value, T tolerance)
            where T : struct, IComparable<T>, IConvertible
        {
            if (typeof(T) == typeof(int))
                return Unsafe.As<ClampedRange<T>>(ForAbsolute(value.ToInt32(null), tolerance.ToInt32(null)));
            if (typeof(T) == typeof(uint))
                return Unsafe.As<ClampedRange<T>>(ForAbsolute(value.ToUInt32(null), tolerance.ToUInt32(null)));
            if (typeof(T) == typeof(long))
                return Unsafe.As<ClampedRange<T>>(ForAbsolute(value.ToInt64(null), tolerance.ToInt64(null)));
            if (typeof(T) == typeof(ulong))
                return Unsafe.As<ClampedRange<T>>(ForAbsolute(value.ToUInt64(null), tolerance.ToUInt64(null)));
            if (typeof(T) == typeof(decimal))
                return Unsafe.As<ClampedRange<T>>(ForAbsolute(value.ToDecimal(null), tolerance.ToDecimal(null)));

            throw new NotSupportedException($"The type {typeof(T)} is not supported by ClampedRange.For.");
        }
#endif
    }
}
