using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtils
{
    public static class PdfHelper
    {
        public static bool MergePDFs(IEnumerable<string> fileNames, string targetPdf)
        {
            bool merged = true;
            using (FileStream stream = new FileStream(targetPdf, FileMode.Create))
            {
                Document document = new Document();
                PdfCopy pdf = new PdfCopy(document, stream);
                PdfReader reader = null;
                try
                {
                    document.Open();
                    foreach (string file in fileNames)
                    {
                        reader = new PdfReader(file);
                        pdf.AddDocument(reader);
                        reader.Close();
                    }
                }
                catch (Exception)
                {
                    merged = false;
                    if (reader != null)
                    {
                        reader.Close();
                    }
                }
                finally
                {
                    if (document != null)
                    {
                        document.Close();
                    }
                }
            }
            return merged;
        }

        public static bool MergePdfStreams(MemoryStream[] streams, string targetPdf)
        {
            byte[] bytes;

            try
            {
                //Create our final combined MemoryStream
                using (MemoryStream finalStream = new MemoryStream())
                {
                    //Create our copy object
                    PdfCopyFields copy = new PdfCopyFields(finalStream);

                    //Loop through each MemoryStream
                    foreach (MemoryStream ms in streams)
                    {
                        //Reset the position back to zero
                        ms.Position = 0;
                        //Add it to the copy object
                        copy.AddDocument(new PdfReader(ms));
                        //Clean up
                        ms.Dispose();
                    }
                    //Close the copy object
                    copy.Close();

                    //Get the raw bytes to save to disk
                    bytes = finalStream.ToArray();
                }

                //Write out the file to the desktop
                using (FileStream fs = new FileStream(targetPdf, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
