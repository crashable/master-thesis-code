namespace AudioBufferBurstLibrary
{
    public interface INativeObject
    {
        public bool Allocated { get; }
        internal void ReleaseResources();
    }
}