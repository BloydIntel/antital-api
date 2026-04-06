var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Antital_API>("api")
    .WithHttpHealthCheck("/healthz");

var ui = builder.AddJavaScriptApp("ui", "../../../React/antital-ui")
    .WithPnpm()
    .WithRunScript("dev")
    // Registers an HTTP endpoint in the model so the dashboard shows a URL/port.
    // Next.js reads PORT; without this, the app may still listen on 3000 but Aspire
    // does not track it as a resource endpoint.
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    // pnpm install runs without a TTY under Aspire; without CI=true it aborts with
    // ERR_PNPM_ABORTED_REMOVE_MODULES_DIR_NO_TTY when it needs to touch node_modules.
    .WithEnvironment("CI", "true")
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("http"))
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
