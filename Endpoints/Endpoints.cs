﻿using Microsoft.AspNetCore.Mvc;
using Repository.App;
using System.Net;
using Repository.App;
using System.Reflection;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Hosting;
using System.IO.Pipelines;

namespace Repository.Endpoints
{
    public class Endpoints
    {
        public Endpoints(WebApplication app)
        {
            var _hostEnvironment = app.Environment;
            // ----------------- CONNECTION ----------------- //
            // To maintain connection
            app.MapGet("ping", (HttpContext httpContext) =>
            {
                return "pong";
            });

            // To retrieve configuration for the registrationprocess in ServiceRegistry
            app.MapGet("/configurations", (HttpContext httpContext) =>
            {
                return Registration.GetConfiguration();
            });


            // ----------------- DATA ----------------- //
            // To save incomming files (.png, .xes, .bpmn, .pnml etc)
            app.MapPost("/resources", (HttpRequest request) =>
            {
                return ResourceReceiver.SaveResource(request);
            })
            //.Accepts<IFormFile>("multipart/form-data")
            .Produces(200);

            // To retrieve/output a list of available resources
            app.MapGet("/resources", (HttpContext httpContext) =>
            {
                return ResourceRetriever.GetResourceList();
            });

            // To retrieve/output a list of available Visualization resources
            app.MapGet("/resources/visualizations", (HttpContext httpContext) =>
            {
                return ResourceRetriever.GetVisualizationList();
            });

            // To retrieve/output a list of available EventLog resources
            app.MapGet("/resources/eventlogs", (HttpContext httpContext) =>
            {
                return ResourceRetriever.GetEventLogList();
            });

            // To retrieve/output model representation (.bpmn, png etc) for the frontend
            app.MapGet("/resources/{resourceId}", (string resourceId) =>
            {
                return ResourceRetriever.GetResourceById(resourceId);
            });

            #region streamingAndTesting
            // To input a stream
            app.MapPost("/resources/stream", async (Stream body) =>
            {
                //string tempfile = CreateTempfilePath();
                //using var stream = File.OpenWrite(tempfile);
                //await body.CopyToAsync(stream);
            });
            static string CreateTempfilePath()
            {
                var filename = $"{Guid.NewGuid()}.tmp";
                var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "temp");
                if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

                return Path.Combine(directoryPath, filename);
            }


            // To output a stream
            app.MapGet("test-api", async (HttpClient httpClient) =>
            {
                var streamResponse = await httpClient.GetStreamAsync("posts");
                return Results.Stream(streamResponse, "application/json");
            });

            app.MapGet("/resources/stream/{resourceId}", (string resourceId) =>
            {
                string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");

                MetadataObject? metadataObject = DBManager.GetMetadataObjectById(resourceId);
                if (metadataObject == null) return Results.BadRequest("Invalid resource ID.");
                string pathToFileType = Path.Combine(pathToResources, metadataObject.FileType);
                string pathToFileExtension = Path.Combine(pathToFileType, metadataObject.FileExtension.ToUpper());
                string pathToFile = Path.Combine(pathToFileExtension, resourceId + "." + metadataObject.FileExtension);

                var filestream = File.OpenRead(pathToFile);
                return Results.File(filestream, contentType: "video/mp4", fileDownloadName: resourceId, enableRangeProcessing: true);
            });

            // Alternate approach to save incomming files. You can send any content type as string 
            app.MapPost("/resources/binary", async (HttpRequest request) =>
            {
                using var reader = new StreamReader(request.Body, System.Text.Encoding.UTF8);
                // Read the raw file as a `string`.
                string fileContent = await reader.ReadToEndAsync();
                // Do something with `fileContent`...
                return "File Was Processed Sucessfully!";
            });
            // Alternative way? For streaming?
            //app.MapGet("/resources/stream/{resourceName}", HttpResponseMessage (string resourceName) =>
            //{
            //    return ResourceRetriever.StreamResponse(resourceName);
            //});
            #endregion

            #region OldEndpoints
            // ----------------- Ugly endpoints ----------------- //
            // ----------------- CONNECTION ----------------- //
            // To maintain connection
            app.MapGet("/api/v1/system/ping", (HttpContext httpContext) =>
            {
                return "pong";
            });

            // To retrieve configuration for the registrationprocess in ServiceRegistry
            app.MapGet("/api/v1/configurations", (HttpContext httpContext) =>
            {
                return Registration.GetConfiguration();
            });


            // ----------------- DATA ----------------- //
            // To save incomming files (.png, .xes, .bpmn, .pnml etc)
            app.MapPost("/api/v1/resources", (HttpRequest request) =>
            {
                return ResourceReceiver.SaveResource(request);
            })
            .Produces(200);


            // To retrieve/output a list of available resources
            app.MapGet("/api/v1/resources", (HttpContext httpContext) =>
            {
                return ResourceRetriever.GetResourceListOld();
            });

            // To retrieve resource (any resource, .xes, .bpmn, .png etc)
            app.MapGet("/api/v1/resources/{resourceId}/content", (string resourceId) =>
            {
                return ResourceRetriever.GetResourceById(resourceId);
            });

            // To retrieve representation of resource - specifically for "buildResourceVisualization" in the old frontend
            //app.MapGet("/api/v1/resources/{resourceId}/view/{visualizationId}", async (HttpRequest request, string resourceId, string visualizationId) =>
            //{
            //    return ResourceRetriever.GetVisualizationById(request, resourceId, visualizationId);
            //});

            // Alternative way? For streaming?
            app.MapGet("/api/v1/resources/stream/{resourceName}", HttpResponseMessage (string resourceName) =>
            {
                return ResourceRetriever.StreamResponse(resourceName);
            });
            #endregion
        }
    }
}
