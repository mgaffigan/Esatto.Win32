#include <string>
#include <sstream>
#include <memory>
#include <wil\com.h>
#include <wil\win32_helpers.h>
#include <wil\resource.h>
#include "Main.h"

unsigned int WM_GOBABYGO = ::RegisterWindowMessage(L"ItpWin32WindowsClrHook_GOBABYGO");

struct virtualallocex_ptr {
	HANDLE hProcess;
	void* ptr;
	virtualallocex_ptr(HANDLE hProcess, void* ptr) : hProcess(hProcess), ptr(ptr) { }
	~virtualallocex_ptr() { ::VirtualFreeEx(hProcess, ptr, 0, MEM_RELEASE); }

	virtualallocex_ptr(const virtualallocex_ptr&) = delete;
	virtualallocex_ptr(virtualallocex_ptr&&) = delete;
};

std::wstring BuildInvokeString(LPCWSTR pwzPreferredVersion, LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName, LPCWSTR pwzArgument);
HRESULT GetProcessFromHwnd(HWND hWndTarget, wil::unique_process_handle& hProcess, DWORD& threadId);
HRESULT AllocateInProcess(HANDLE hProcess, void* pBuff, size_t cbBuff, std::unique_ptr<virtualallocex_ptr>& pResult);

extern "C" __declspec(dllexport) HRESULT __stdcall RunAssemblyRemote(
	HWND hWndTarget,
	LPCWSTR pwzPreferredVersion,
	LPCWSTR pwzAssemblyPath,
	LPCWSTR pwzTypeName,
	LPCWSTR pwzMethodName,
	LPCWSTR pwzArgument
) {
	wil::unique_process_handle hProcess;
	DWORD threadId;
	RETURN_IF_FAILED(GetProcessFromHwnd(hWndTarget, hProcess, threadId));

	auto invokeString = BuildInvokeString(pwzPreferredVersion, pwzAssemblyPath, pwzTypeName, pwzMethodName, pwzArgument);
	std::unique_ptr<virtualallocex_ptr> pBuf;
	size_t cbBuf = invokeString.size() * sizeof(wchar_t);
	RETURN_IF_FAILED(AllocateInProcess(hProcess.get(), (void*)invokeString.c_str(), cbBuf, pBuf));

	wil::unique_hmodule hInstance;
	if (!::GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, (LPCTSTR)&MessageHookProc, &hInstance)) {
		RETURN_HR_MSG(HRESULT_FROM_WIN32(GetLastError()), "Could not get module handle");
	}

	//auto hInstance = wil::GetModuleInstanceHandle();
	wil::unique_hhook hook(::SetWindowsHookEx(WH_CALLWNDPROC, &MessageHookProc, hInstance.get(), threadId));
	if (!hook.get()) {
		RETURN_HR_MSG(HRESULT_FROM_WIN32(GetLastError()), "Failed to set hook");
	}

	if (0 != ::SendMessage(hWndTarget, WM_GOBABYGO, (WPARAM)pBuf->ptr, (LPARAM)cbBuf)) {
		RETURN_HR_MSG(E_FAIL, "Unexpected result from SendMessage");
	}

	HRESULT hr = S_OK;
	if (!::ReadProcessMemory(hProcess.get(), pBuf->ptr, &hr, sizeof(HRESULT), nullptr)) {
		RETURN_HR_MSG(HRESULT_FROM_WIN32(GetLastError()), "Could not read result");
	}

	return hr;
}

HRESULT GetProcessFromHwnd(HWND hWndTarget, wil::unique_process_handle& hProcess, DWORD& threadId) {
	DWORD processID = 0;
	threadId = ::GetWindowThreadProcessId(hWndTarget, &processID);
	if (!processID) {
		RETURN_HR_MSG(E_HANDLE, "Could not find hWnd");
	}

	hProcess = wil::unique_process_handle(::OpenProcess(PROCESS_ALL_ACCESS, FALSE, processID));
	if (!hProcess.get()) {
		RETURN_HR_MSG(E_ACCESSDENIED, "Could not open process of hWnd");
	}

	return S_OK;
}

HRESULT AllocateInProcess(HANDLE hProcess,
	void* pBuff, size_t cbBuff, std::unique_ptr<virtualallocex_ptr>& pResult
) {
	void* ptr = ::VirtualAllocEx(hProcess, nullptr, cbBuff, MEM_COMMIT, PAGE_READWRITE);
	if (!ptr) {
		RETURN_HR_MSG(E_OUTOFMEMORY, "Could not allocate in remote process");
	}
	auto temp = std::make_unique<virtualallocex_ptr>(hProcess, ptr);

	if (!::WriteProcessMemory(hProcess, ptr, pBuff, cbBuff, nullptr)) {
		RETURN_HR_MSG(HRESULT_FROM_WIN32(::GetLastError()), "Could not write process memory");
	}

	pResult.swap(temp);
	return S_OK;
}

std::wstring BuildInvokeString(
	LPCWSTR pwzPreferredVersion,
	LPCWSTR pwzAssemblyPath,
	LPCWSTR pwzTypeName,
	LPCWSTR pwzMethodName,
	LPCWSTR pwzArgument
) {
	std::wostringstream result;
	result << (pwzPreferredVersion ? pwzPreferredVersion : L"") << L'\0';
	result << pwzAssemblyPath << L'\0';
	result << pwzTypeName << L'\0';
	result << pwzMethodName << L'\0';
	result << (pwzArgument ? pwzArgument : L"") << L'\0';
	return result.str();
}
