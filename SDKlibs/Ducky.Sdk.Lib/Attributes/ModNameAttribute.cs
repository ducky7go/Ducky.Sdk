using System;

namespace Ducky.Sdk.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ModNameAttribute : Attribute
{
    public string Name { get; }

    public ModNameAttribute(string name)
    {
        Name = name;
    }
}
