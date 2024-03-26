#include <string>
#include <sstream>
#include <wil\com.h>
#include "Main.h"

struct HookInvokeString {
	std::wstring preferredVersion;
	std::wstring assemblyPath;
	std::wstring typeName;
	std::wstring methodName;
	std::wstring argument;

	HookInvokeString(std::wistringstream& str) {
		std::getline(str, preferredVersion, L'\0');
		std::getline(str, assemblyPath, L'\0');
		std::getline(str, typeName, L'\0');
		std::getline(str, methodName, L'\0');
		std::getline(str, argument, L'\0');
	}
};

HRESULT HandleGoBabyGo(wchar_t* pData, size_t cchData) {
	std::wistringstream argText(std::wstring(pData, cchData));
	HookInvokeString p(argText);

	return RunAssembly(
		p.preferredVersion.length() ? p.preferredVersion.data() : nullptr,
		p.assemblyPath.data(),
		p.typeName.data(),
		p.methodName.data(),
		p.argument.length() ? p.argument.data() : nullptr
	);
}

extern "C" __declspec(dllexport) LRESULT __stdcall MessageHookProc(int nCode, WPARAM wparam, LPARAM lparam) {
	if (nCode == HC_ACTION) {
		auto cwp = (CWPSTRUCT*)lparam;
		if (cwp->message == WM_GOBABYGO) {
			auto result = HandleGoBabyGo((wchar_t*)cwp->wParam, (size_t)(cwp->lParam / sizeof(wchar_t)));
			*(HRESULT*)(cwp->wParam) = result;
		}
	}

	return CallNextHookEx(nullptr, nCode, wparam, lparam);
}