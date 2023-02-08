namespace ZLibDotNet;

/// <summary>
/// A proxy with nuint overloads for the System.Runtime.CompilerServices.Unsafe class that contains generic, low-level functionality for manipulating pointers.
/// </summary>
internal static class Unsafe
{
    /// <summary>
    /// Adds an element offset to the given reference.
    /// </summary>
    /// <typeparam name="T">The type of reference.</typeparam>
    /// <param name="source">The reference to add the offset to.</param>
    /// <param name="elementOffset">The offset to add.</param>
    /// <returns>A new reference that reflects the addition of offset to pointer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T Add<T>(ref T source, int elementOffset) =>
        ref Add(ref source, (uint)elementOffset);

    /// <summary>
    /// Adds an element offset to the given reference.
    /// </summary>
    /// <typeparam name="T">The type of reference.</typeparam>
    /// <param name="source">The reference to add the offset to.</param>
    /// <param name="elementOffset">The offset to add.</param>
    /// <returns>A new reference that reflects the addition of offset to pointer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T Add<T>(ref T source, nuint elementOffset) =>
        ref netUnsafe.Add(ref source, (int)elementOffset);

    /// <summary>
    /// Subtracts an element offset from the given reference.
    /// </summary>
    /// <typeparam name="T">The type of reference.</typeparam>
    /// <param name="source">The reference to subtract the offset from.</param>
    /// <param name="elementOffset">The offset to subtract.</param>
    /// <returns>A new reference that reflects the subraction of offset from pointer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T Subtract<T>(ref T source, int elementOffset) =>
        ref Subtract(ref source, (uint)elementOffset);

    /// <summary>
    /// Subtracts an element offset from the given reference.
    /// </summary>
    /// <typeparam name="T">The type of reference.</typeparam>
    /// <param name="source">The reference to subtract the offset from.</param>
    /// <param name="elementOffset">The offset to subtract.</param>
    /// <returns>A new reference that reflects the subraction of offset from pointer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T Subtract<T>(ref T source, nuint elementOffset) =>
        ref netUnsafe.Subtract(ref source, (int)elementOffset);
}