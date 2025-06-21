using Esatto.Win32.HVSocket;

using var httpClient = HyperVSocketHttpClient.Create(Guid.Parse("C7240163-6E2B-4466-9E41-FF74E7F0DE47"));

var response = await httpClient.GetStringAsync("/weatherforecast");
Console.WriteLine(response);
