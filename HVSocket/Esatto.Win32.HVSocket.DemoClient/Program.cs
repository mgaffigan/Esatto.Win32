using Esatto.Win32.HVSocket;

using var httpClient = HyperVSocketHttpClient.Create(
    Guid.Parse("e0e16197-dd56-4a10-9195-5ee7a155a838"),
    Guid.Parse("C7240163-6E2B-4466-9E41-FF74E7F0DE48"));

var response = await httpClient.GetStringAsync("/weatherforecast");
Console.WriteLine(response);
