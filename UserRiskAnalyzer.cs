using Microsoft.IdentityServer.Public.ThreatDetectionFramework;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.IdentityServer.Public;
using System.Security.Claims;

namespace ThreatDetectionModule
{
    /// <summary>
    /// UserRiskAnalyzer is the main class implementing ThreatDetectionModule abstract class and IPreAuthenticationThreatDetectionModule and IPostAuthenticationThreatDetectionModule interfaces.
    /// This module will allow a "No risk" user to be able to login successfully, allow "Low risk" user to login after additional auth (MFA) and block "High risk" user
    /// </summary>
    public class UserRiskAnalyzer : Microsoft.IdentityServer.Public.ThreatDetectionFramework.ThreatDetectionModule, IPreAuthenticationThreatDetectionModule, IPostAuthenticationThreatDetectionModule
    {

        public override string VendorName => "Microsoft";
        public override string ModuleIdentifier => "UserRiskAnalyzer";

        /// <summary>
        /// ADFS calls this method while loading the module
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configData"></param>
        public override void OnAuthenticationPipelineLoad(ThreatDetectionLogger logger, ThreatDetectionModuleConfiguration configData)
        {
        }


        /// <summary>
        /// ADFS calls this method while unloading the module
        /// </summary>
        /// <param name="logger"></param>
        public override void OnAuthenticationPipelineUnload(ThreatDetectionLogger logger)
        {
        }

        
        /// <summary>
        /// ADFS calls this method when there is any change in the configuration. 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configData"></param>
        public override void OnConfigurationUpdate(ThreatDetectionLogger logger, ThreatDetectionModuleConfiguration configData)
        {            
        }

        public Task<ThrottleStatus> EvaluatePreAuthentication(ThreatDetectionLogger logger, RequestContext requestContext, SecurityContext securityContext, ProtocolContext protocolContext, IList<Claim> additionalClams)
        {

            try
            {
                RiskScore isRisky = RiskyUserHelper.GetRiskScore(securityContext.UserIdentifier);

                if (isRisky == RiskScore.High)
                {
                    logger?.WriteAdminLogErrorMessage($"EvaluatePreAuthentication: Blocked request for user {securityContext.UserIdentifier}");
                    return Task.FromResult<ThrottleStatus>(ThrottleStatus.Block);
                }
                logger?.WriteDebugMessage($"EvaluatePreAuthentication: Allowed request for user {securityContext.UserIdentifier}");
                return Task.FromResult<ThrottleStatus>(ThrottleStatus.Allow);


            }
            catch (Exception ex)
            {
                logger.WriteAdminLogErrorMessage(ex.ToString());
                throw;
            }

            throw new NotImplementedException();
        }

        Task<RiskScore> IPostAuthenticationThreatDetectionModule.EvaluatePostAuthentication(ThreatDetectionLogger logger, RequestContext requestContext, SecurityContext securityContext, ProtocolContext protocolContext, AuthenticationResult authenticationResult, IList<Claim> additionalClams)
        {
            try
            {
                RiskScore isRisky = RiskyUserHelper.GetRiskScore(securityContext.UserIdentifier);

                if (isRisky == RiskScore.High || isRisky == RiskScore.Medium)
                {
                    logger?.WriteAdminLogErrorMessage($"EvaluatePostAuthentication: Risk Score {isRisky}  returned for user {securityContext.UserIdentifier}");


                }
                else
                {
                    logger?.WriteDebugMessage($"EvaluatePostAuthentication: Risk Score {isRisky} returned for user {securityContext.UserIdentifier}");
                }
                return Task.FromResult<RiskScore>(isRisky);


            }
            catch (Exception ex)
            {
                logger.WriteAdminLogErrorMessage(ex.ToString());
                throw;
            }

            throw new NotImplementedException();
        }
    }
}
