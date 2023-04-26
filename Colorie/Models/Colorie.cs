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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Colorie.Common;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;

namespace Colorie.Models
{
    public class Colorie
    {
        private uint CurrentCell { get; set; }

        public ColorieSettings Settings { get; private set; }

        public ColorieLibraryImage TemplateImage { get; private set; }

        private ColorieError Error { get; set; }

        public bool IsNewAndUnchanged { get; set; }

        public string ColorieName => Settings.ColorieName;

        public DateTime LastOpenedTime => Settings.LastOpened;

        public InkStrokeContainer InkStrokeContainer { get; private set; } = new InkStrokeContainer();

        public static async Task<Colorie> CreateFromTemplateAsync(string templateImageName)
        {
            var colorie = new Colorie() { IsNewAndUnchanged = true };
            var colorieName = Path.GetRandomFileName();
            await colorie.InitializeAsync(colorieName, templateImageName);
            return colorie;
        }

        private async Task InitializeAsync(string colorieName, string templateImageName)
        {
            Settings = new ColorieSettings(colorieName, templateImageName, Tools.ResolutionType);
            await Settings.SaveSettingsToFileAsync();
            var imageName = Settings.ColorieImageName +
                Tools.GetResolutionTypeAsString(Settings.ColorieResolutionType);
            TemplateImage = await ColorieBitmapLibraryImage.LoadImageFromFileAsync(
                await Settings.GetLibraryImageLocationAsync(),
                await Settings.GetLibraryImagePreprocessingLocationAsync(),
                imageName + Tools.GetResourceString("FileType/png"),
                imageName + Tools.GetResourceString("FileType/preprocessing"));
            InkStrokeContainer = new InkStrokeContainer();
        }

        public void SetOpenedTimeToNow() => Settings.LastOpened = DateTime.Now;

        public void SetCurrentCell(Point location)
        {
            if (!TemplateImage.IsOnBoundary(location))
            {
                CurrentCell = TemplateImage.GetCorrespondingCellId(location);
            }
        }

        public bool IsWithinUserCurrentCell(Point location) => CurrentCell == 0 ?
            false : CurrentCell == TemplateImage.GetCorrespondingCellId(location);

        public bool HasFilledCells() =>
            (TemplateImage as ColorieBitmapLibraryImage)?.CellColorCache.Count > 0;

        public Dictionary<uint, Color> ClearAllCells()
        {
            var oldCache = (TemplateImage as ColorieBitmapLibraryImage).CellColorCache;
            TemplateImage.EraseAllCells();
            return oldCache;
        }

        public byte[] ApplyFilledCells(Dictionary<uint, Color> dict) =>
            (TemplateImage as ColorieBitmapLibraryImage).ApplyFilledCells(dict);

        public void ClearAllInk() => InkStrokeContainer.Clear();

        public static async Task<Colorie> LoadThumbnailDetailsAsync(string colorieName)
        {
            var colorie = new Colorie();
            await colorie.LoadSettingsAsync(colorieName);
            return colorie;
        }

        public async Task LoadColoriePageFilesAsync()
        {
            var imageName = Settings.ColorieImageName +
                Tools.GetResolutionTypeAsString(Settings.ColorieResolutionType);
            TemplateImage = await ColorieBitmapLibraryImage.LoadImageFromFileAsync(
                await Settings.GetLibraryImageLocationAsync(),
                await Settings.GetLibraryImagePreprocessingLocationAsync(),
                imageName + Tools.GetResourceString("FileType/png"),
                imageName + Tools.GetResourceString("FileType/preprocessing"));
            await LoadInkAsync(Settings.ColorieDirectory);
        }

        private async Task LoadInkAsync(StorageFolder colorieDirectory)
        {
            InkStrokeContainer = new InkStrokeContainer();
            var fileName = ColorieName + Tools.GetResourceString("FileType/inkFileType");
            var file = await Tools.GetFileAsync(colorieDirectory, fileName);
            if (file != null)
            {
                using (var stream = await file.OpenSequentialReadAsync())
                {
                    await InkStrokeContainer.LoadAsync(stream);
                }
            }
        }

        private async Task LoadSettingsAsync(string colorieName)
        {
            var coloriesDirectory = await Tools.GetColoriesDirectoryAsync();
            var colorieDirectory = await Tools.GetSubDirectoryAsync(coloriesDirectory, colorieName);
            if (colorieDirectory == null)
            {
                Error = ColorieError.ColorieNotFound;
                return;
            }

            try
            {
                Settings = await ColorieSettings.LoadSettingsAsync(colorieName);
            }
            catch
            {
                // Set known defaults and mark as error
                Settings = new ColorieSettings(colorieName, null, DeviceResolutionType.Uncalculated)
                {
                    ColorieDirectory = colorieDirectory
                };

                Error = ColorieError.SettingsParsing;
                return;
            }

            var doesImageExist = await ColorieLibraryImage.CheckImageExistsAsync(
                await Settings.GetLibraryImageLocationAsync(),
                await Settings.GetLibraryImagePreprocessingLocationAsync(),
                Settings.ColorieImageName +
                    Tools.GetResolutionTypeAsString(Settings.ColorieResolutionType) +
                    Tools.GetResourceString("FileType/png"),
                Settings.ColorieImageName +
                    Tools.GetResolutionTypeAsString(Settings.ColorieResolutionType) +
                    Tools.GetResourceString("FileType/preprocessing"));

            if (!doesImageExist)
            {
                Error = ColorieError.TemplateImage;
            }
        }

        public async Task<bool> DeleteIfNewAndUnchangedAsync()
        {
            if (IsNewAndUnchanged)
            {
                await DeleteColorieAsync();
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task DeleteIfNewAndUnchangedElseSaveAsync() =>
            await (await DeleteIfNewAndUnchangedAsync() ? Task.CompletedTask : SaveToFileAsync(true));

        public async Task SaveAsync() => await SaveToFileAsync();

        private async Task SaveToFileAsync(bool isQuickSave = false)
        {
            await SaveFilledCellsToFileAsync(Settings.ColorieDirectory);
            await SaveInkToFileAsync(Settings.ColorieDirectory);

            if (!isQuickSave)
            {
                await Settings.SaveSettingsToFileAsync();
            }
        }

        private async Task SaveInkToFileAsync(StorageFolder colorieDirectory)
        {
            var fileName = ColorieName + Tools.GetResourceString("FileType/inkFileType");
            var inkFile = await Tools.CreateFileAsync(colorieDirectory, fileName);

            if (InkStrokeContainer.GetStrokes().Count == 0)
            {
                if (inkFile != null)
                {
                    await inkFile.DeleteAsync();
                }

                return;
            }

            using (var inkFileStream = (await inkFile.OpenStreamForWriteAsync()).AsOutputStream())
            {
                if (inkFileStream == null)
                {
                    throw new Exception("Cannot save to ink file");
                }

                await InkStrokeContainer.SaveAsync(inkFileStream);
            }
        }

        private async Task SaveFilledCellsToFileAsync(StorageFolder colorieDirectory) =>
            await (TemplateImage as ColorieBitmapLibraryImage)
                .SaveFilledCellsToFileAsync(ColorieName, colorieDirectory);

        public async Task DeleteColorieAsync()
        {
            try
            {
                await Settings.ColorieDirectory.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch
            {
                Debug.WriteLine("Colorie.DeleteColorieAsync() - Exception, Could not delete Colorie");
            }
        }

        public static async Task<bool> DisplayErrorDialogAsync(string colorieName)
        {
            var deleteColorieDialog = new ContentDialog
            {
                Title = Tools.GetResourceString("ErrorDialog/Title"),
                Content = Tools.GetResourceString("ErrorDialog/Content"),
                PrimaryButtonText = Tools.GetResourceString("ErrorDialog/IgnoreButtonText"),
                SecondaryButtonText = Tools.GetResourceString("ErrorDialog/DeleteButtonText")
            };

            var result = await deleteColorieDialog.ShowAsync();

            return result == ContentDialogResult.Secondary;
        }

        public bool ColorieHasError() => Error != ColorieError.None;

        private enum ColorieError
        {
            None,
            ColorieNotFound,
            SettingsParsing,
            TemplateImage,
            Ink,
            Ttf
        }
    }
}