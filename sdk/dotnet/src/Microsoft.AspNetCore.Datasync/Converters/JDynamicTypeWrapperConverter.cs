﻿// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.OData.Query.Wrapper;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Datasync.Converters
{
    /// <summary>
    /// Represents a custom <see cref="JsonConverter"/> to serialize <see cref="DynamicTypeWrapper"/> instances to JSON.
    /// </summary>
    internal class JDynamicTypeWrapperConverter : JsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified <see cref="DynamicTypeWrapper"/> type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>true if this instance can convert the specified object type; otherwise, false.</returns>
        public override bool CanConvert(Type objectType)
        {
            if (objectType is null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            return typeof(DynamicTypeWrapper).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        [SuppressMessage("General", "RCS1079:Throwing of new NotImplementedException.", Justification = "Only valid in write scenario.")]
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("DynamicTypeWrapper is only valid in write scenario.");
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (value is DynamicTypeWrapper dynamicTypeWrapper)
            {
                serializer.Serialize(writer, dynamicTypeWrapper.Values);
            }
        }
    }
}
