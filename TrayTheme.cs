using System.Drawing;
using System.Windows.Forms;

namespace ImeHalfWidth;

internal sealed class TrayTheme : ProfessionalColorTable
{
    public override Color MenuBorder => Color.FromArgb(215, 224, 224);
    public override Color ToolStripDropDownBackground => Color.White;
    public override Color ImageMarginGradientBegin => Color.White;
    public override Color ImageMarginGradientMiddle => Color.White;
    public override Color ImageMarginGradientEnd => Color.White;
    public override Color MenuItemSelected => Color.FromArgb(230, 243, 243);
    public override Color MenuItemSelectedGradientBegin => Color.FromArgb(230, 243, 243);
    public override Color MenuItemSelectedGradientEnd => Color.FromArgb(230, 243, 243);
    public override Color MenuItemBorder => Color.FromArgb(15, 107, 107);
    public override Color SeparatorDark => Color.FromArgb(215, 224, 224);
    public override Color SeparatorLight => Color.FromArgb(230, 243, 243);
}

internal static class TrayMenu
{
    public static ContextMenuStrip Create()
    {
        return new ContextMenuStrip
        {
            Renderer = new ToolStripProfessionalRenderer(new TrayTheme()) { RoundedEdges = false },
            Font = new Font("Microsoft YaHei UI", 10f),
            ForeColor = Color.FromArgb(27, 42, 42),
            ShowCheckMargin = true,
            ShowImageMargin = false,
            Padding = new Padding(6, 8, 6, 8)
        };
    }
}
