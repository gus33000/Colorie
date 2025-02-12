﻿// ---------------------------------------------------------------------------------
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
using Colorie.Models;
using Colorie.Views;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input.Inking;

namespace Colorie.Common
{
    public class DrawingToolChangedEventArgs : EventArgs
    {
        public DrawingTool NewDrawingTool { get; set; }
    }

    public class PaletteColorChangedEventArgs : EventArgs
    {
        public Color NewColor { get; set; }
    }

    public class PageIndexChangedEventArgs : EventArgs
    {
        public PageIndexEnum NewIndex { get; set; }
    }

    public class StrokesCollectedEventArgs : EventArgs
    {
        public List<InkStroke> CollectedStrokes { get; set; }
    }

    public class CellOperationEventArgs : EventArgs
    {
        public Point FirstPoint { get; set; }

        public Point CurrentPoint { get; set; }
    }

    public class PointerPositionChangedEventArgs : EventArgs
    {
        public Point PreviousPoint { get; set; }

        public Point CurrentPoint { get; set; }
    }

    public class StrokeManufacturedEventArgs : EventArgs
    {
        public InkStroke Stroke { get; set; }
    }

    public class RightClickPanChangedEventArgs : EventArgs
    {
        public double HorizontalOffset { get; set; }

        public double VerticalOffset { get; set; }
    }
}
