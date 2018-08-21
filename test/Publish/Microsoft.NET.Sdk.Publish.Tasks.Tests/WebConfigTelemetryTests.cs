﻿using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.NET.Sdk.Publish.Tasks;
using Xunit;

namespace Microsoft.NET.Sdk.Publish.Tasks.Tests
{
    public class WebConfigTelemetryTests
    {

#if NET46
        [Fact]
        public void WebConfigTransform_Finds_ProjectGuid_IfSolutionPathIsPassed()
        {
            // Arrange
            string solutionFileFullPath = GetSolutionFileFullPath();
            string projectFullPath = GetTestProjectsFullPath();

            // Act
            string projectGuid = WebConfigTelemetry.GetProjectGuidFromSolutionFile(solutionFileFullPath, projectFullPath);

            // Assert
            Assert.Equal("{66964EC2-712A-451A-AB4F-33F18D8F54F1}", projectGuid);
        }

        [Theory]
        [InlineData("*UnDefined*")]
        [InlineData("")]
        [InlineData(@"c:\AFolderThatDoesNotExist")]
        public void WebConfigTransform_Finds_ProjectGuid_IfProjectPathIsPassed(string solutionFilePath)
        {
            // Arrange
            string projectFullPath = GetTestProjectsFullPath();

            // Act
            string projectGuid = WebConfigTelemetry.GetProjectGuidFromSolutionFile(solutionFilePath, projectFullPath);

            // Assert
            Assert.Equal("{66964EC2-712A-451A-AB4F-33F18D8F54F1}", projectGuid);
        }
#endif
        [Theory]
        [InlineData("{66964EC2-712A-451A-AB4F-33F18D8F54F1}")]
        public void WebConfigTelemetry_SetsProjectGuidIfNotOptedOut(string projectGuid)
        {
            // Arrange
            XDocument transformedWebConfig = WebConfigTransform.Transform(null, "test.exe", configureForAzure: false, useAppHost: true, extension:".exe", aspNetCoreHostingModel:null, environmentName: null);
            Assert.True(XNode.DeepEquals(WebConfigTransformTemplates.WebConfigTemplate, transformedWebConfig));
            
            //Act 
            XDocument output = WebConfigTelemetry.AddTelemetry(transformedWebConfig, projectGuid, false, null, null);

            // Assert
            Assert.True(XNode.DeepEquals(WebConfigTransformTemplates.WebConfigTemplateWithProjectGuid, output));
        }

        [Theory]
        [InlineData("{66964EC2-712A-451A-AB4F-33F18D8F54F1}")]
        public void WebConfigTelemetry_DoesNotSetProjectGuidIfOptedOut_ThroughIgnoreProjectGuid(string projectGuid)
        {
            // Arrange
            XDocument transformedWebConfig = WebConfigTransform.Transform(null, "test.exe", configureForAzure: false, useAppHost: true, extension: ".exe", aspNetCoreHostingModel:null, environmentName: null);
            Assert.True(XNode.DeepEquals(WebConfigTransformTemplates.WebConfigTemplate, transformedWebConfig));

            //Act 
            XDocument output= WebConfigTelemetry.AddTelemetry(transformedWebConfig, projectGuid, true, null, null);

            // Assert
            Assert.True(XNode.DeepEquals(WebConfigTransformTemplates.WebConfigTemplate, output));
        }

        [Theory]
        [InlineData("{66964EC2-712A-451A-AB4F-33F18D8F54F1}")]
        public void WebConfigTelemetry_RemovesProjectGuidIfOptedOut_ThroughIgnoreProjectGuid(string projectGuid)
        {
            // Arrange
            XDocument transformedWebConfig = WebConfigTransform.Transform(null, "test.exe", configureForAzure: false, useAppHost: true, extension: ".exe", aspNetCoreHostingModel:null, environmentName: null);
            Assert.True(XNode.DeepEquals(WebConfigTransformTemplates.WebConfigTemplate, transformedWebConfig));

            // Adds Guid to the config
            XDocument transformedWebConfigWithGuid = WebConfigTelemetry.AddTelemetry(transformedWebConfig, projectGuid, false, null, null);
            Assert.True(XNode.DeepEquals(WebConfigTransformTemplates.WebConfigTemplateWithProjectGuid, transformedWebConfigWithGuid));

            //Act 
            XDocument output = WebConfigTelemetry.AddTelemetry(transformedWebConfigWithGuid, projectGuid, true, null, null);

            // Assert
            Assert.True(XNode.DeepEquals(WebConfigTransformTemplates.WebConfigTemplate, output));
        }

        private const string TelemetryOptout = "DOTNET_CLI_TELEMETRY_OPTOUT";
#if NET46
        [Fact]
        public void WebConfigTelemetry_FindsProjectGuid_IfCLIOptedOutEnvVariableIsNotSet()
        {
            // Arrange
            string projectFullPath = GetTestProjectsFullPath();
            XDocument transformedWebConfig = WebConfigTransform.Transform(null, "test.exe", configureForAzure: false, useAppHost: true, extension: ".exe", aspNetCoreHostingModel: null, environmentName: null);
            string previousValue = Environment.GetEnvironmentVariable(TelemetryOptout);

            //Act 
            Environment.SetEnvironmentVariable(TelemetryOptout, "0");
            XDocument output = WebConfigTelemetry.AddTelemetry(transformedWebConfig, null, false, null, projectFullPath);
            
            // Assert
            Assert.True(XNode.DeepEquals(WebConfigTransformTemplates.WebConfigTemplateWithProjectGuid, output));

            // Reset
            Environment.SetEnvironmentVariable(TelemetryOptout, previousValue);
        }
#endif
        [Fact]
        public void WebConfigTelemetry_DoesNotSearchForProjectGuid_IfCLIOptedOutEnvVariableIsSet()
        {
            // Arrange
            string projectFullPath = GetTestProjectsFullPath();
            XDocument transformedWebConfig = WebConfigTransform.Transform(null, "test.exe", configureForAzure: false, useAppHost: true, extension: ".exe", aspNetCoreHostingModel: null, environmentName: null);
            string previousValue = Environment.GetEnvironmentVariable(TelemetryOptout);

            //Act 
            Environment.SetEnvironmentVariable(TelemetryOptout, "1");
            XDocument output = WebConfigTelemetry.AddTelemetry(transformedWebConfig, null, false, null, projectFullPath);

            // Assert
            Assert.True(XNode.DeepEquals(WebConfigTransformTemplates.WebConfigTemplate, output));

            // Reset
            Environment.SetEnvironmentVariable(TelemetryOptout, previousValue);
        }

        private string GetSolutionFileFullPath()
        {
            DirectoryInfo current = new DirectoryInfo(AppContext.BaseDirectory);
            string solutionFullPath = null;
            while (current != null)
            {
                solutionFullPath = Path.Combine(current.FullName, "Microsoft.NET.Sdk.Web.sln");
                if (File.Exists(solutionFullPath))
                {
                    break;
                }

                current = current.Parent;
            }

            return solutionFullPath;
        }

        private string GetTestProjectsFullPath()
        {
            string solutionFileFullPath = GetSolutionFileFullPath();
            return Path.Combine(Path.GetDirectoryName(solutionFileFullPath), "test", "publish", "Microsoft.NET.Sdk.Publish.Tasks.Tests", "Microsoft.NET.Sdk.Publish.Tasks.Tests.csproj");
        }
    }
}
