using System;
using System.ComponentModel;
using System.Globalization;
using UnityEngine;

namespace HandQuickbelts
{
    public sealed class Vector3ConfigTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string text = value as string;
            if (text == null)
            {
                return base.ConvertFrom(context, culture, value);
            }

            string[] parts = text.Trim().Trim('(', ')').Split(',');
            if (parts.Length != 3)
            {
                throw new FormatException("Expected a vector in x, y, z format.");
            }

            return new Vector3(
                float.Parse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture),
                float.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture),
                float.Parse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture));
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is Vector3)
            {
                Vector3 vector = (Vector3)value;
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.######}, {1:0.######}, {2:0.######}",
                    vector.x,
                    vector.y,
                    vector.z);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
