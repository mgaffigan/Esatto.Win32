namespace Esatto.Win32.HVSocket;

public static class HyperVSocketHttpClient
{
    /// <summary>
    /// Creates a new instance of <see cref="HttpClient"/> configured for the specified service.
    /// </summary>
    /// <param name="serviceId">The unique identifier of the service for which the <see cref="HttpClient"/> is being created.</param>
    /// <returns>A configured <see cref="HttpClient"/> instance for the specified service.</returns>
    public static HttpClient Create(Guid serviceId)
        => Create(Guid.Parse("a42e7cda-d03f-480c-9cc2-a4de20abb878") /* HV_GUID_PARENT */, serviceId);

    /// <summary>
    /// Creates a new <see cref="HttpClient"/> instance configured to communicate with a specific virtual machine and
    /// service.
    /// </summary>
    /// <remarks>The returned <see cref="HttpClient"/> is configured with a custom <see
    /// cref="HttpMessageHandler"/>      to enable communication over a Hyper-V socket. The base address is a dummy URI
    /// constructed      using the provided <paramref name="vmId"/> and <paramref name="serviceId"/>.</remarks>
    /// <param name="vmId">The unique identifier of the virtual machine.</param>
    /// <param name="serviceId">The unique identifier of the service within the virtual machine.</param>
    /// <returns>An <see cref="HttpClient"/> instance with a base address set to a URI derived from the specified <paramref
    /// name="vmId"/> and <paramref name="serviceId"/>.</returns>
    public static HttpClient Create(Guid vmId, Guid serviceId)
    {
        return new HttpClient(HyperVSocketHttpHandler.Create(vmId, serviceId))
        {
            // no real host resolution here, so we use a dummy URI
            BaseAddress = new Uri($"http://{vmId:n}.{serviceId:n}/"),
        };
    }
}
