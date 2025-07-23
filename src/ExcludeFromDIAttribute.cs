using System;

namespace auto_dial
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ExcludeFromDIAttribute : Attribute
    {
    }
}
