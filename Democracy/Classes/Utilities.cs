using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace Democracy.Classes
{
    public class Utilities
    {
        /*public static void UploadPhoto(HttpPostedFileBase file)
        {
            //Upload image
            string path = String.Empty;
            string pic = String.Empty;

            if (file != null)
            {
                

                pic = Path.GetFileName(file.FileName);
                path = Path.Combine(Server.MapPath("~/Content/Photos"), pic);
                file.SaveAs(path);
                
                using (MemoryStream ms = new MemoryStream())
                {
                    file.InputStream.CopyTo(ms);
                    byte[] array = ms.GetBuffer();
                }
            }
        }*/

    }
}