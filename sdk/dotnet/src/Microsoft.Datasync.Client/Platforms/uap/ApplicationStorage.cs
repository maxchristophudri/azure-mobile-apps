﻿// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Datasync.Client.Utils;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Microsoft.Datasync.Client.Platforms
{
    /// <summary>
    /// An implementation of the <see cref="IApplicationStorage"/> interface for
    /// UWP/UAP/Windows Storage platform.
    /// </summary>
    internal class ApplicationStorage : IApplicationStorage
    {
        internal ApplicationStorage(string containerName = "")
        {
            SharedContainerName = string.IsNullOrWhiteSpace(containerName) ? "ms-datasync-client" : containerName;
            Preferences = ApplicationData.Current.LocalSettings.CreateContainer(SharedContainerName, ApplicationDataCreateDisposition.Always).Values;
        }

        /// <summary>
        /// The name of the shared container for preferences.
        /// </summary>
        private string SharedContainerName { get; }

        /// <summary>
        /// The set of properties for the preference storage.
        /// </summary>
        private IPropertySet Preferences { get; }

        /// <summary>
        /// Clear all the values within the store.
        /// </summary>
        public void ClearValues()
        {
            Preferences.Clear();
        }

        /// <summary>
        /// Remove a provided key from the store, if it exists.
        /// </summary>
        /// <param name="key">The key</param>
        public void RemoveValue(string key)
        {
            if (Preferences.ContainsKey(key))
            {
                Preferences.Remove(key);
            }
        }

        /// <summary>
        /// Stores the provided key-value pair into the application storage, overwriting
        /// the same key if needed.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value to store</param>
        public void SetValue(string key, string value)
        {
            Preferences[key] = value;
        }

        /// <summary>
        /// Try to get a value from the key-value application storage.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The retrieved value</param>
        /// <returns>True if the key was found</returns>
        public bool TryGetValue(string key, out string value)
        {
            if (Preferences.TryGetValue(key, out object prefValue))
            {
                value = prefValue.ToString();
                return value != null;
            }

            value = null;
            return false;
        }
    }
}
