using System;

namespace CSConbinator
{
    [Serializable]
    public struct Token
    {
        public string Type;
        public Span Span;
    }
}