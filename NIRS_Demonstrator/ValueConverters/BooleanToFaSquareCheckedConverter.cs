using Avalonia.Data.Converters;
using System;
using System.Globalization;
using System.Windows;

namespace NIRS_Demonstrator
{
    public class BooleanToFaEyeIconConverter : IValueConverter
    {
        public static readonly BooleanToFaEyeIconConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
           
                return (bool)value ? "fa-solid fa-eye-slash" : "fa-solid fa-eye";//"\uf205" : "\uf204";
          
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
