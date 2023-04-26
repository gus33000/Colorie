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
using System.Threading.Tasks;
using Colorie.Common;
using Colorie.Components;
using Colorie.Models;
using Colorie.UndoRedoOperations;
using Colorie.ViewModels;
using Microsoft.Graphics.Canvas;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace Colorie.Views
{
    public class ColoriePageController : Observable
    {
        public ColoriePageController(CanvasElements canvasElements, ColorPaletteViewModel colorPaletteVM)
        {
            CanvasElements = canvasElements;
            ColorPaletteViewModel = colorPaletteVM;
            CanvasInputController = new CanvasInputController(CanvasElements);
            SetupInputEvents();
            DataTransferManager.GetForCurrentView().DataRequested += OnShareDataRequested;
            AutosaveTimer.Tick += (s, e) => OnAutosave();
        }

        ~ColoriePageController()
        {
            DataTransferManager.GetForCurrentView().DataRequested -= OnShareDataRequested;
        }

        private CanvasElements CanvasElements { get; }

        private ColorPaletteViewModel ColorPaletteViewModel { get; }

        private CanvasInputController CanvasInputController { get; set; }

        private UndoRedo UndoRedo { get; } = new UndoRedo();

        private bool IsControlPressed { get; set; }

        private Models.Colorie CurrentColorie { get; set; }

        private DispatcherTimer AutosaveTimer { get; } = new DispatcherTimer();

        private bool IsCurrentlySaving { get; set; }

        public event EventHandler ColorieLoadCompleted;

        public InkCellController InkCellController
        {
            get => _inkCellController;
            set => Set(ref _inkCellController, value);
        }

        private InkCellController _inkCellController;

        private async void OnAutosave()
        {
            UndoRedo.EndTransaction();
            UpdateButtonState();

            await SaveAsync();
            ResetAutosaveTimer();
        }

        public void CleanUp()
        {
            DataTransferManager.GetForCurrentView().DataRequested -= OnShareDataRequested;
        }

        public async Task LoadColorieAsync(Models.Colorie colorie)
        {
            IsColorieLoading = true;
            var loadingDialog = new ColorieLoadingDialog(Tools.GetResourceString("Dialog/Loading"));
            loadingDialog.ShowAsync().AsTask().ContinueWithoutWaiting();

            CurrentColorie = colorie;

            await CurrentColorie.LoadColoriePageFilesAsync();

            InkCellController = new InkCellController(colorie);
            CanvasInputController.InkCellController = InkCellController;
            CanvasInputController.CurrentColorie = CurrentColorie;

            OnPropertyChanged(nameof(TemplateImage));
            OnPropertyChanged(nameof(TemplateImageHeight));
            OnPropertyChanged(nameof(TemplateImageWidth));

            await InkCellController.LoadImageAsync();

            ColorieLoadCompleted?.Invoke(this, EventArgs.Empty);

            UpdateButtonState();

            IsColorieLoading = false;
            loadingDialog.Hide();
        }

        public float DisplayDpi { get; set; } = Constants.InitialDisplayDpi;

        public double InkScale { get; set; } = 1;

        public DrawingTool DrawingTool
        {
            get => CanvasInputController.DrawingTool;
            set => CanvasInputController.DrawingTool = value;
        }

        public bool IsTouchInkingEnabled
        {
            get => CanvasElements.InkCanvas.InkPresenter.InputDeviceTypes
                .HasFlag(CoreInputDeviceTypes.Touch);
            set
            {
                if (value != CanvasElements.InkCanvas.InkPresenter.InputDeviceTypes
                    .HasFlag(CoreInputDeviceTypes.Touch))
                {
                    if (value)
                    {
                        CanvasElements.InkCanvas.InkPresenter.InputDeviceTypes |=
                            CoreInputDeviceTypes.Touch;
                    }
                    else
                    {
                        CanvasElements.InkCanvas.InkPresenter.InputDeviceTypes &=
                            ~CoreInputDeviceTypes.Touch;
                    }

                    OnPropertyChanged();
                }
            }
        }

        public bool IsColorieLoading
        {
            get => _isColorieLoading;
            set => Set(ref _isColorieLoading, value);
        }

        private bool _isColorieLoading;

        public BitmapImage TemplateImage => (CurrentColorie.TemplateImage
            as ColorieBitmapLibraryImage)?.LibraryBitmapImage;

        public uint TemplateImageHeight => (CurrentColorie.TemplateImage
            as ColorieBitmapLibraryImage)?.Height ?? 0;

        public uint TemplateImageWidth => (CurrentColorie.TemplateImage
            as ColorieBitmapLibraryImage)?.Width ?? 0;

        public bool CanUndo => UndoRedo.HasUndoOperations();

        public bool CanRedo => UndoRedo.HasRedoOperations();

        public async Task<bool> DeleteNewColorieIfUnchangedAsync() =>
            await (CurrentColorie?.DeleteIfNewAndUnchangedAsync() ?? Task.FromResult(true));

        public bool CanDeleteInk => CurrentColorie != null &&
            (CurrentColorie.InkStrokeContainer.GetStrokes().Count > 0 ||
            CurrentColorie.HasFilledCells());

        public void ResetAutosaveTimer() => AutosaveTimer?.Stop();

        private void StartAutosaveTimer(int delayInMilliseconds = 500)
        {
            ResetAutosaveTimer();
            AutosaveTimer.Interval = new TimeSpan(delayInMilliseconds * 10 * 1000);
            AutosaveTimer?.Start();
        }

        public void OnUndo()
        {
            UndoRedo.Undo();
            UpdateButtonState();
            CanvasElements.DryInkCanvas.Invalidate();

            StartAutosaveTimer();
        }

        public void OnRedo()
        {
            UndoRedo.Redo();
            UpdateButtonState();
            CanvasElements.DryInkCanvas.Invalidate();

            StartAutosaveTimer();
        }

        public void OnShare() => DataTransferManager.ShowShareUI();

        public async void OnSaveToFile() => await ExportColorieAsync();

        public async Task SaveAsync()
        {
            if (!IsCurrentlySaving)
            {
                // Currently here in case back button is pressed repeatedly.
                IsCurrentlySaving = true;
                await ColorPaletteViewModel.SaveColorPaletteStateAsync();
                await SaveColorieThumbnailAsync(CurrentColorie.Settings.ColorieDirectory, CanvasElements.ColorieGrid);
                await CurrentColorie.SaveAsync();
                IsCurrentlySaving = false;
            }
        }

        private async void OnShareDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var deferral = args.Request.GetDeferral();

            await ShareAsync(args.Request.Data);

            deferral.Complete();
        }

        private async Task ShareAsync(DataPackage dataPackage)
        {
            dataPackage.Properties.Title = Tools.GetResourceString("Sharing/ShareTitle");
            dataPackage.Properties.Description = Tools.GetResourceString("Sharing/ShareDescription");

            var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("colorie.png",
                CreationCollisionOption.GenerateUniqueName);
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);

            await ColorieExporter.ExportIntoStreamAsync(CanvasElements.ColorieGrid, 1080, 720, stream);

            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(stream));

            dataPackage.SetStorageItems(new[] { file });

            var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem);
            dataPackage.Properties.Thumbnail = RandomAccessStreamReference.CreateFromStream(thumbnail);

            stream.Dispose();
        }

        public async void OnClearAllInk()
        {
            UndoRedo.StartTransaction();
            UndoRedo.InsertUndoRedoOperation(UndoRedoOperation.EraseAllStrokes,
                new StrokeOperationArgs
                {
                    StrokeContainer = CurrentColorie.InkStrokeContainer,
                    ModifiedStrokes = CurrentColorie.InkStrokeContainer.GetStrokes()
                });
            CurrentColorie.ClearAllInk();
            var oldCache = await InkCellController.ClearAllCellsAsync();
            UndoRedo.InsertUndoRedoOperation(UndoRedoOperation.EraseAllCells,
                new CellOperationArgs
                {
                    CellController = InkCellController,
                    ColorDictionary = oldCache
                });
            UndoRedo.EndTransaction();

            UpdateButtonState();

            CanvasElements.DryInkCanvas.Invalidate();

            StartAutosaveTimer();
        }

        public async Task ExportColorieAsync()
        {
            var savePicker = new FileSavePicker();
            savePicker.FileTypeChoices.Add(Tools.GetResourceString("FilePicker/FileTypeChoiceHelper"),
                new List<string> { Tools.GetResourceString("FilePicker/FileTypeChoice") });

            var file = await savePicker.PickSaveFileAsync();

            // Only continue if the user actually selected a file.
            if (file != null)
            {
                var stream = await file.OpenAsync(FileAccessMode.ReadWrite);

                await ColorieExporter.ExportIntoStreamAsync(CanvasElements.ColorieGrid, (int)CanvasElements.ColorieGrid.Width,
                    (int)CanvasElements.ColorieGrid.Height, stream);

                stream.Dispose();
            }
        }

        public async Task SaveColorieThumbnailAsync(StorageFolder colorieDirectory, FrameworkElement elementToGenerate)
        {
            var thumbnailFile = await Tools.CreateFileAsync(colorieDirectory,
                CurrentColorie.ColorieName + Tools.GetResourceString("FileType/thumbnail"));

            if (thumbnailFile != null)
            {
                try
                {
                    using (var stream = await thumbnailFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        var ratio = elementToGenerate.Width / elementToGenerate.Height;

                        // Multiple by 2 for Hero Size
                        var h = (int)((Constants.HeroThumbnailSize.Height * 2) - 1);
                        var w = (int)((Constants.HeroThumbnailSize.Height * 2 * ratio) - 1);

                        await ColorieExporter.ExportIntoStreamAsync(elementToGenerate, w, h, stream);
                    }
                }
                catch
                {
                    // This is fine, on this rare occurance a thumbnail simply isn't generated,
                    // normally this is due to multiple simulataneous calls to this function.
                }
            }
        }

        public void DrawDryInk(CanvasDrawingSession session) =>
            session.DrawInk(CurrentColorie.InkStrokeContainer.GetStrokes());

        private void UpdateButtonState()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(CanDeleteInk));
        }

        public void Page_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Control)
            {
                IsControlPressed = true;
                e.Handled = true;
            }
        }

        public void Page_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Control:
                    IsControlPressed = false;
                    e.Handled = true;
                    break;

                case VirtualKey.Y:
                    if (IsControlPressed)
                    {
                        OnRedo();
                    }

                    e.Handled = true;
                    break;

                case VirtualKey.Z:
                    if (IsControlPressed)
                    {
                        OnUndo();
                    }

                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        private void SetupInputEvents()
        {
            CanvasInputController.PointerPressed += OnPointerPressed;
            CanvasInputController.PointerReleased += OnPointerReleased;
            CanvasInputController.StrokesCollected += OnStrokesCollected;
            CanvasInputController.StrokeEraserMoved += OnStrokeEraserMoved;
            CanvasInputController.RightPanMoved += OnRightPanMoved;
            CanvasInputController.FillCell += OnFillCell;
            CanvasInputController.EraseCell += OnEraseCell;
            CanvasInputController.StrokeManufactured += OnStrokeManufactured;
        }

        private void OnPointerPressed(object sender, EventArgs e)
        {
            ResetAutosaveTimer();
            ColorPaletteViewModel.SetPaletteToSelectedColor();
        }

        // Shorter delay to prevent UI taking too long to update
        private void OnPointerReleased(object sender, EventArgs e)
        {
            StartAutosaveTimer(Constants.ShortDelay);
            CurrentColorie.IsNewAndUnchanged = false;
        }

        public void OnStrokeManufactured(object sender, StrokeManufacturedEventArgs e)
        {
            CurrentColorie.InkStrokeContainer.AddStroke(e.Stroke);
            CanvasElements.DryInkCanvas.Invalidate();

            UndoRedo.InsertUndoRedoOperation(UndoRedoOperation.AddStrokes,
                new StrokeOperationArgs
                {
                    StrokeContainer = CurrentColorie.InkStrokeContainer,
                    ModifiedStrokes = new List<InkStroke> { e.Stroke }
                });
        }

        private void OnStrokesCollected(object sender, StrokesCollectedEventArgs e)
        {
            CurrentColorie.InkStrokeContainer.AddStrokes(e.CollectedStrokes);
            CanvasElements.DryInkCanvas.Invalidate();

            UndoRedo.InsertUndoRedoOperation(UndoRedoOperation.AddStrokes,
                new StrokeOperationArgs
                {
                    StrokeContainer = CurrentColorie.InkStrokeContainer,
                    ModifiedStrokes = e.CollectedStrokes.AsReadOnly()
                });

            UpdateButtonState();
        }

        private void OnStrokeEraserMoved(object sender, PointerPositionChangedEventArgs e)
        {
            var rect = CurrentColorie.InkStrokeContainer.SelectWithLine(
                e.PreviousPoint, e.CurrentPoint);
            var erasedStrokes = new List<InkStroke>();
            foreach (var stroke in CurrentColorie.InkStrokeContainer.GetStrokes())
            {
                if (stroke.Selected)
                {
                    erasedStrokes.Add(stroke);
                }
            }

            if (!rect.IsEmpty && rect.Height * rect.Width > 0)
            {
                if (erasedStrokes.Count > 0)
                {
                    UndoRedo.InsertUndoRedoOperation(UndoRedoOperation.EraseStrokes,
                        new StrokeOperationArgs
                        {
                            StrokeContainer = CurrentColorie.InkStrokeContainer,
                            ModifiedStrokes = erasedStrokes
                        });
                    UpdateButtonState();
                }

                CurrentColorie.InkStrokeContainer.DeleteSelected();
                CanvasElements.DryInkCanvas.Invalidate();
            }
        }

        private void OnRightPanMoved(object sender, RightClickPanChangedEventArgs e) =>
            CanvasElements.CanvasScrollViewer.ChangeView(e.HorizontalOffset, e.VerticalOffset,
                CanvasElements.CanvasScrollViewer.ZoomFactor, true);

        private async void OnFillCell(object sender, CellOperationEventArgs e)
        {
            var firstCellId = CurrentColorie.TemplateImage.GetCorrespondingCellId(e.FirstPoint);
            var cellId = CurrentColorie.TemplateImage.GetCorrespondingCellId(e.CurrentPoint);
            if (cellId != 0 && cellId == firstCellId)
            {
                DeleteInkWithinCell(cellId, CurrentColorie.InkStrokeContainer);

                var prevColor = (CurrentColorie.TemplateImage as ColorieBitmapLibraryImage).GetColorOfCell(cellId);
                var newColor = CanvasElements.InkCanvas.InkPresenter.CopyDefaultDrawingAttributes().Color;
                await InkCellController.FillCellAsync(cellId, newColor);
                UndoRedo.InsertUndoRedoOperation(UndoRedoOperation.FillCell,
                    new CellOperationArgs
                    {
                        CellController = InkCellController,
                        CellId = cellId,
                        NewColor = newColor,
                        PreviousColor = prevColor
                    });
                UndoRedo.EndTransaction();
                UpdateButtonState();
            }

            CanvasElements.DryInkCanvas.Invalidate();
        }

        private async void OnEraseCell(object sender, CellOperationEventArgs e)
        {
            var firstCellId = CurrentColorie.TemplateImage.GetCorrespondingCellId(e.FirstPoint);
            var cellId = CurrentColorie.TemplateImage.GetCorrespondingCellId(e.CurrentPoint);
            if (cellId != 0 && cellId == firstCellId)
            {
                DeleteInkWithinCell(cellId, CurrentColorie.InkStrokeContainer);
                var color = (CurrentColorie.TemplateImage as ColorieBitmapLibraryImage).GetColorOfCell(cellId);
                await InkCellController.EraseCellAsync(cellId);
                UndoRedo.InsertUndoRedoOperation(UndoRedoOperation.EraseCell,
                    new CellOperationArgs
                    {
                        CellController = InkCellController,
                        CellId = cellId,
                        NewColor = Constants.ClearColor,
                        PreviousColor = color
                    });
                UndoRedo.EndTransaction();
                UpdateButtonState();
            }

            CanvasElements.DryInkCanvas.Invalidate();
        }

        private void DeleteInkWithinCell(uint cellId, InkStrokeContainer strokeContainer)
        {
            var erasedStrokes = InkCellController.DeleteInkWithinCell(cellId, CurrentColorie.InkStrokeContainer);
            if (erasedStrokes.Count > 0)
            {
                UndoRedo.InsertUndoRedoOperation(UndoRedoOperation.EraseStrokes,
                    new StrokeOperationArgs
                    {
                        StrokeContainer = CurrentColorie.InkStrokeContainer,
                        ModifiedStrokes = erasedStrokes
                    });
            }

            CanvasElements.DryInkCanvas.Invalidate();
        }
    }
}
