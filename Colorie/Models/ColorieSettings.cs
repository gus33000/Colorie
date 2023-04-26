// ---------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//
// The MIT License (MIT)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// ---------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Colorie.Common;
using Colorie.FileIO;
using Windows.Storage;

namespace Colorie.Models
{
    public class ColorieSettings
    {
        public ColorieSettings()
        {
            LastOpened = DateTime.Now;
        }

        public ColorieSettings(string colorieName, string templateImageName,
            DeviceResolutionType colorieResolutionType)
            : base()
        {
            ColorieImageName = templateImageName;
            ColorieName = colorieName;
            ColorieResolutionType = colorieResolutionType;
        }

        private static ColorieSettingsReader Reader { get; } = new ColorieSettingsReader();

        private ColorieSettingsWriter Writer { get; } = new ColorieSettingsWriter();

        public StorageFolder ColorieDirectory { get; set; }

        public string ColorieImageName { get; set; }

        public string ColorieName { get; set; }

        public DeviceResolutionType ColorieResolutionType { get; set; }

        public DateTime LastOpened { get; set; }

        public static async Task<ColorieSettings> LoadSettingsAsync(string colorieName)
        {
            var coloriesDirectory = await Tools.GetColoriesDirectoryAsync();
            var colorieDirectory = await coloriesDirectory.CreateFolderAsync(colorieName, CreationCollisionOption.OpenIfExists);

            var settings = await Reader.ReadAsync(colorieDirectory, colorieName + Tools.GetResourceString("FileType/settings"));
            settings.ColorieDirectory = colorieDirectory;
            return settings;
        }

        public async Task<StorageFolder> GetLibraryImageLocationAsync()
        {
            var assetsDir = await Tools.GetAssetsFolderAsync();
            var libraryImagesDir = await Tools.GetSubDirectoryAsync(assetsDir, "LibraryImages");
            var libraryImageDir = await Tools.GetSubDirectoryAsync(libraryImagesDir, ColorieImageName);

            return libraryImageDir;
        }

        public async Task<StorageFolder> GetLibraryImagePreprocessingLocationAsync()
        {
            var assetsDir = await Tools.GetAssetsFolderAsync();
            var libraryImagesDir = await Tools.GetSubDirectoryAsync(assetsDir, "LibraryImages");
            var libraryImageDir = await Tools.GetSubDirectoryAsync(libraryImagesDir, ColorieImageName);

            return libraryImageDir;
        }

        public async Task SaveSettingsToFileAsync()
        {
            if (ColorieDirectory == null)
            {
                var coloriesDirectory = await Tools.GetColoriesDirectoryAsync();
                ColorieDirectory = await Tools.CreateSubDirectoryAsync(coloriesDirectory, ColorieName);
            }

            var fname = ColorieName + Tools.GetResourceString("FileType/settings");
            await Writer.WriteAsync(ColorieDirectory, fname, this);
        }
    }
}