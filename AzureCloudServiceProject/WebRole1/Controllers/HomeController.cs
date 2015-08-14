using System.Web.Mvc;
using AzureStorageManagerLibrary;
using System.IO;
using System;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Diagnostics;

namespace WebRole1.Controllers
{
    public class HomeController : Controller
    {
        public HomeController()
        {
            asManager = new AzureStorageManager(RoleEnvironment.GetConfigurationSettingValue(Constants.stringName));
        }
        private AzureStorageManager asManager;

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public async Task<ActionResult> Upload()
        {
            return View((await asManager.generateResizedImageContainer()).ListBlobs().Select(p => p.Uri.ToString()).ToList());
        }

        [HttpPost]
        public async Task<ActionResult> Upload(HttpPostedFileBase image)
        {
            if (image != null && image.ContentLength > 0)
            {
                CloudBlobContainer container = asManager.generateOriginalImageContainer();
                CloudBlockBlob blob = container.GetBlockBlobReference(FileNameCorrector.makeValidFileName(image.FileName));
                if (blob.Exists())
                    blob = container.GetBlockBlobReference(FileNameCorrector.makeValidFileName(Path.GetFileNameWithoutExtension(image.FileName) + Guid.NewGuid().ToString() + Path.GetExtension(image.FileName)));
                await blob.UploadFromStreamAsync(image.InputStream);
                asManager.insertQueue(new MyImage()
                {
                    fileName = FileNameCorrector.makeValidFileName(Path.GetFileName(image.FileName)),
                    height = Constants.imageHeight,
                    width = Constants.imageWidth,
                    timeStamp = DateTime.UtcNow,
                    Image = (asManager.generateOriginalImageContainer()).ListBlobs().
                                First(p => p.Uri.ToString().Contains(FileNameCorrector.
                                makeValidFileName(Path.GetFileName(image.FileName)))).Uri.ToString()
                });
            }
            return RedirectToAction("Upload");
        }
    }
}