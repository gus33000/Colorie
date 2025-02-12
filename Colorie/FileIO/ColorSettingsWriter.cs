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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Colorie.ViewModels;
using Windows.Storage;

namespace Colorie.FileIO
{
    public class ColorSettingsWriter : ColorieFileWriter<ColorPaletteState, Stream>
    {
        protected override async Task<Stream> GetStreamAsync(StorageFile file)
        {
            try
            {
                var writeStream = await file.OpenAsync(FileAccessMode.ReadWrite);
                var stream = writeStream.AsStreamForWrite();
                return stream;
            }
            catch
            {
                return null;
            }
        }

        protected override Task WriteToFile(Stream stream, ColorPaletteState paletteState)
        {
            try
            {
                var settings = new XmlWriterSettings { Async = true };
                using (var writer = XmlWriter.Create(stream, settings))
                {
                    writer.WriteStartElement("ColorSettings");

                    writer.WriteElementString("ColorsCount", paletteState.Colors.Length.ToString());
                    writer.WriteStartElement("Colors");
                    foreach (int? color in paletteState.Colors)
                    {
                        if (color == null)
                        {
                            writer.WriteElementString("Color", "null");
                        }
                        else
                        {
                            writer.WriteElementString("Color", color.ToString());
                        }
                    }

                    writer.WriteEndElement();

                    writer.WriteElementString("SelectedIndex", paletteState.SelectedIndex.ToString());

                    writer.WriteEndElement();
                }
            }
            catch
            {
                Debug.WriteLine("ColorSettingsWriter.Write() -- Exception Thrown");
            }

            return Task.CompletedTask;
        }
    }
}