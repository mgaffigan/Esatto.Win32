#include <variant>
#include <string>
#include <wil\stl.h>
#include <wil\com.h>
#include <wil\win32_helpers.h>
#include <MetaHost.h>
#pragma comment(lib, "mscoree.lib")
#include <corerror.h>
#include "main.h"

HRESULT GetRuntime(LPCWSTR pwzPreferredVersion, wil::com_ptr<ICLRRuntimeHost>& result);
HRESULT GetLoadedNetCoreRuntime(LPCWSTR pwzPreferredVersion, wil::com_ptr<ICLRRuntimeHost>& result);

extern "C" __declspec(dllexport) HRESULT __stdcall RunAssembly(
	LPCWSTR pwzPreferredVersion,
	LPCWSTR pwzAssemblyPath,
	LPCWSTR pwzTypeName,
	LPCWSTR pwzMethodName,
	LPCWSTR pwzArgument
) {
	std::wstring preferredVersion = pwzPreferredVersion ? pwzPreferredVersion : L"";
	wil::com_ptr<ICLRRuntimeHost> host;
	if (preferredVersion == L"netcore") {
		RETURN_IF_FAILED(GetLoadedNetCoreRuntime(pwzPreferredVersion, host));
	}
	else {
		RETURN_IF_FAILED(GetRuntime(pwzPreferredVersion, host));
	}

	DWORD returnValue = 0;
	RETURN_IF_FAILED(host->ExecuteInDefaultAppDomain(pwzAssemblyPath, pwzTypeName, pwzMethodName, pwzArgument, &returnValue));
	// returnValue is unused

	return S_OK;
}

HRESULT GetRuntime(LPCWSTR pwzPreferredVersion, wil::com_ptr<ICLRRuntimeHost>& result) {
	wil::com_ptr<ICLRMetaHost> metaHost;
	RETURN_IF_FAILED(CLRCreateInstance(CLSID_CLRMetaHost, IID_PPV_ARGS(&metaHost)));

	wil::com_ptr<IEnumUnknown> enumerator;
	RETURN_IF_FAILED(metaHost->EnumerateLoadedRuntimes(GetCurrentProcess(), &enumerator));

	wil::com_ptr<ICLRRuntimeInfo> currentInfo;
	std::wstring highestVersion;
	for (const wil::com_ptr<IUnknown>& runtimeInfoUnk : wil::make_range(enumerator.get())) {
		auto runtimeInfo = runtimeInfoUnk.query<ICLRRuntimeInfo>();

		std::wstring version;
		RETURN_IF_FAILED(wil::AdaptFixedSizeToAllocatedResult(version,
			[&](_Out_writes_(valueLength) PWSTR value, size_t valueLength, _Out_ size_t* valueLengthNeededWithNul) -> HRESULT {
				DWORD length = static_cast<DWORD>(valueLength);
				auto hr = runtimeInfo->GetVersionString(value, &length);
				*valueLengthNeededWithNul = length;
				return hr;
			}));

		if (pwzPreferredVersion) {
			if (version.starts_with(pwzPreferredVersion)) {
				// if prefix match, use
				currentInfo = runtimeInfo;
				break;
			}
		}
		else {
			if (version > highestVersion) {
				currentInfo = runtimeInfo;
				highestVersion = version;
			}
		}
	}

	if (!currentInfo.get()) {
		RETURN_HR_MSG(COR_E_ARGUMENTOUTOFRANGE, "Runtime not loaded");
	}

	RETURN_IF_FAILED(currentInfo->GetInterface(CLSID_CLRRuntimeHost, IID_PPV_ARGS(&result)));
	return S_OK;
}

typedef HRESULT(STDAPICALLTYPE* FnGetNETCoreCLRRuntimeHost)(REFIID riid, void** pUnk);

HRESULT GetLoadedNetCoreRuntime(LPCWSTR pwzPreferredVersion, wil::com_ptr<ICLRRuntimeHost>& result) {
	// Note: This uses API that is not yet deprecated, but is heading that way.  Work is underway
	// to add an API to the .NET Core hosting API to replace this.  As of 2024-04-28, no such API
	// has been added.
	// 
	// It sounds like the long-term will be:
	// 1. To statically link nethost.lib
	// 2. Use the nethost.lib API to locate hostfxr.dll
	// 3. Load hostfxr.dll
	// 4. Call hostfxr_get_runtime_delegate(nullptr, hdt_load_assembly_and_get_function_pointer)
	// 5. Call p_hdt_load_assembly_and_get_function_pointer(...) to run the assembly
	// 
	// This is a lot more work than the current approach, but it is the future.  It also moves to a
	// length-counted entrypoint (`static int Main(nint arg, int cbArg)` instead of `int main(string)`)
	// 
	// See https://github.com/dotnet/runtime/issues/52688 for discussion on removal.
	// See https://github.com/dotnet/runtime/blob/main/docs/design/features/native-hosting.md#calling-managed-function-net-5-and-above for eventual replacement.

	// There can only be one CoreCLR runtime in a process
	wil::unique_hmodule coreCLRModule(::GetModuleHandle(L"coreclr.dll"));
	RETURN_HR_IF_NULL(COR_E_FILENOTFOUND, coreCLRModule.get());

	// Locate GetCLRRuntimeHost
	const auto pfnGetCLRRuntimeHost = reinterpret_cast<FnGetNETCoreCLRRuntimeHost>(::GetProcAddress(coreCLRModule.get(), "GetCLRRuntimeHost"));
	RETURN_HR_IF_NULL(COR_E_ENTRYPOINTNOTFOUND, pfnGetCLRRuntimeHost);

	RETURN_IF_FAILED(pfnGetCLRRuntimeHost(IID_PPV_ARGS(&result)));
	return S_OK;
}