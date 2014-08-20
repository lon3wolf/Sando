using Sando.UI.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Sando.UI.View
{
    public class ColorGenerator
    {

        static internal Brush GetNormalTextColor()
        {
            var key = Microsoft.VisualStudio.Shell.VsBrushes.ToolWindowTextKey;
            var brush = (Brush)Application.Current.Resources[key];
            return brush;
        }

        static internal Brush GetToolBackgroundColor()
        {
            var key = Microsoft.VisualStudio.Shell.VsBrushes.ToolWindowBackgroundKey;
            var brush = (Brush)Application.Current.Resources[key];
            return brush;
        }


        internal static Brush GetHistoryTextColor()
        {
            if (FileOpener.Is2012OrLater())
            {
                var key = Microsoft.VisualStudio.Shell.VsBrushes.ToolWindowTabMouseOverTextKey;
                var color = (Brush)Application.Current.Resources[key];
                var other = (Brush)Application.Current.Resources[Microsoft.VisualStudio.Shell.VsBrushes.ToolWindowBackgroundKey];
                if (color.ToString().Equals(other.ToString()))
                {
                    return (Brush)Application.Current.Resources[Microsoft.VisualStudio.Shell.VsBrushes.HelpSearchResultLinkSelectedKey];
                }
                else
                    return color;
            }
            else
            {
                var key = Microsoft.VisualStudio.Shell.VsBrushes.HelpSearchResultLinkSelectedKey;
                return (Brush)Application.Current.Resources[key];
            }
        }

        static internal Color GetHighlightColor()
        {
            var key = Microsoft.VisualStudio.Shell.VsBrushes.HighlightKey;
            var brush = (SolidColorBrush)Application.Current.Resources[key];
            return brush.Color;
        }

        static internal Color GetHighlightBorderColor()
        {
            var key = Microsoft.VisualStudio.Shell.VsBrushes.HighlightTextKey;
            var brush = (SolidColorBrush)Application.Current.Resources[key];
            return brush.Color;
        }
    }
}
