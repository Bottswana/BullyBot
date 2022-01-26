using System;
using Serilog;
using System.IO;
using System.Net;
using System.Text;
using BullyBot.Models;
using System.Net.Http;
using Newtonsoft.Json;
using BullyBot.Classes;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace BullyBot.ExerciseDataSources;
public class FitBitDownload : IExerciseDataSource
{
    private const string BASE_URL = "https://api.fitbit.com";
    private static string _AccessTokenCache;
    private static string _RefreshToken;
    private static string _ClientToken;

    #region Initialisation
    public FitBitDownload(IConfiguration Config)
    {
        // Check Refresh and Client Token
        _ClientToken = Config?.GetValue<string>("ClientToken");
        _RefreshToken = Config?.GetValue<string>("RefreshToken");
        if( string.IsNullOrEmpty(_RefreshToken) || string.IsNullOrEmpty(_ClientToken) )
        {
            throw new Exception("Invalid FitBit Configuration");
        }
    }
    #endregion Initialisation
    
    #region Public Methods
    public async Task<ExerciseDataModel> DownloadData()
    {
        using var RequestClient = new HttpClient();
        if( string.IsNullOrEmpty(_AccessTokenCache) )
        {
            // Renew access token if we don't have one
            Log.Debug("Retrieving a Access Token from our cached Refresh Token");
            await _RenewAccessToken();
        }
        
        try
        {
            // Setup request headers
            RequestClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _AccessTokenCache);
            RequestClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Make Daily Activity request
            var RequestDateString = DateTime.Now.ToString("yyyy-MM-dd");
            var ActivityRequest = await RequestClient.GetStringAsync($"{BASE_URL}/1/user/-/activities/date/{RequestDateString}.json");
            if( !string.IsNullOrEmpty(ActivityRequest) )
            {
                // Calculate minutes from each activity
                long ActiveMinutes = 0;
                var JsonData = JsonConvert.DeserializeObject<FitBitDataModel.ActivityData>(ActivityRequest);
                foreach( var Activity in JsonData?.Activities ?? Array.Empty<FitBitDataModel.Activity>() )
                {
                    var NumberMinutes = (double)Activity.DurationMilliseconds / 60000;
                    ActiveMinutes += (long)Math.Ceiling(NumberMinutes);
                }
                
                // Return data
                return new ExerciseDataModel
                {
                    restingHeartRate = JsonData?.Summary?.HeartRate,
                    numberSteps = JsonData?.Summary?.Steps,
                    activeMinutes = ActiveMinutes,
                    uploadDate = Epoch.Now
                };
            }
            
            // No data to return
            return null;
        }
        catch( HttpRequestException Ex )
        {
            // Check if we are getting a 401 and already have a cached token (means it's expired)
            if( Ex.StatusCode == HttpStatusCode.Unauthorized && _AccessTokenCache != null )
            {
                // Clear the access token
                Log.Debug("Access token has expired, initiating refresh flow");
                _AccessTokenCache = null;
                
                // Retry the request
                return await DownloadData();
            }
            
            // Other error
            throw;
        }
    }
    #endregion Public Methods
    
    #region Private Methods
    private static async Task<bool> _RenewAccessToken()
    {
        using var RequestClient = new HttpClient();
        try
        {
            // Setup request headers
            RequestClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _ClientToken);
            RequestClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Create request content with headers
            var RequestContent = new StringContent($"grant_type=refresh_token&refresh_token={_RefreshToken}");
            RequestContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            // Make refresh request
            var refreshTokenRequest = await RequestClient.PostAsync($"{BASE_URL}/oauth2/token", RequestContent);
            if( refreshTokenRequest.IsSuccessStatusCode )
            {
                Log.Debug("Refresh token flow successful");
                var requestResponse = await refreshTokenRequest.Content.ReadAsStringAsync();
                var RefreshData = JsonConvert.DeserializeObject<FitBitDataModel.RefreshFlowResult>(requestResponse);
                if( RefreshData != null && !string.IsNullOrEmpty(RefreshData.AccessToken) && !string.IsNullOrEmpty(RefreshData.RefreshToken) )
                {
                    // Update configuration with refresh token
                    try
                    {
                        await _UpdateConfig(InitialisationHandler.LoadedConfiguration, _RefreshToken, RefreshData.RefreshToken);
                        #if DEBUG
                        try
                        {
                            // Ignore failures for development config
                            var DevelopmentConfig = InitialisationHandler.LoadedConfiguration.Replace(".json", ".development.json");
                            await _UpdateConfig(DevelopmentConfig, _RefreshToken, RefreshData.RefreshToken);
                        }
                        catch( Exception ) {}
                        #endif
                    }
                    catch( Exception Ex )
                    {
                        Log.Error(Ex, "Error updating configuration file. Refresh token will be lost on application quit!");
                    }
                    
                    // Update cache of tokens
                    Log.Debug("Access token retrieved successfully: {RefreshToken}", RefreshData.RefreshToken);
                    Log.Debug("Access token retrieved successfully: {AccessToken}", RefreshData.AccessToken);
                    _AccessTokenCache = RefreshData.AccessToken;
                    _RefreshToken = RefreshData.RefreshToken;
                    return true;
                }
            }
            
            // Failed to retrieve access token
            Log.Error("Failed to retrieve access token. Response data failed or was not what was expected");
            return false;
        }
        catch( Exception Ex )
        {
            Log.Error(Ex, "Error retrieving new Access Token");
            return false;
        }
    }
    
    private static async Task _UpdateConfig(string ConfigFile, string OldToken, string NewToken)
    {
        var FileData = await File.ReadAllTextAsync(ConfigFile);
        if( !string.IsNullOrEmpty(FileData) )
        {
            var NewFileData = FileData.Replace(OldToken, NewToken);
            if( !FileData.Equals(NewFileData) )
            {
                Log.Debug("Updating configuration file {Config}", ConfigFile);
                await File.WriteAllTextAsync(ConfigFile, NewFileData, Encoding.UTF8);
            }
        }
    }
    #endregion Private Methods
}