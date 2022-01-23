using System;
using Amazon;
using Serilog;
using Amazon.S3;
using System.IO;
using Amazon.Runtime;
using Amazon.S3.Model;
using BullyBot.Models;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BullyBot.ExerciseDataSources;
public class AWSFileDownload : IExerciseDataSource
{
    private readonly GetObjectRequest _Target;
    private readonly AmazonS3Client _Client;

    #region Initialisation
    public AWSFileDownload(IConfiguration Config)
    {
        // Check file information
        var Filename = Config?.GetValue<string>("File");
        var Bucket = Config?.GetValue<string>("Bucket");
        if( string.IsNullOrEmpty(Filename) || string.IsNullOrEmpty(Bucket) )
        {
            throw new Exception("Invalid AWS Configuration");
        }
        
        // Retrieve the AWS Configuration
        var AWSRegion = RegionEndpoint.GetBySystemName(Config?.GetValue<string>("Region"));
        var AWSSecret = Config.GetValue<string>("Secret");
        var AWSKeyID = Config.GetValue<string>("KeyID");
        
        // Check the config is valid
        if( AWSRegion == null || string.IsNullOrEmpty(AWSSecret) || string.IsNullOrEmpty(AWSKeyID) )
        {
            throw new Exception("Invalid AWS Configuration");
        }
        
        // Create AWS Config Instance
        _Client = new AmazonS3Client(new BasicAWSCredentials(AWSKeyID, AWSSecret), AWSRegion);
        _Target = new GetObjectRequest
        {
            BucketName = Bucket,
            Key = Filename
        };
        
        AWSSecret = null;
        AWSKeyID = null;
    }
    #endregion Initialisation
    
    #region Public Methods
    public async Task<ExerciseDataModel> DownloadData()
    {
        try
        {
            // Request file from AWS
            var FileResponse = await _Client.GetObjectAsync(_Target);
            using var TestStream = FileResponse.ResponseStream;

            // Parse JSON into structure
            var StreamRead = new StreamReader(TestStream);
            var FileData = await StreamRead.ReadToEndAsync();
            return JsonConvert.DeserializeObject<ExerciseDataModel>(FileData);
        }
        catch( Exception Ex )
        {
            Log.Error(Ex, "An error occoured downloading data from the S3 Bucket");
            return null;
        }
    }
    #endregion Public Methods
}