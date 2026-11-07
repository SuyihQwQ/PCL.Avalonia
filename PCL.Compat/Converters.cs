// SPDX-License-Identifier: Apache-2.0
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace MeloongCore.Compat;

/// <summary>对绑定值做加法，parameter 为加数。</summary>
public class AdditionConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is null) return 0d;
        if (!double.TryParse(value.ToString(), NumberStyles.Any, culture, out var before)) return 0d;
        var scale = 1d;
        if (parameter is not null) double.TryParse(parameter.ToString(), NumberStyles.Any, culture, out scale);
        return before + scale;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is null) return BindingOperations.DoNothing;
        if (!double.TryParse(value.ToString(), NumberStyles.Any, culture, out var before)) return BindingOperations.DoNothing;
        var scale = 1d;
        if (parameter is not null) double.TryParse(parameter.ToString(), NumberStyles.Any, culture, out scale);
        return before - scale;
    }
}

/// <summary>对绑定值做乘法，parameter 为乘数。</summary>
public class MultiplicationConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is null) return 0d;
        if (!double.TryParse(value.ToString(), NumberStyles.Any, culture, out var before)) return 0d;
        var scale = 1d;
        if (parameter is not null) double.TryParse(parameter.ToString(), NumberStyles.Any, culture, out scale);
        return before * scale;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is null) return BindingOperations.DoNothing;
        if (!double.TryParse(value.ToString(), NumberStyles.Any, culture, out var before)) return BindingOperations.DoNothing;
        var scale = 1d;
        if (parameter is not null) double.TryParse(parameter.ToString(), NumberStyles.Any, culture, out scale);
        if (scale == 0) return BindingOperations.DoNothing;
        return before / scale;
    }
}

/// <summary>布尔取反后用于 IsVisible。</summary>
public class InverseBooleanToVisibilityConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is null) return true;
        return bool.TryParse(value.ToString(), out var boolValue) ? !boolValue : true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is null) return false;
        return value is bool visible && !visible;
    }
}

/// <summary>布尔取反。</summary>
public class InverseBooleanConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is null) return false;
        return bool.TryParse(value.ToString(), out var boolValue) && !boolValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is null) return false;
        return bool.TryParse(value.ToString(), out var boolValue) && !boolValue;
    }
}
