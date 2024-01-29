// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

public static class KeycloakBuilderExtensions
{
    private const int DefaultContainerPort = 8080;

    public static IResourceBuilder<KeycloakContainerResource> AddKeycloakContainer(
        this IDistributedApplicationBuilder builder,
        string name,
        string? connectionString = null,
        string? database = null,
        int? port = null)
    {
        var keycloakContainer = new KeycloakContainerResource(name);
        var startCommand = builder.Environment.IsDevelopment() ? "start-dev" : "start";

        return builder
            .AddResource(keycloakContainer)
            .AddConnectionString(connectionString: connectionString, database: database)
            .WithManifestPublishingCallback(WriteKeycloakContainerToManifest)
            .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", port: port, containerPort: DefaultContainerPort))
            .WithAnnotation(new ContainerImageAnnotation { Image = "quay.io/keycloak/keycloak", Tag = "latest" })
            .WithEnvironment("KEYCLOAK_ADMIN", "admin")
            .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin")
            .WithEnvironment("KC_HEALTH_ENABLED", "true")
            .WithEnvironment("KC_METRICS_ENABLED", "true")
            .WithArgs(startCommand);
    }

    public static IResourceBuilder<KeycloakContainerResource> WithReference(
           this IResourceBuilder<KeycloakContainerResource> builder,
           IResourceBuilder<IResourceWithConnectionString> builderResourceWithConnectionString)
    {
        (builder as IResourceBuilder<IResourceWithEnvironment>).WithReference(builderResourceWithConnectionString);
        builder.WithEnvironment(context =>
        {
            var connectionString = context.EnvironmentVariables[$"ConnectionStrings__{builderResourceWithConnectionString.Resource.Name}"];
            //var connectionString = builderResourceWithConnectionString.Resource.GetConnectionString();
            AddConnectionString(context, connectionString);
        });
        return builder;
    }

    private static void WriteKeycloakContainerToManifest(this ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "keycloak.server.v0");
    }

    public static IResourceBuilder<KeycloakContainerResource> AddConnectionString(
        this IResourceBuilder<KeycloakContainerResource> builder,
        string? connectionString = null,
        string? database = null)
    {
        builder.WithEnvironment(context =>
        {
            AddConnectionString(context, connectionString: connectionString, database: database);
        });
        return builder;
    }

    private static void AddConnectionString(
        EnvironmentCallbackContext context,
        string? connectionString = null,
        string? database = null)
    {
        context.EnvironmentVariables["CONNEXION"] = connectionString!;
        if (connectionString is null)
        {
            return;
        }

        if (string.IsNullOrEmpty(database))
        {
            database = DetermineDatabase(connectionString);
        }
        context.EnvironmentVariables["KC_DB"] = database;
        DbConnectionStringBuilder dbConnexionBuilder = new DbConnectionStringBuilder();
        dbConnexionBuilder.ConnectionString = connectionString;
        switch (database)
        {
            case "mssql":
                AddSQLServerConnection(context, dbConnexionBuilder);
                break;
            case "mysql":
                AddMySQLConnection(context, dbConnexionBuilder);
                break;
            case "postgres":
                AddPostgreSQLConnection(context, dbConnexionBuilder);
                break;
            case "oracle":
                AddOracleConnection(context, dbConnexionBuilder);
                break;
            case "mariadb":
                AddMariaConnection(context, dbConnexionBuilder);
                break;
        }
        
    }

    private static string DetermineDatabase(string connectionString)
    {
        if (connectionString.ToLower().Contains("server="))
        {
            if (connectionString.ToLower().Contains("port="))
            {
                return "mysql";
            }
            else
            {
                return "mssql";
            }
        }
        else if (connectionString.ToLower().Contains("host="))
        {
            return "postgres";
        }
        else if (connectionString.ToLower().Contains("datasource"))
        {
            return "oracle";
        }

        return string.Empty;
    }

    private static void AddMariaConnection(EnvironmentCallbackContext context, DbConnectionStringBuilder dbConnexionBuilder)
    {
        var hosting = dbConnexionBuilder["Server"]?.ToString();
        var username = dbConnexionBuilder["User"]?.ToString();
        var password = dbConnexionBuilder["Password"]?.ToString();
        var portHosting = dbConnexionBuilder["Port"]?.ToString();
        var databaseName = dbConnexionBuilder["Database"]?.ToString();
        context.EnvironmentVariables["KC_DB_URL"] = $"jdbc:mariadb://{hosting!}:{portHosting}/{databaseName}";
        context.EnvironmentVariables["KC_DB_USERNAME"] = username!;
        context.EnvironmentVariables["KC_DB_PASSWORD"] = password!;
    }

    private static void AddOracleConnection(EnvironmentCallbackContext context, DbConnectionStringBuilder dbConnexionBuilder)
    {
        var hosting = dbConnexionBuilder["Data Source"]?.ToString();
        var username = dbConnexionBuilder["User Id"]?.ToString();
        var password = dbConnexionBuilder["Password"]?.ToString();
        var databaseName = dbConnexionBuilder["Database"]?.ToString();
        context.EnvironmentVariables["KC_DB_URL"] = $"jdbc:oracle:thin:@{hosting!}/{databaseName}";
        context.EnvironmentVariables["KC_DB_USERNAME"] = username!;
        context.EnvironmentVariables["KC_DB_PASSWORD"] = password!;
    }

    private static void AddSQLServerConnection(EnvironmentCallbackContext context, DbConnectionStringBuilder dbConnexionBuilder)
    {
        var hosting = dbConnexionBuilder["Server"]?.ToString();
        var username = dbConnexionBuilder["User Id"]?.ToString();
        var password = dbConnexionBuilder["Password"]?.ToString();
        var databaseName = dbConnexionBuilder["Database"]?.ToString();
        context.EnvironmentVariables["KC_DB_URL"] = $"jdbc:sqlserver://{hosting!};databaseName={databaseName}";
        context.EnvironmentVariables["KC_DB_USERNAME"] = username!;
        context.EnvironmentVariables["KC_DB_PASSWORD"] = password!;
    }

    private static void AddMySQLConnection(EnvironmentCallbackContext context, DbConnectionStringBuilder dbConnexionBuilder)
    {
        var hosting = dbConnexionBuilder["Server"]?.ToString();
        var username = dbConnexionBuilder["Uid"]?.ToString();
        var password = dbConnexionBuilder["Pwd"]?.ToString();
        var portHosting = dbConnexionBuilder["Port"]?.ToString();
        var databaseName = dbConnexionBuilder["Database"]?.ToString();
        context.EnvironmentVariables["KC_DB_URL"] = $"jdbc:mysql://{hosting!}:{portHosting}/{databaseName}";
        context.EnvironmentVariables["KC_DB_USERNAME"] = username!;
        context.EnvironmentVariables["KC_DB_PASSWORD"] = password!;
    }

    private static void AddPostgreSQLConnection(EnvironmentCallbackContext context, DbConnectionStringBuilder dbConnexionBuilder)
    {
        var hosting = dbConnexionBuilder["Host"]?.ToString();
        var username = dbConnexionBuilder["Username"]?.ToString();
        var password = dbConnexionBuilder["Password"]?.ToString();
        var portHosting = dbConnexionBuilder["Port"]?.ToString();
        var databaseName = dbConnexionBuilder["Database"]?.ToString();
        context.EnvironmentVariables["KC_DB_URL"] = $"jdbc:postgresql://{hosting!}:{portHosting}/{databaseName}";
        context.EnvironmentVariables["KC_DB_USERNAME"] = username!;
        context.EnvironmentVariables["KC_DB_PASSWORD"] = password!;
    }
}