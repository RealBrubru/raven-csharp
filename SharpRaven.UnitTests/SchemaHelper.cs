﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace SharpRaven.UnitTests
{
    public static class SchemaHelper
    {
        public static string GetSchemaPath()
        {
            var directory = new DirectoryInfo(Environment.CurrentDirectory);
            FileInfo file = null;

            while (directory != null && directory.Exists &&
                   (file = directory.EnumerateFiles("*.json")
                                    .FirstOrDefault(f => f.FullName.EndsWith("schema.json"))) == null)
            {
                directory = directory.Parent;
            }

            return file != null ? file.FullName : null;
        }


        public static JsonSchema GetSchema()
        {
            var stream = typeof(SerializationTests).Assembly
                                                   .GetManifestResourceStream(typeof(SerializationTests),
                                                                              "schema.json");

            if (stream == null)
            {
                return null;
            }

            using (StreamReader reader = new StreamReader(stream))
            {
                return JsonSchema.Parse(reader.ReadToEnd());
            }
        }


        public static JsonSchema GenerateSchema(Type type)
        {
            var schemaGenerator = new JsonSchemaGenerator();
            var schema = schemaGenerator.Generate(type);
            schema.Title = type.Name;
            return schema.MapSchemaTypes(type);
        }


        private static PropertyInfo GetProperty(Type type, KeyValuePair<string, JsonSchema> jsProperty)
        {
            if (type.IsArray)
                // Unwrap the type contained in an array.
                type = type.GetElementType();

            foreach (var p in type.GetProperties())
            {
                var jsonPropertyAttribute = p.GetCustomAttribute<JsonPropertyAttribute>();

                if ((jsonPropertyAttribute == null || jsonPropertyAttribute.PropertyName != jsProperty.Key) &&
                    (p.Name != jsProperty.Key))
                    continue;

                if (jsonPropertyAttribute != null && jsonPropertyAttribute.Required == Required.Always)
                    jsProperty.Value.Required = true;

                return p;
            }

            throw new KeyNotFoundException("Property not found: " + jsProperty.Key);
        }


        /// <summary>
        /// Maps types from the specified <paramref name="type"/> to the <paramref name="schema"/> and
        /// returns the modified schema.
        /// </summary>
        /// <param name="schema">The schema.</param>
        /// <param name="type">The type.</param>
        /// <returns>
        /// The modified schema.
        /// </returns>
        /// <remarks>
        /// Shamefully stolen from <a href="https://json.codeplex.com/discussions/245604">CodePlex</a>.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// schema
        /// or
        /// type
        /// </exception>
        private static JsonSchema MapSchemaTypes(this JsonSchema schema, Type type)
        {
            if (schema == null)
                throw new ArgumentNullException("schema");

            if (type == null)
                throw new ArgumentNullException("type");

            if (schema.Properties == null)
                return schema;

            foreach (var jsProperty in schema.Properties)
            {
                PropertyInfo property = GetProperty(type, jsProperty);
                Type propertyType = property.PropertyType;

                if (propertyType.IsGenericType
                    && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    Type underlyingType = Nullable.GetUnderlyingType(propertyType);

                    if (underlyingType == typeof(DateTime))
                        jsProperty.Value.Format = "date-time";
                    else if (underlyingType.BaseType == typeof(Enum))
                    {
                        jsProperty.Value.Type = JsonSchemaType.String;
                        jsProperty.Value.Enum = new JArray(GetEnumValues(underlyingType, property));
                    }
                }
                else if (propertyType == typeof(DateTime))
                {
                    jsProperty.Value.Format = "date-time";
                }
                else if (propertyType.BaseType == typeof(Enum))
                {
                    jsProperty.Value.Type = JsonSchemaType.String;
                    jsProperty.Value.Enum = new JArray(GetEnumValues(propertyType, property));
                }
                else if (jsProperty.Value.Items != null && jsProperty.Value.Items.Any())
                {
                    foreach (var item in jsProperty.Value.Items)
                    {
                        var arg = propertyType.GetGenericArguments();
                        if (arg.Any())
                            propertyType = arg[0];

                        item.MapSchemaTypes(propertyType);
                    }
                }
                else if (jsProperty.Value.Properties != null && jsProperty.Value.Properties.Any())
                {
                    jsProperty.Value.MapSchemaTypes(propertyType);
                }
            }

            return schema;
        }


        private static IEnumerable<object> GetEnumValues(Type enumType, PropertyInfo property)
        {
            var converterAttribute = property.GetCustomAttribute<JsonConverterAttribute>();

            if (converterAttribute != null)
            {
                var converter = (JsonConverter) Activator.CreateInstance(converterAttribute.ConverterType);
                if (converter.CanConvert(enumType))
                {
                    foreach (var value in Enum.GetValues(enumType))
                    {
                        using (var stringWriter = new StringWriter())
                        {
                            using (var jsonWriter = new JsonTextWriter(stringWriter))
                            {
                                converter.WriteJson(jsonWriter, value, new JsonSerializer());
                            }

                            yield return stringWriter.ToString().Replace("\"", String.Empty);
                            ;
                        }
                    }
                }
            }
            else
                foreach (var name in Enum.GetNames(enumType))
                    yield return name;
        }
    }
}