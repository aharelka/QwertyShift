using System;

namespace QwertyShift
{
    /// <summary>
    /// Holds information about a specific keyboard layout.
    /// </summary>
    public class KeyboardLayoutInfo
    {
        public IntPtr Handle { get; }
        public string SystemName { get; }
        public string CustomName { get; }
        
        public string SpokenText => string.IsNullOrWhiteSpace(CustomName) ? SystemName : CustomName;

        /// <summary>
        /// Initializes a new instance of the KeyboardLayoutInfo class.
        /// </summary>
        /// <param name="handle">The memory pointer (handle) of the layout.</param>
        /// <param name="systemName">The default Windows name for this layout.</param>
        /// <param name="customName">An optional custom name set by the user.</param>
        public KeyboardLayoutInfo(IntPtr handle, string systemName, string customName = null)
        {
            Handle = handle;
            SystemName = systemName;
            CustomName = customName;
        }
    }

    public interface ILayoutAnnouncer : IDisposable
    {
        void Announce(KeyboardLayoutInfo info);
    }
}