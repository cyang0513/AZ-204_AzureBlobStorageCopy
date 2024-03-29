﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Configuration;

namespace AzureBlobStorage
{
   class Program
   {
      static IConfiguration m_Config;

      static void Main(string[] args)
      {
         Console.WriteLine("CHYA Azure Blob copy test...");
         m_Config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

         var destContainerName = m_Config.GetSection("DestinationContainerName").Value;
         var sourceContainerName = m_Config.GetSection("SourceContainerName").Value;
         var sasConnStr = m_Config.GetSection("SASConnStr").Value;

         var blobService = new BlobServiceClient(sasConnStr);

         var source = blobService.GetBlobContainerClient(sourceContainerName);

         var destination = blobService.GetBlobContainerClient(destContainerName);

         CopyBlob(source, destination);

         Console.ReadKey();

      }

      static void CopyBlob(BlobContainerClient source, BlobContainerClient target)
      {
         var blobFileSource = source.GetBlobClient("2020ScrumGuideUS-210114-121334.pdf");
         var blobFileTarget = target.GetBlobClient("2020ScrumGuide.pdf");

         var leaseClient = blobFileSource.GetBlobLeaseClient();
         try
         {

            var resp = leaseClient.Acquire(new TimeSpan(-1));

            Console.WriteLine("Lease Id: " + leaseClient.LeaseId);
            Console.WriteLine("Lease state: " + blobFileSource.GetProperties().Value.LeaseState);
            Console.WriteLine("Lease status: " + blobFileSource.GetProperties().Value.LeaseStatus);
            Console.WriteLine("Lease duration " + blobFileSource.GetProperties().Value.LeaseDuration);

            var copy = blobFileTarget.StartCopyFromUriAsync(blobFileSource.Uri);

            Task.WaitAny(copy);

            Console.WriteLine("Lease state: " + blobFileSource.GetProperties().Value.LeaseState);
            Console.WriteLine("Lease status: " + blobFileSource.GetProperties().Value.LeaseStatus);

            Console.WriteLine("File copied: " + copy.Result.GetRawResponse());

            var tier = blobFileSource.SetAccessTier(AccessTier.Cool, new BlobRequestConditions()
                                                                     {
                                                                        LeaseId = leaseClient.LeaseId
                                                                     });

            Console.WriteLine("Source Tier changed: " + tier.Status);

         }
         catch (Exception e)
         {
            Console.WriteLine(e);
            throw;
         }
         finally
         {
            leaseClient.Release();
            Console.WriteLine("Lease state: " + blobFileSource.GetProperties().Value.LeaseState);
            Console.WriteLine("Lease status: " + blobFileSource.GetProperties().Value.LeaseStatus);
         }

      }

   }
}
