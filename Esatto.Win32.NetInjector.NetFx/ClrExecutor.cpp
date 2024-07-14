#include <variant>
#include <string>

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <wil\stl.h>
#include <wil\com.h>
#include <wil\win32_helpers.h>
#include <MetaHost.h>
#pragma comment(lib, "mscoree.lib")
#include <corerror.h>
#include <hostfxr.h>
#include <coreclr_delegates.h>
#include "main.h"

HRESULT GetRuntime(std::wstring& preferredVersion, wil::com_ptr<ICLRRuntimeHost>& result);
HRESULT GetLoadedNetCoreRuntime(wil::com_ptr<ICLRRuntimeHost>& result);
HRESULT RunAssemblyWithHostfxr(LPCWSTR pwzPreferredVersion, LPCWSTR pwzAssemblyPath,
	LPCWSTR pwzTypeName, LPCWSTR pwzMethodName, LPCWSTR pwzArgument);

extern "C" __declspec(dllexport) HRESULT __stdcall RunAssembly(
	LPCWSTR pwzPreferredVersion,
	LPCWSTR pwzAssemblyPath,
	LPCWSTR pwzTypeName,
	LPCWSTR pwzMethodName,
	LPCWSTR pwzArgument
) {
	std::wstring preferredVersion = pwzPreferredVersion ? pwzPreferredVersion : L"";
	// Hostfxr uses a different calling API
	if (preferredVersion == L"hostfxr") {
		return RunAssemblyWithHostfxr(nullptr, pwzAssemblyPath, pwzTypeName, pwzMethodName, pwzArgument);
	}
	else if (preferredVersion.ends_with(L".runtimeconfig.json")) {
		return RunAssemblyWithHostfxr(pwzPreferredVersion, pwzAssemblyPath, pwzTypeName, pwzMethodName, pwzArgument);
	}

	// Use the ICLRRuntimeHost API
	wil::com_ptr<ICLRRuntimeHost> host;
	if (preferredVersion == L"netcore") {
		RETURN_IF_FAILED(GetLoadedNetCoreRuntime(host));
	}
	else {
		RETURN_IF_FAILED(GetRuntime(preferredVersion, host));
	}

	DWORD returnValue = 0;
	RETURN_IF_FAILED(host->ExecuteInDefaultAppDomain(pwzAssemblyPath, pwzTypeName, pwzMethodName, pwzArgument, &returnValue));
	// returnValue is unused

	return S_OK;
}

HRESULT FindRuntimeInList(IEnumUnknown* enumerator, wil::com_ptr<ICLRRuntimeInfo>& currentInfo, const std::wstring& preferredVersion) {
	std::wstring highestVersion;
	for (const wil::com_ptr<IUnknown>& runtimeInfoUnk : wil::make_range(enumerator)) {
		auto runtimeInfo = runtimeInfoUnk.query<ICLRRuntimeInfo>();

		std::wstring version;
		RETURN_IF_FAILED(wil::AdaptFixedSizeToAllocatedResult(version,
			[&](_Out_writes_(valueLength) PWSTR value, size_t valueLength, _Out_ size_t* valueLengthNeededWithNul) -> HRESULT {
				DWORD length = static_cast<DWORD>(valueLength);
				auto hr = runtimeInfo->GetVersionString(value, &length);
				*valueLengthNeededWithNul = length;
				return hr;
			}));

		if (!preferredVersion.empty()) {
			if (version.starts_with(preferredVersion)) {
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

	return S_OK;
}

HRESULT GetRuntime(std::wstring& preferredVersion, wil::com_ptr<ICLRRuntimeHost>& result) {
	wil::com_ptr<ICLRMetaHost> metaHost;
	RETURN_IF_FAILED(CLRCreateInstance(CLSID_CLRMetaHost, IID_PPV_ARGS(&metaHost)));

	wil::com_ptr<IEnumUnknown> enumerator;
	RETURN_IF_FAILED(metaHost->EnumerateLoadedRuntimes(GetCurrentProcess(), enumerator.put()));

	wil::com_ptr<ICLRRuntimeInfo> currentInfo;
	RETURN_IF_FAILED(FindRuntimeInList(enumerator.get(), currentInfo, preferredVersion));

	if (!currentInfo.get()) {
		RETURN_IF_FAILED(metaHost->EnumerateInstalledRuntimes(enumerator.put()));
		RETURN_IF_FAILED(FindRuntimeInList(enumerator.get(), currentInfo, preferredVersion));
	}

	if (!currentInfo.get()) {
		RETURN_HR_MSG(COR_E_ARGUMENTOUTOFRANGE, "Runtime not loaded");
	}

	RETURN_IF_FAILED(currentInfo->GetInterface(CLSID_CLRRuntimeHost, IID_PPV_ARGS(&result)));
	return S_OK;
}

typedef HRESULT(STDAPICALLTYPE* FnGetNETCoreCLRRuntimeHost)(REFIID riid, void** pUnk);

HRESULT GetLoadedNetCoreRuntime(wil::com_ptr<ICLRRuntimeHost>& result) {
	// Note: This uses API that is not yet deprecated, but is heading that way.
	// See https://github.com/dotnet/runtime/issues/52688 for discussion on removal.

	// There can only be one CoreCLR runtime in a process
	wil::unique_hmodule coreCLRModule(::GetModuleHandle(L"coreclr.dll"));
	RETURN_HR_IF_NULL(COR_E_FILENOTFOUND, coreCLRModule.get());

	// Locate GetCLRRuntimeHost
	const auto pfnGetCLRRuntimeHost = reinterpret_cast<FnGetNETCoreCLRRuntimeHost>(::GetProcAddress(coreCLRModule.get(), "GetCLRRuntimeHost"));
	RETURN_HR_IF_NULL(COR_E_ENTRYPOINTNOTFOUND, pfnGetCLRRuntimeHost);

	RETURN_IF_FAILED(pfnGetCLRRuntimeHost(IID_PPV_ARGS(&result)));
	return S_OK;
}

typedef int(__stdcall* get_hostfxr_path_fn)(
	char_t* buffer,
	size_t* buffer_size,
	const struct get_hostfxr_parameters* parameters);

struct hostfxr {
	hostfxr_get_runtime_delegate_fn get_runtime_delegate;
	hostfxr_initialize_for_runtime_config_fn initialize_for_runtime_config;

	HRESULT GetNetHostDll(std::wstring& path) {
		// Find nethost.dll based on our path
		HMODULE hModuleNetInjector = nullptr;
		RETURN_HR_IF(E_UNEXPECTED, !::GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, (LPCTSTR)&RunAssemblyWithHostfxr, &hModuleNetInjector));
		RETURN_IF_FAILED(wil::GetModuleFileNameW(hModuleNetInjector, path));

		// Replace Esatto.Win32.NetInjector.NetFx.dll with nethost.dll
		size_t lastSlash = path.find_last_of(L'\\');
		RETURN_HR_IF_MSG(E_UNEXPECTED, lastSlash == std::wstring::npos, "Could not retrieve path to Esatto.Win32.NetInjector.NetFx.dll");
		path.resize(lastSlash + 1);
		path += L"nethost.dll";

		return S_OK;
	}

	HRESULT Initialize() {
		std::wstring netHostPath;
		RETURN_IF_FAILED(GetNetHostDll(netHostPath));

		// Load nethost.dll
		wil::unique_hmodule nethostModule(::LoadLibraryW(netHostPath.c_str()));
		RETURN_HR_IF_NULL_MSG(COR_E_FILENOTFOUND, nethostModule.get(), "Could not load nethost.dll");
		get_hostfxr_path_fn get_hostfxr_path = reinterpret_cast<get_hostfxr_path_fn>(::GetProcAddress(nethostModule.get(), "get_hostfxr_path"));

		// Find hostfxr.dll
		wchar_t hostfxrPath[MAX_PATH];
		size_t hostfxrPathSize = ARRAYSIZE(hostfxrPath);
		RETURN_IF_FAILED_MSG((HRESULT)get_hostfxr_path(hostfxrPath, &hostfxrPathSize, nullptr), "Could not find hostfxr.dll");

		// We do not want to unload hostfxr, so this is not unique_hmodule
		HMODULE hostfxrModule = ::LoadLibraryW(hostfxrPath);
		RETURN_HR_IF_NULL_MSG(COR_E_FILENOTFOUND, hostfxrModule, "Could not load hostfxr.dll from '%ls'", hostfxrPath);

		get_runtime_delegate = reinterpret_cast<hostfxr_get_runtime_delegate_fn>(::GetProcAddress(hostfxrModule, "hostfxr_get_runtime_delegate"));
		RETURN_HR_IF_NULL(COR_E_ENTRYPOINTNOTFOUND, get_runtime_delegate);

		initialize_for_runtime_config = reinterpret_cast<hostfxr_initialize_for_runtime_config_fn>(::GetProcAddress(hostfxrModule, "hostfxr_initialize_for_runtime_config"));
		RETURN_HR_IF_NULL(COR_E_ENTRYPOINTNOTFOUND, initialize_for_runtime_config);

		return S_OK;
	}
};

std::wstring_view GetFileNameWithoutExtension(std::wstring_view path) {
	size_t lastSlash = path.find_last_of(L'\\');
	size_t lastDot = path.find_last_of(L'.');
	if (lastDot == std::wstring::npos || lastDot < lastSlash) {
		lastDot = path.size();
	}
	return path.substr(lastSlash + 1, lastDot - lastSlash - 1);
}

HRESULT RunAssemblyWithHostfxr(LPCWSTR pwzRuntimeConfig, LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName, LPCWSTR pwzArgument)
{
	hostfxr hostfxr;
	RETURN_IF_FAILED(hostfxr.Initialize());

	// Load the runtime
	hostfxr_handle inst = nullptr;
	if (pwzRuntimeConfig) {
		RETURN_IF_FAILED((HRESULT)hostfxr.initialize_for_runtime_config(pwzRuntimeConfig, nullptr, &inst));
	}

	load_assembly_and_get_function_pointer_fn load_assembly_and_get_function_pointer = nullptr;
	RETURN_IF_FAILED((HRESULT)hostfxr.get_runtime_delegate(inst, hdt_load_assembly_and_get_function_pointer,
		reinterpret_cast<void**>(&load_assembly_and_get_function_pointer)));
	RETURN_HR_IF_NULL(COR_E_ENTRYPOINTNOTFOUND, load_assembly_and_get_function_pointer);

	// Get the entry point
	// TypeName must be assembly qualified for some reason
	std::wstring typeName = pwzTypeName;
	typeName.append(L", ");
	typeName.append(GetFileNameWithoutExtension(pwzAssemblyPath));

	void* delegate = nullptr;
	RETURN_IF_FAILED((HRESULT)load_assembly_and_get_function_pointer(pwzAssemblyPath, typeName.c_str(),
		pwzMethodName, nullptr, nullptr, &delegate));
	RETURN_HR_IF_NULL(COR_E_ENTRYPOINTNOTFOUND, delegate);

	// Call the entry point
	component_entry_point_fn entryPoint = reinterpret_cast<component_entry_point_fn>(delegate);
	size_t argSize = pwzArgument ? (wcslen(pwzArgument) + 1) * sizeof(wchar_t) : 0;
	return (HRESULT)entryPoint(const_cast<wchar_t*>(pwzArgument), (int32_t)argSize);
}