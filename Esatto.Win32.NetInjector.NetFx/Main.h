#pragma once

extern "C" __declspec(dllexport) HRESULT __stdcall RunAssemblyRemote(
    HWND hWndTarget,
    LPCWSTR pwzPreferredVersion,
    LPCWSTR pwzAssemblyPath,
    LPCWSTR pwzTypeName,
    LPCWSTR pwzMethodName,
    LPCWSTR pwzArgument
);

extern "C" __declspec(dllexport) LRESULT __stdcall MessageHookProc(int nCode, WPARAM wparam, LPARAM lparam);

extern "C" __declspec(dllexport) HRESULT __stdcall RunAssembly(
    LPCWSTR pwzPreferredVersion,
    LPCWSTR pwzAssemblyPath,
    LPCWSTR pwzTypeName,
    LPCWSTR pwzMethodName,
    LPCWSTR pwzArgument
);

extern unsigned int WM_GOBABYGO;