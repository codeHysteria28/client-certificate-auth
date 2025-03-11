using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Azure.Security.KeyVault.Secrets;
using Azure.Security.KeyVault.Certificates;
using System.Security.Cryptography.X509Certificates;

try
{
    // initializing configuration builder
    var builder = new ConfigurationBuilder()
        .AddUserSecrets<Program>();

    // building configuration and retrieving user secrets
    var configuration = builder.Build();
    //string? userAssignedClientID = configuration["userAssignedClientID"];
    const string certName = "my-api-certificate";
    string keyVaultName = configuration["key_vault_name"];
    string keyVaultUri = $"https://{keyVaultName}.vault.azure.net";

    //var credential = new DefaultAzureCredential(
    //    new DefaultAzureCredentialOptions
    //    {
    //        ManagedIdentityClientId = userAssignedClientID,
    //    }
    //);

    var credential = new DefaultAzureCredential();

    // create a client
    var client = new CertificateClient(new Uri(keyVaultUri), credential);

    // retrieve the certificate
    Console.WriteLine($"Retrieving a certificate from {keyVaultName}...");
    var certificate = await client.GetCertificateAsync(certName);
    var secretClient = new SecretClient(new Uri(keyVaultUri), credential);
    var secret = await secretClient.GetSecretAsync(certificate.Value.Name);
    var pfxBytes = Convert.FromBase64String(secret.Value.Value);
    Console.WriteLine($"Certificate name: {certificate.Value.Name}");

    // convert the certificate to X509Certificate2
    var x509Certificate = new X509Certificate2(pfxBytes, (string)null, X509KeyStorageFlags.MachineKeySet);

    // create an HttpClientHandler and attach the certificate
    var handler = new HttpClientHandler();
    handler.ClientCertificates.Add(x509Certificate);

    // create an HttpClient usign the handler
    using var httpClient = new HttpClient(handler);

    // make the GET request using the configured HttpClient
    await GetApimAsync(httpClient);
}
catch (TaskCanceledException)
{
    Console.WriteLine("The operation timed out.");
}
catch (AuthenticationFailedException ex)
{
    Console.WriteLine(ex.Message);
}

async Task GetApimAsync(HttpClient httpClient)
{
    Console.WriteLine("");
    Console.WriteLine("Sending a HTTP GET request to APIM instance ...");
    using HttpResponseMessage response = await httpClient.GetAsync("https://secureapim.azure-api.net/echo/resource?param1=sample");
    response.EnsureSuccessStatusCode();
    string responseBody = await response.Content.ReadAsStringAsync();
    Console.WriteLine(responseBody);
}