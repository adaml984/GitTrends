﻿using System;
using System.Threading.Tasks;
using Refit;
using GitTrends.Shared;

namespace GitTrends
{
    abstract class AzureFunctionsApiService : BaseMobileApiService
    {
        #region Constant Fields
        static readonly Lazy<IAzureFunctionsApi> _azureFunctionsApiClientHolder = new Lazy<IAzureFunctionsApi>(() => RestService.For<IAzureFunctionsApi>(CreateHttpClient(AzureConstants.AzureFunctionsApiUrl)));
        #endregion

        #region Properties
        static IAzureFunctionsApi AzureFunctionsApiClient => _azureFunctionsApiClientHolder.Value;
        #endregion

        #region Methods
        public static Task<GetGitHubClientIdDTO> GetGitHubClientId() => ExecuteMobilePollyFunction(() => AzureFunctionsApiClient.GetGitTrendsClientId());
        public static Task<GitHubToken> GenerateGitTrendsOAuthToken(GenerateTokenDTO generateTokenDTO) => ExecuteMobilePollyFunction(() => AzureFunctionsApiClient.GenerateGitTrendsOAuthToken(generateTokenDTO));
        #endregion
    }
}