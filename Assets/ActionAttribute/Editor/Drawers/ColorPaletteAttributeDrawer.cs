using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ColorPaletteAttribute))]
    internal sealed class ColorPaletteAttributeDrawer : ActionPropertyDrawer { }
}
