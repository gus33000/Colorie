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

using Colorie.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Colorie.Views
{
    public class ArtworkTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ArtworkHeroTemplate { get; set; }

        public DataTemplate ArtworkTemplate { get; set; }

        public DataTemplate ArtworkLoadingTemplate { get; set; }

        public DataTemplate ArtworkErrorTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) =>
            !(item is Thumbnail thumbnail) || thumbnail.HadError ? ArtworkErrorTemplate :
            thumbnail.IsLoading ? ArtworkLoadingTemplate :
            thumbnail.IsHero ? ArtworkHeroTemplate : ArtworkTemplate;
    }
}
