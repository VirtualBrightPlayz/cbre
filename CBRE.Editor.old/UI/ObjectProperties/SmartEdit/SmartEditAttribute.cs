using CBRE.DataStructures.GameData;
using System;

namespace CBRE.Editor.UI.ObjectProperties.SmartEdit
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class SmartEditAttribute : Attribute
    {
        public VariableType VariableType { get; set; }

        public SmartEditAttribute(VariableType variableType)
        {
            VariableType = variableType;
        }
    }
}