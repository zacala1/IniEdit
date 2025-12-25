using System.Text;
using System.Windows.Forms;
using IniEdit;

namespace IniEdit.GUI
{
    /// <summary>
    /// Helper class for clipboard operations with INI sections and properties
    /// </summary>
    public static class ClipboardHelper
    {
        private const string SectionFormat = "IniEdit.Section";
        private const string PropertyFormat = "IniEdit.Property";
        private const string PropertiesFormat = "IniEdit.Properties";

        /// <summary>
        /// Copy section to clipboard
        /// </summary>
        public static void CopySection(Section section)
        {
            var data = new DataObject();

            // Store as custom format
            var sb = new StringBuilder();
            sb.AppendLine($"[{section.Name}]");

            foreach (var prop in section)
            {
                var value = prop.IsQuoted ? $"\"{prop.Value}\"" : prop.Value;
                sb.AppendLine($"{prop.Name}={value}");
            }

            data.SetData(SectionFormat, section.Clone());
            data.SetText(sb.ToString());

            Clipboard.SetDataObject(data);
        }

        /// <summary>
        /// Copy property to clipboard
        /// </summary>
        public static void CopyProperty(Property property)
        {
            var data = new DataObject();

            // Clone the property to preserve all attributes
            var propClone = new Property(property.Name, property.Value)
            {
                Comment = property.Comment,
                IsQuoted = property.IsQuoted
            };
            foreach (var comment in property.PreComments)
            {
                propClone.PreComments.Add(comment);
            }

            data.SetData(PropertyFormat, propClone);

            var value = property.IsQuoted ? $"\"{property.Value}\"" : property.Value;
            data.SetText($"{property.Name}={value}");

            Clipboard.SetDataObject(data);
        }

        /// <summary>
        /// Copy multiple properties to clipboard
        /// </summary>
        public static void CopyProperties(IEnumerable<Property> properties)
        {
            var propList = properties.ToList();
            if (propList.Count == 0)
                return;

            if (propList.Count == 1)
            {
                CopyProperty(propList[0]);
                return;
            }

            var data = new DataObject();
            var clonedList = new List<Property>();
            var sb = new StringBuilder();

            foreach (var property in propList)
            {
                var propClone = new Property(property.Name, property.Value)
                {
                    Comment = property.Comment,
                    IsQuoted = property.IsQuoted
                };
                foreach (var comment in property.PreComments)
                {
                    propClone.PreComments.Add(comment);
                }
                clonedList.Add(propClone);

                var value = property.IsQuoted ? $"\"{property.Value}\"" : property.Value;
                sb.AppendLine($"{property.Name}={value}");
            }

            data.SetData(PropertiesFormat, clonedList);
            data.SetText(sb.ToString());

            Clipboard.SetDataObject(data);
        }

        /// <summary>
        /// Check if clipboard contains a section
        /// </summary>
        public static bool HasSection()
        {
            var data = Clipboard.GetDataObject();
            return data?.GetDataPresent(SectionFormat) ?? false;
        }

        /// <summary>
        /// Check if clipboard contains a property
        /// </summary>
        public static bool HasProperty()
        {
            var data = Clipboard.GetDataObject();
            return data?.GetDataPresent(PropertyFormat) ?? false;
        }

        /// <summary>
        /// Check if clipboard contains multiple properties
        /// </summary>
        public static bool HasProperties()
        {
            var data = Clipboard.GetDataObject();
            return data?.GetDataPresent(PropertiesFormat) ?? false;
        }

        /// <summary>
        /// Get section from clipboard
        /// </summary>
        public static Section? GetSection()
        {
            var data = Clipboard.GetDataObject();
            if (data?.GetDataPresent(SectionFormat) ?? false)
            {
                var section = data.GetData(SectionFormat) as Section;
                return section?.Clone();
            }
            return null;
        }

        /// <summary>
        /// Get property from clipboard
        /// </summary>
        public static Property? GetProperty()
        {
            var data = Clipboard.GetDataObject();
            if (data?.GetDataPresent(PropertyFormat) ?? false)
            {
                var property = data.GetData(PropertyFormat) as Property;
                if (property != null)
                {
                    // Clone the property
                    var propClone = new Property(property.Name, property.Value)
                    {
                        Comment = property.Comment,
                        IsQuoted = property.IsQuoted
                    };
                    foreach (var comment in property.PreComments)
                    {
                        propClone.PreComments.Add(comment);
                    }
                    return propClone;
                }
            }
            return null;
        }

        /// <summary>
        /// Get multiple properties from clipboard
        /// </summary>
        public static List<Property>? GetProperties()
        {
            var data = Clipboard.GetDataObject();
            if (data?.GetDataPresent(PropertiesFormat) ?? false)
            {
                var properties = data.GetData(PropertiesFormat) as List<Property>;
                if (properties != null)
                {
                    var clonedList = new List<Property>();
                    foreach (var property in properties)
                    {
                        var propClone = new Property(property.Name, property.Value)
                        {
                            Comment = property.Comment,
                            IsQuoted = property.IsQuoted
                        };
                        foreach (var comment in property.PreComments)
                        {
                            propClone.PreComments.Add(comment);
                        }
                        clonedList.Add(propClone);
                    }
                    return clonedList;
                }
            }
            return null;
        }
    }
}
