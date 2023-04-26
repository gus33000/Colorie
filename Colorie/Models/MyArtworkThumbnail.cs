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

using System.Threading.Tasks;
using Colorie.Common;
using Colorie.Components;

namespace Colorie.Models
{
    public class MyArtworkThumbnail : Thumbnail
    {
        public MyArtworkThumbnail(string colorieName, bool isHero)
            : base(isHero)
        {
            ThumbnailBackgroundImageName = colorieName + Tools.GetResourceString("FileType/thumbnail");
            ColorieName = colorieName;
        }

        private string ColorieName { get; }

        public Colorie Colorie { get; private set; }

        public override async Task LoadThumbnailAsync()
        {
            Colorie = await Colorie.LoadThumbnailDetailsAsync(ColorieName);

            if (Colorie == null || Colorie.ColorieHasError())
            {
                SetErrorThumbnail();
            }
            else
            {
                var settings = Colorie.Settings;
                var imgfile = await Tools.GetFileAsync(settings.ColorieDirectory,
                    settings.ColorieName + Tools.GetResourceString("FileType/thumbnail"));

                await LoadThumbnailBackgroundImageAsync(imgfile);
            }

            IsLoading = false;
        }

        protected override async Task LoadColorieAsync()
        {
            if (Colorie == null || Colorie.ColorieHasError())
            {
                MarkForDelete = await Colorie.DisplayErrorDialogAsync(ColorieName);
            }
            else
            {
                NavigationController.LoadColoriePage(Colorie);
            }
        }
    }
}
