namespace SNESassetsWPF.Enums
{
    /// <summary>
    /// Debug rendering modes for PNL visualisation.
    /// </summary>
    public enum PnlDebugMode
    {
        None = 0,

        /// <summary>
        /// Shows pattern regions using PnlDebugRenderer.
        /// </summary>
        PatternDebug = 1,

        /// <summary>
        /// Future mode: overlays pattern outlines on top of the normal PNL renderer.
        /// </summary>
        OverlayDebug = 2
    }
}
