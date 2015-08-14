using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using System.IO;
using System.Drawing;

namespace AzureStorageManagerLibrary
{
    public class AzureStorageManager
    {
        private CloudStorageAccount csAccount;
        private CloudBlobClient cbClient;
        private CloudQueueClient cqClient;

        private CloudQueue cQueue;

        public AzureStorageManager(String accountSettings)
        {
            csAccount = CloudStorageAccount.Parse(accountSettings);
            createClients();
            cQueue = generateQueue();
        }

        public void createClients()
        {
            cbClient = csAccount.CreateCloudBlobClient();
            cqClient = csAccount.CreateCloudQueueClient();
        }

        public async Task<CloudBlobContainer> generateResizedImageContainer()
        {
            CloudBlobContainer container = cbClient.GetContainerReference(Constants.resizedBlobContainerName);

            if (await container.CreateIfNotExistsAsync())
                container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            return container;
        }

        public CloudBlobContainer generateOriginalImageContainer()
        {
            CloudBlobContainer container = cbClient.GetContainerReference(Constants.originalBlobContainerName);
            
            if (container.CreateIfNotExists())
                container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            return container;
        }

        public CloudQueue generateQueue()
        {
            CloudQueue cQueue = cqClient.GetQueueReference(Constants.queueName);
            cQueue.CreateIfNotExistsAsync();
            return cQueue;
        }

        public void insertQueue(MyImage image)
        {
            CloudQueueMessage message = new CloudQueueMessage(JsonConvert.SerializeObject(image));
            cQueue.AddMessageAsync(message);
        }

        public async Task runQueue()
        {
            foreach (CloudQueueMessage message in cQueue.GetMessages(5))
            {
                //Deserialize
                MyImage img = JsonConvert.DeserializeObject<MyImage>(message.AsString);

                //Retrieve reference to a blob
                CloudBlockBlob blobImage = generateOriginalImageContainer().
                    GetBlockBlobReference(FileNameCorrector.makeValidFileName(img.fileName));

                using (var st = new MemoryStream())
                {
                    //Download to stream
                    await blobImage.DownloadToStreamAsync(st);

                    //Resize image
                    Bitmap newImage = ImageResizeController.ResizeImage(Image.FromStream(st), Constants.imageWidth, Constants.imageHeight);

                    //Generate container for resized images
                    CloudBlobContainer resizedImageContainer = await generateResizedImageContainer();

                    //Retrieve reference to a blob
                    CloudBlockBlob blob = resizedImageContainer.GetBlockBlobReference(FileNameCorrector.makeValidFileName(img.fileName));

                    if (blob.Exists())
                        blob = resizedImageContainer.GetBlockBlobReference(FileNameCorrector.makeValidFileName(Path.GetFileNameWithoutExtension(img.fileName) + Guid.NewGuid().ToString() + Path.GetExtension(img.fileName)));

                    //Convert image to byte
                    byte[] data = ImageResizeController.ImageToByte(newImage);

                    await blob.UploadFromByteArrayAsync(data, 0, data.Length);

                    await cQueue.DeleteMessageAsync(message);
                }
            }
            await cQueue.ClearAsync();
        }
    }
}