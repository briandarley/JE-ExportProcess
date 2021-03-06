﻿using System.IO;
using System.Net.Mail;
using Aspose.Pdf;
using Aspose.Pdf.Devices;
using BitMiracle.LibTiff.Classic;


namespace ExportProcess.Utilities
{
    public class TiffConversion
    {

        public static byte[] ExtractMultiTiffDocumentsToByteArray(string sourceFile)
        {
            using (var ms = new MemoryStream())
            {

                var ts = new TiffStream();

                using (var input = Tiff.Open(sourceFile, "r"))
                {
                    var totalPages = input.NumberOfDirectories();
                    var pageNumber = 0;
                    using (var output = Tiff.ClientOpen("someName", Constants.LibTiff.FileMode.Write, ms, ts))
                    {

                        do
                        {
                            AddPageToTiff(input, output, pageNumber, totalPages);

                            pageNumber += 1;
                        } while (input.ReadDirectory());

                        ms.Position = 0;
                        var result = new byte[ts.Size(ms)];
                        ts.Read(ms, result, 0, result.Length);
                        return result;

                    }
                }
            }

        }



        private static void AddPageToTiff(Tiff sourceFile, Tiff destinationFile, int pageNumber, int totalPages)
        {

            var width = sourceFile.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            var height = sourceFile.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            var samplesPerPixel = sourceFile.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
            var bitsPerSample = sourceFile.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            var photo = sourceFile.GetField(TiffTag.PHOTOMETRIC)[0].ToInt();
            var xresolution = sourceFile.GetField(TiffTag.XRESOLUTION)[0].ToInt();
            var yresolution = sourceFile.GetField(TiffTag.YRESOLUTION)[0].ToInt();

            var scanlineSize = sourceFile.ScanlineSize();
            var buffer = new byte[height][];
            for (var i = 0; i < height; ++i)
            {
                buffer[i] = new byte[scanlineSize];
                sourceFile.ReadScanline(buffer[i], i);
            }

            destinationFile.SetDirectory(0);
            destinationFile.SetField(TiffTag.IMAGEWIDTH, width);
            destinationFile.SetField(TiffTag.IMAGELENGTH, height);
            destinationFile.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel);
            destinationFile.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample);
            destinationFile.SetField(TiffTag.ROWSPERSTRIP, destinationFile.DefaultStripSize(0));
            destinationFile.SetField(TiffTag.PHOTOMETRIC, photo);
            destinationFile.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
            destinationFile.SetField(TiffTag.COMPRESSION, Compression.CCITT_T6);
            destinationFile.SetField(TiffTag.XRESOLUTION, xresolution);
            destinationFile.SetField(TiffTag.YRESOLUTION, yresolution);

            // change orientation of the image
            //output.SetField(TiffTag.ORIENTATION, Orientation.RIGHTBOT);
            destinationFile.SetField(TiffTag.SAMPLESPERPIXEL, 1);

            // specify that it's a page within the multipage file
            destinationFile.SetField(TiffTag.SUBFILETYPE, FileType.PAGE);
            // specify the page number
            destinationFile.SetField(TiffTag.PAGENUMBER, pageNumber, totalPages);



            for (var j = 0; j < height; ++j)
                destinationFile.WriteScanline(buffer[j], j);


            destinationFile.RewriteDirectory();

        }



        public static byte[] ConvertPdfToByteArray(string sourceFile)
        {
            new License().SetLicense("Aspose.Total.lic");
            var pdfDocument = new Document(sourceFile);

            // Create Resolution object
            var resolution = new Resolution(200);

            // Create TiffSettings object
            var tiffSettings = new TiffSettings
            {
                Compression = CompressionType.CCITT4,
                Depth = ColorDepth.Default,
                Shape = ShapeType.Portait,
                SkipBlankPages = false
            };

            // Create TIFF device
            var tiffDevice = new TiffDevice(resolution, tiffSettings);


            byte[] results;
            using (var mem = new MemoryStream())
            {
                tiffDevice.Process(pdfDocument, mem);
                mem.Position = 0;
                results = mem.ToArray();
            }

            return results;

        }

        public static byte[] ConvertJpgToPdfToByteArray(string sourceFile)
        {
            byte[] results = null;
            var document = new iTextSharp.text.Document();
            
            using (var mem = new FileStream("z:\\test.pdf", FileMode.Create, FileAccess.Write, FileShare.None))
            //using (var mem = new MemoryStream())
            {
                iTextSharp.text.pdf.PdfWriter.GetInstance(document, mem);
                document.Open();
                using (var imageStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var image = iTextSharp.text.Image.GetInstance(imageStream);
                    if (image.Height > image.Width)
                    {
                        //Maximum height is 800 pixels.
                        float percentage = 0.0f;
                        percentage = 700 / image.Height;
                        image.ScalePercent(percentage * 100);
                    }
                    else
                    {
                        //Maximum width is 600 pixels.
                        float percentage = 0.0f;
                        percentage = 540 / image.Width;
                        image.ScalePercent(percentage * 100);
                    }


                    image.ScaleToFitHeight = true;
                    document.Add(image);
                    

                }
                //mem.Position = 0;
                //results = mem.ToArray();
                document.Close();
            }


            return results;
        }
    }
}
