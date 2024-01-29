var builder = DistributedApplication.CreateBuilder(args);
/*
var keycloakServer = builder.AddKeycloakContainer("keyCloak", "Host=192.168.55.3;Port=5433;username=postgres;Password=Test2021;Database=keycloakDB");

var catalogDb = builder.AddPostgres("postgres").WithPgAdmin().AddDatabase("catalogdb");

var basketCache = builder.AddRedis("basketcache");

var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
                     .WithReference(catalogDb)
                     .WithReplicas(2);

var messaging = builder.AddRabbitMQ("messaging");

var basketService = builder.AddProject("basketservice", @"..\BasketService\BasketService.csproj")
                    .WithReference(basketCache)
                    .WithReference(messaging);

builder.AddProject<Projects.MyFrontend>("frontend")
       .WithReference(basketService)
       .WithReference(keycloakServer)
       .WithReference(catalogService.GetEndpoint("http"));

builder.AddProject<Projects.OrderProcessor>("orderprocessor")
       .WithReference(messaging)
       .WithLaunchProfile("OrderProcessor");

builder.AddProject<Projects.ApiGateway>("apigateway")
       .WithReference(basketService)
       .WithReference(catalogService);

builder.AddProject<Projects.CatalogDb>("catalogdbapp")
       .WithReference(catalogDb);
*/
//uilder.AddPostgres("postgres");//.WithEndpoint(name:"postgres").AddDatabase("keycloakDB");
/*
var postgresContainer = builder.AddPostgresContainer("postgresContainer", port:45555).WithEnvironment("POSTGRES_DB","keycloakDB");
var keycloakDB = postgresContainer.AddDatabase("keycloakDB");
*/
var postgres = builder.AddPostgresContainer("postgres", port:45555).WithEnvironment("POSTGRES_DB","keycloakDB");
var keycloakDB = postgres.AddDatabase("keycloakDB");

builder.AddKeycloakContainer("keyCloak").WithReference(keycloakDB);

builder.Build().Run();
