using System;
using ClientManager;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ClientManager
{
    public class AzureManager
    {
        private CloudStorageAccount csAccount;
        private CloudBlobClient cbClient;
        private CloudBlobContainer container;

        public AzureManager()
        {
            csAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting(Constants.stringName));
            cbClient = csAccount.CreateCloudBlobClient();
        }

        public void createContainer()
        {
            container = blobClient.GetContainerReference(Constants.blobContainerName);
            if (container.CreateIfNotExists())
            {
                container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            }
        }

        public void uploadCloud(String blobName)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            // Create or overwrite the "myblob" blob with contents from a local file.
            using (var fileStream = System.IO.File.OpenRead(@"path\myfile"))
            {
                blockBlob.UploadFromStream(fileStream);
            }
        }

    }
}