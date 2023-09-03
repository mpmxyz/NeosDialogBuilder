namespace NeosDialogBuilder
{
    /// <summary>
    /// Allows mapping a value and getting the original value from a mapped value
    /// </summary>
    /// <typeparam name="I"></typeparam>
    /// <typeparam name="O"></typeparam>
    public interface IReversibleMapper<I, O>
    {
        /// <summary>
        /// Tries to map the value
        /// </summary>
        /// <param name="value">original value</param>
        /// <param name="mapped">mapped value if function return strue</param>
        /// <returns>true, if mapping is successful</returns>
        bool TryMap(I value, out O mapped);

        /// <summary>
        /// Tries to determine original value
        /// </summary>
        /// <param name="value">mapped value</param>
        /// <param name="unmapped">original value, if function returns true</param>
        /// <returns>true, if rever</returns>
        bool TryUnmap(O value, out I unmapped);
    }
}
