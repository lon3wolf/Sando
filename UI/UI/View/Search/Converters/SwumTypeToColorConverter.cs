using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Resources;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Sando.Recommender;
using Sando.DependencyInjection;
using System.Windows;
using Sando.UI.Actions;

namespace Sando.UI.View.Search.Converters
{
    [ValueConversion(typeof(SwumRecommnedationType), typeof(Brush))]
    public class SwumTypeToColorConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                var listBoxItem = values[0];
                var value = (((System.Windows.Controls.ListBoxItem)listBoxItem).Content as Sando.Recommender.SwumQueriesSorter.InternalSwumRecommendedQuey).Type;
                var selected = (bool)values[1];
                if (selected)
                    return ColorGenerator.GetHistoryTextColor();
                switch (value is SwumRecommnedationType ? (SwumRecommnedationType)value : SwumRecommnedationType.History)
                {
                    case SwumRecommnedationType.History:
                        return ColorGenerator.GetHistoryTextColor();
                    default:
                        return ColorGenerator.GetNormalTextColor();
                }
            }
            catch (Exception e)
            {
                return ColorGenerator.GetNormalTextColor();
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
