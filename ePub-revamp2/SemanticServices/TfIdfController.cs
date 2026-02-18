using ePub2.SemanticServices;
using ManualTFIDFDemo.Services;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace YourNamespace.Controllers
{
    public class TfIdfSurfaceController : SurfaceController
    {
        // POST /TfIdfSurface/BuildVectors
        [HttpPost]
        public ActionResult BuildVectors()
        {
            try
            {
                // Step 1: Map file IDs to HTML file paths
                var files = new Dictionary<int, string>
                {
                    {1, @"C:\Users\pbbuser\Desktop\will-dnv3\brproc.html"},
                    {2, @"C:\Users\pbbuser\Desktop\will-dnv3\brproc1.html"},
                    {3, @"C:\Users\pbbuser\Desktop\will-dnv3\brproc3.html"},
                    {4, @"C:\Users\pbbuser\Desktop\will-dnv3\brproc4.html"},
                    {5, @"C:\Users\pbbuser\Desktop\will-dnv3\contact.html"},
                    {6, @"C:\Users\pbbuser\Desktop\will-dnv3\default.html"},
                    {7, @"C:\Users\pbbuser\Desktop\will-dnv3\estate.html"},
                    {8, @"C:\Users\pbbuser\Desktop\will-dnv3\FAQ.html"},
                    {9, @"C:\Users\pbbuser\Desktop\will-dnv3\fc-compare.html"},
                    {10, @"C:\Users\pbbuser\Desktop\will-dnv3\fees.html"},
                    {11, @"C:\Users\pbbuser\Desktop\will-dnv3\guideG-S.html"},
                    {12, @"C:\Users\pbbuser\Desktop\will-dnv3\pbtsb.html"},
                    {13, @"C:\Users\pbbuser\Desktop\will-dnv3\sampleforms.html"},
                    {14, @"C:\Users\pbbuser\Desktop\will-dnv3\willwrite.html"}
                };

                // Step 2: Extract text from HTML files
                var fileTexts = new Dictionary<int, string>();
                foreach (var kv in files)
                {
                    fileTexts[kv.Key] = HtmlTextExtractor.ReadHtmlFile(kv.Value);
                }

                // Step 3: Build TF-IDF vectors
                var tfidf = new TfIdfService();
                tfidf.Build(fileTexts);

                // Save vectors to .txt
                string vectorFile = @"C:\source\ePub-revamp2 - 1208 - gemini\ePub-revamp2\tfidf_vector.txt";
                FileVectorStorage.Save(vectorFile, tfidf.GetDocVectors(), tfidf.GetIdf());

                return Content("TF-IDF vectors built and saved successfully.");
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }
    }
}